using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class TeamDecisionTests
{
    // Tạo DbContext bộ nhớ riêng và chuẩn bị dữ liệu cho ca kiểm thử.
    private static InventoryDbContext Context()
    {
        var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        db.Users.Add(new User { UserId = 1, Username = "test-admin", FullName = "Test Admin", PasswordHash = "unit-test", Role = new Role { RoleId = 1, RoleName = RoleNames.Admin }, IsActive = true });
        db.Categories.Add(new Category { CategoryId = 1, CategoryName = "Test", IsActive = true });
        db.Products.Add(new Product { ProductId = 1, ProductCode = "P1", ProductName = "Product", CategoryId = 1, Unit = "piece", ReorderPoint = 2, MaximumStock = 10, PurchasePrice = 3, IsActive = true });
        db.Warehouses.AddRange(new Warehouse { WarehouseId = 1, WarehouseCode = "A", WarehouseName = "Warehouse A", IsActive = true }, new Warehouse { WarehouseId = 2, WarehouseCode = "B", WarehouseName = "Warehouse B", IsActive = true });
        db.Suppliers.Add(new Supplier { SupplierId = 1, SupplierCode = "S1", SupplierName = "Supplier", IsActive = true });
        db.SaveChanges(); return db;
    }
    // Tạo phiên quản trị viên giả lập cho ca kiểm thử.
    private static UserSession Admin()
    {
        var session = new UserSession(); session.SignIn(new(1, "test-admin", "Test Admin", null, RoleNames.Admin)); return session;
    }

    // Kiểm tra lịch sử lưu đủ từng lần chuyển trạng thái và người xử lý mới nhất.
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task LifecycleAudit_KeepsEveryTransitionAndLatestActors(bool import)
    {
        await using var db = Context(); var auth = new AuthorizationService(db); var session = Admin();
        var imports = new ImportService(db, auth); var exports = new ExportService(db, auth);
        var request = new ImportReceiptDto { WarehouseId = 1, SupplierId = 1, Note = "Original", Details = [new(1, 1, 2)] };
        var exportRequest = new ExportReceiptDto { WarehouseId = 1, Note = "Original", Details = [new(1, 1, 2)] };
        db.Inventories.Add(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 10, UpdatedAt = DateTime.Now }); await db.SaveChangesAsync();
        var id = import ? await imports.CreateDraftAsync(session, request) : await exports.CreateDraftAsync(session, exportRequest);
        Assert.AreEqual(0, await db.ReceiptStatusChanges.CountAsync());
        if (import)
        {
            await imports.SubmitAsync(session, id); await imports.RejectAsync(session, id, "First review");
            await imports.UpdateDraftAsync(session, id, request); await imports.SubmitAsync(session, id);
            await imports.RejectAsync(session, id, "Second review"); await imports.ResubmitAsync(session, id);
            await imports.SubmitAsync(session, id); await imports.ApproveAsync(session, id); await imports.CompleteAsync(session, id);
        }
        else
        {
            await exports.SubmitAsync(session, id); await exports.RejectAsync(session, id, "First review");
            await exports.UpdateDraftAsync(session, id, exportRequest); await exports.SubmitAsync(session, id);
            await exports.RejectAsync(session, id, "Second review"); await exports.ResubmitAsync(session, id);
            await exports.SubmitAsync(session, id); await exports.ApproveAsync(session, id); await exports.CompleteAsync(session, id);
        }
        var history = await db.ReceiptStatusChanges.OrderBy(x => x.ReceiptStatusChangeId).ToListAsync();
        Assert.AreEqual(9, history.Count);
        CollectionAssert.AreEqual(new[] { "PENDING_APPROVAL", "REJECTED", "DRAFT", "PENDING_APPROVAL", "REJECTED", "DRAFT", "PENDING_APPROVAL", "APPROVED", "COMPLETED" }, history.Select(x => x.ToStatus).ToArray());
        Assert.IsTrue(history.All(x => x.ChangedBy == 1 && x.FromStatus != x.ToStatus && (import ? x.ExportReceiptId is null : x.ImportReceiptId is null)));
        for (var i = 1; i < history.Count; i++) Assert.AreEqual(history[i - 1].ToStatus, history[i].FromStatus);
        var audit = import ? (await imports.GetByIdAsync(session, id))!.Audit : (await exports.GetByIdAsync(session, id))!.Audit;
        Assert.AreEqual(1, audit.SubmittedBy); Assert.AreEqual("Test Admin", audit.SubmittedByName); Assert.AreEqual(history[6].ChangedAt, audit.SubmittedAt);
        Assert.AreEqual(1, audit.ApprovedBy); Assert.AreEqual(history[7].ChangedAt, audit.ApprovedAt);
        Assert.AreEqual(1, audit.CompletedBy); Assert.AreEqual(history[8].ChangedAt, audit.CompletedAt);
        Assert.AreEqual("Second review", audit.RejectReason); Assert.IsNull(audit.CancelledAt);
        if (import) await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => imports.CompleteAsync(session, id));
        else await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => exports.CompleteAsync(session, id));
        Assert.AreEqual(9, await db.ReceiptStatusChanges.CountAsync());
    }

    // Kiểm tra hủy phiếu lưu người thực hiện, lý do và không làm thay đổi tồn.
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task LifecycleAudit_CancelRecordsActorReasonAndDoesNotPostStock(bool import)
    {
        await using var db = Context(); var auth = new AuthorizationService(db); var session = Admin();
        if (import) db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "I", WarehouseId = 1, SupplierId = 1, CreatedBy = 1, Status = "DRAFT" });
        else db.ExportReceipts.Add(new ExportReceipt { ExportReceiptId = 1, ReceiptCode = "E", WarehouseId = 1, CreatedBy = 1, Status = "DRAFT" });
        await db.SaveChangesAsync();
        var before = import ? (await new ImportService(db, auth).GetByIdAsync(session, 1))!.Audit : (await new ExportService(db, auth).GetByIdAsync(session, 1))!.Audit;
        Assert.IsNull(before.SubmittedBy); Assert.IsNull(before.ApprovedAt); Assert.IsNull(before.CancelledAt);
        if (import) await new ImportService(db, auth).CancelAsync(session, 1, "  Not needed  ");
        else await new ExportService(db, auth).CancelAsync(session, 1, "  Not needed  ");
        var row = await db.ReceiptStatusChanges.SingleAsync();
        Assert.AreEqual("DRAFT", row.FromStatus); Assert.AreEqual("CANCELLED", row.ToStatus); Assert.AreEqual("Not needed", row.Reason);
        var audit = import ? (await new ImportService(db, auth).GetByIdAsync(session, 1))!.Audit : (await new ExportService(db, auth).GetByIdAsync(session, 1))!.Audit;
        Assert.AreEqual(1, audit.CancelledBy); Assert.AreEqual("Test Admin", audit.CancelledByName); Assert.AreEqual(row.ChangedAt, audit.CancelledAt);
        Assert.AreEqual(0, await db.InventoryTransactions.CountAsync());
    }

    // Kiểm tra lịch sử phân trang ổn định, bảo vệ phạm vi xem và không cho xóa dấu vết.
    [TestMethod]
    public async Task LifecycleHistory_PagesStablyProtectsScopeAndPreventsHistoryDeletion()
    {
        await using var db = Context(); var auth = new AuthorizationService(db); var session = Admin();
        db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "I", WarehouseId = 1, SupplierId = 1, CreatedBy = 1, Status = "DRAFT" });
        var time = new DateTime(2026, 10, 15);
        for (var i = 1; i <= 25; i++) db.ReceiptStatusChanges.Add(new ReceiptStatusChange { ReceiptStatusChangeId = i, ImportReceiptId = 1, FromStatus = "REJECTED", ToStatus = "DRAFT", ChangedBy = 1, ChangedAt = time });
        await db.SaveChangesAsync(); var service = new ReceiptAuditService(db, auth);
        var first = await service.GetStatusChangesAsync(session, true, 1); var second = await service.GetStatusChangesAsync(session, true, 1, 2);
        Assert.AreEqual(25, first.Total); Assert.AreEqual(20, first.Rows.Count); Assert.AreEqual(25L, first.Rows[0].Id); Assert.AreEqual(5, second.Rows.Count); Assert.AreEqual(5L, second.Rows[0].Id);
        Assert.IsFalse(first.Rows.Select(x => x.Id).Intersect(second.Rows.Select(x => x.Id)).Any());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new ImportService(db, auth).DeleteDraftAsync(session, 1));
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => service.GetStatusChangesAsync(session, true, 1, 0));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.GetStatusChangesAsync(new UserSession(), true, 1));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.GetStatusChangesAsync(session, false, 1));
        Assert.AreEqual(25, await db.ReceiptStatusChanges.CountAsync());
    }

    // Kiểm tra gửi lại phiếu giữ ghi chú và toàn bộ lịch sử từ chối.
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Rejections_PreserveNotesAndAllHistoryAcrossResubmit(bool import)
    {
        await using var db = Context(); var auth = new AuthorizationService(db); var session = Admin();
        if (import) db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "IMP", WarehouseId = 1, SupplierId = 1, CreatedBy = 1, Status = "PENDING_APPROVAL", Note = "Original note", Details = [new ImportDetail { ProductId = 1, Quantity = 1, UnitPrice = 2 }] });
        else db.ExportReceipts.Add(new ExportReceipt { ExportReceiptId = 1, ReceiptCode = "EXP", WarehouseId = 1, CreatedBy = 1, Status = "PENDING_APPROVAL", Note = "Original note", Details = [new ExportDetail { ProductId = 1, Quantity = 1, UnitPrice = 2 }] });
        await db.SaveChangesAsync();
        for (var i = 0; i < 2; i++)
        {
            if (import) { var service = new ImportService(db, auth); await service.RejectAsync(session, 1, $"Reason {i}"); if (i == 0) { await service.ResubmitAsync(session, 1); await service.SubmitAsync(session, 1); } }
            else { var service = new ExportService(db, auth); await service.RejectAsync(session, 1, $"Reason {i}"); if (i == 0) { await service.ResubmitAsync(session, 1); await service.SubmitAsync(session, 1); } }
        }
        var note = import ? (await db.ImportReceipts.SingleAsync()).Note : (await db.ExportReceipts.SingleAsync()).Note;
        Assert.AreEqual("Original note", note);
        Assert.AreEqual(2, await db.ReceiptRejections.CountAsync());
        var history = await new ReceiptAuditService(db, auth).GetRejectionsAsync(session, import, 1);
        Assert.AreEqual(2, history.Total); Assert.AreEqual("Reason 1", history.Rows[0].Reason);
    }

    // Kiểm tra phải có lý do hợp lệ trước khi chuyển trạng thái hủy phiếu.
    [TestMethod]
    public async Task Cancel_RequiresReasonBeforeChangingState()
    {
        await using var db = Context(); var service = new ImportService(db, new AuthorizationService(db));
        db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "IMP", WarehouseId = 1, SupplierId = 1, CreatedBy = 1, Status = "DRAFT" }); await db.SaveChangesAsync();
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.CancelAsync(Admin(), 1, " "));
        Assert.AreEqual("DRAFT", (await db.ImportReceipts.SingleAsync()).Status);
        await service.CancelAsync(Admin(), 1, "No longer needed");
        Assert.AreEqual("No longer needed", (await db.ImportReceipts.SingleAsync()).CancelReason);
    }

    // Kiểm tra hoàn tất và điều chỉnh lưu đủ số lượng, lịch sử cùng người xử lý.
    [TestMethod]
    public async Task CompletionAndAdjustment_PersistQuantityAuditAndCompletionActor()
    {
        await using var db = Context(); var auth = new AuthorizationService(db); var session = Admin(); var imports = new ImportService(db, auth);
        var id = await imports.CreateDraftAsync(session, new ImportReceiptDto { WarehouseId = 1, SupplierId = 1, Details = [new(1, 5, 3)] });
        await imports.SubmitAsync(session, id); await imports.ApproveAsync(session, id); await imports.CompleteAsync(session, id);
        var receipt = await db.ImportReceipts.SingleAsync(); Assert.AreEqual(1, receipt.CompletedBy); Assert.IsNotNull(receipt.CompletedAt);
        var inventory = new InventoryService(db, auth); await inventory.AdjustAsync(session, 1, 1, -2, "Count correction");
        var adjustment = await db.InventoryTransactions.SingleAsync(x => x.TransactionType == "ADJUSTMENT");
        Assert.AreEqual(5, adjustment.BeforeQuantity); Assert.AreEqual(3, adjustment.AfterQuantity); Assert.AreEqual(-2, adjustment.SignedDifference);
    }

    // Kiểm tra hai biến động chuyển kho có chung mã tham chiếu và số lượng trái dấu.
    [TestMethod]
    public async Task Transfer_UsesSharedReferenceAndOppositeSignedMovements()
    {
        await using var db = Context(); db.Inventories.Add(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 7 }); await db.SaveChangesAsync();
        var service = new InventoryService(db, new AuthorizationService(db)); await service.TransferAsync(Admin(), 1, 2, 1, 3, "Transfer test");
        var rows = await db.InventoryTransactions.OrderBy(x => x.WarehouseId).ToListAsync();
        Assert.AreEqual(2, rows.Count); Assert.AreEqual(rows[0].ReferenceId, rows[1].ReferenceId); Assert.IsNotNull(rows[0].ReferenceId);
        Assert.AreEqual(-3, rows[0].SignedDifference); Assert.AreEqual(3, rows[1].SignedDifference);
        Assert.AreEqual(4, rows[0].AfterQuantity); Assert.AreEqual(3, rows[1].AfterQuantity);
    }

    // Kiểm tra phân tích dùng giá trị dòng phiếu hoàn tất và phân biệt lịch sử chưa đủ dữ liệu.
    [TestMethod]
    public async Task Analytics_UsesCompletedDetailValueAndDistinguishesUnknownHistory()
    {
        await using var db = Context(); var date = new DateTime(2026, 10, 14);
        db.Inventories.AddRange(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 2 }, new Inventory { ProductId = 1, WarehouseId = 2, Quantity = 5 });
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = 1, WarehouseId = 1, TransactionType = "IMPORT", Quantity = 2, CreatedBy = 1, CreatedAt = date.AddDays(-100) });
        db.ExportReceipts.AddRange(new ExportReceipt { ExportReceiptId = 1, ReceiptCode = "DONE", WarehouseId = 1, CreatedBy = 1, Status = "COMPLETED", ReceiptDate = date.AddDays(-100), Details = [new ExportDetail { ProductId = 1, Quantity = 3, UnitPrice = 7 }] },
            new ExportReceipt { ExportReceiptId = 2, ReceiptCode = "DRAFT", WarehouseId = 1, CreatedBy = 1, Status = "DRAFT", ReceiptDate = date, Details = [new ExportDetail { ProductId = 1, Quantity = 100, UnitPrice = 100 }] });
        await db.SaveChangesAsync(); var result = await new AnalyticsService(db, new AuthorizationService(db)).GetAsync(Admin(), asOf: date);
        Assert.AreEqual(21m, result.EstimatedInventoryValue); Assert.AreEqual(21m, result.Abc.Single().ExportValue); Assert.AreEqual("A", result.Abc.Single().Classification);
        Assert.AreEqual(8, result.Stock[0].SuggestedQuantity); Assert.AreEqual("DEAD_STOCK", result.Stock[0].MovementStatus); Assert.AreEqual("INSUFFICIENT_HISTORY", result.Stock[1].MovementStatus);
    }

    // Kiểm tra ABC giữ dòng vượt ngưỡng trong nhóm đang tích lũy và dùng đúng khoảng thời gian.
    [TestMethod]
    public async Task Analytics_AbcIncludesCrossingRowAndUsesExactWindow()
    {
        await using var db = Context(); var today = new DateTime(2026, 10, 14);
        var values = new[] { 70m, 15m, 10m, 5m };
        for (var i = 0; i < values.Length; i++)
        {
            var productId = i + 10;
            db.Products.Add(new Product { ProductId = productId, ProductCode = $"P{productId}", ProductName = "ABC Product", CategoryId = 1, Unit = "piece", ReorderPoint = 2, MaximumStock = 10, IsActive = true });
            db.ExportReceipts.Add(new ExportReceipt { ReceiptCode = $"ABC{i}", WarehouseId = 1, CreatedBy = 1, Status = "COMPLETED", ReceiptDate = today, Details = [new ExportDetail { ProductId = productId, Quantity = 1, UnitPrice = values[i] }] });
        }
        db.ExportReceipts.Add(new ExportReceipt { ReceiptCode = "OUTSIDE", WarehouseId = 1, CreatedBy = 1, Status = "COMPLETED", ReceiptDate = today.AddMonths(-12).AddDays(-1), Details = [new ExportDetail { ProductId = 1, Quantity = 1, UnitPrice = 999 }] });
        await db.SaveChangesAsync(); var data = await new AnalyticsService(db, new AuthorizationService(db)).GetAsync(Admin(), asOf: today);
        CollectionAssert.AreEqual(new[] { "A", "A", "B", "C" }, data.Abc.Select(x => x.Classification).ToArray());
        Assert.AreEqual(100m, data.Abc.Sum(x => x.ExportValue)); Assert.AreEqual(1m, data.Abc.Last().CumulativeShare);
    }

    // Kiểm tra giá trị xuất bằng 0 không bị ép vào một nhóm ABC.
    [TestMethod]
    public async Task Analytics_ZeroValuesStayUnclassified()
    {
        await using var db = Context(); var date = new DateTime(2026, 10, 14);
        db.ExportReceipts.Add(new ExportReceipt { ReceiptCode = "FREE", WarehouseId = 1, CreatedBy = 1, Status = "COMPLETED", ReceiptDate = date, Details = [new ExportDetail { ProductId = 1, Quantity = 5, UnitPrice = 0 }] }); await db.SaveChangesAsync();
        var data = await new AnalyticsService(db, new AuthorizationService(db)).GetAsync(Admin(), asOf: date);
        Assert.AreEqual("UNCLASSIFIED", data.Abc.Single().Classification); Assert.AreEqual(0m, data.Abc.Single().ValueShare);
    }

    // Kiểm tra nhân viên không được xem phân tích hoặc lịch sử của kho ngoài phạm vi.
    [TestMethod]
    public async Task AnalyticsAndAudit_DenyOtherWarehouseForStaff()
    {
        await using var db = Context();
        db.Users.Add(new User { UserId = 2, Username = "staff", FullName = "Staff", PasswordHash = "test", IsActive = true, Role = new Role { RoleId = 2, RoleName = RoleNames.Staff } });
        db.UserWarehouses.Add(new UserWarehouse { UserId = 2, WarehouseId = 1, IsActive = true });
        db.Inventories.AddRange(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 2 }, new Inventory { ProductId = 1, WarehouseId = 2, Quantity = 500 });
        db.ExportReceipts.Add(new ExportReceipt { ExportReceiptId = 1, ReceiptCode = "PRIVATE", WarehouseId = 2, CreatedBy = 1, Status = "REJECTED" }); await db.SaveChangesAsync();
        var session = new UserSession(); session.SignIn(new(2, "staff", "Staff", null, RoleNames.Staff)); session.SetCurrentWarehouse(1);
        var auth = new AuthorizationService(db); var analytics = new AnalyticsService(db, auth);
        var data = await analytics.GetAsync(session); Assert.AreEqual(6m, data.EstimatedInventoryValue); Assert.AreEqual(1, data.Stock.Count);
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => analytics.GetAsync(session, 2));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => new ReceiptAuditService(db, auth).GetRejectionsAsync(session, false, 1));
    }

    // Kiểm tra lịch sử từ chối có phân trang và chặn xóa phiếu nháp đã có lịch sử.
    [TestMethod]
    public async Task RejectionHistory_PaginatesAndPreventsDeletingHistoricalDraft()
    {
        await using var db = Context();
        db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "HISTORY", WarehouseId = 1, SupplierId = 1, CreatedBy = 1, Status = "DRAFT" });
        db.ReceiptRejections.AddRange(Enumerable.Range(1, 25).Select(i => new ReceiptRejection { ImportReceiptId = 1, RejectedBy = 1, RejectedAt = new DateTime(2026,10,14).AddMinutes(i), Reason = $"Review {i}" })); await db.SaveChangesAsync();
        var auth = new AuthorizationService(db); var service = new ReceiptAuditService(db, auth);
        var first = await service.GetRejectionsAsync(Admin(), true, 1); var second = await service.GetRejectionsAsync(Admin(), true, 1, 2);
        Assert.AreEqual(25, first.Total); Assert.AreEqual(20, first.Rows.Count); Assert.AreEqual(5, second.Rows.Count);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new ImportService(db, auth).DeleteDraftAsync(Admin(), 1));
        Assert.AreEqual(25, await db.ReceiptRejections.CountAsync()); Assert.AreEqual(1, await db.ImportReceipts.CountAsync());
    }
}
