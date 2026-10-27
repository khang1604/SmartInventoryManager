using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class AccountSafetyAndPagingTests
{
    // Tạo DbContext bộ nhớ riêng và chuẩn bị dữ liệu cho ca kiểm thử.
    private static InventoryDbContext Context()
    {
        var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = RoleNames.Admin }, new Role { RoleId = 2, RoleName = RoleNames.Staff }, new Role { RoleId = 3, RoleName = RoleNames.WarehouseManager });
        db.Users.AddRange(
            new User { UserId = 1, Username = "admin", Email = "admin@example.invalid", FullName = "Admin", PasswordHash = "unit", RoleId = 1, IsActive = true },
            new User { UserId = 2, Username = "staff", Email = "staff@example.invalid", FullName = "Staff", PasswordHash = "unit", RoleId = 2, IsActive = true },
            new User { UserId = 3, Username = "manager", Email = "manager@example.invalid", FullName = "Manager", PasswordHash = "unit", RoleId = 3, IsActive = true });
        db.SaveChanges(); return db;
    }

    // Tạo phiên đăng nhập giả lập theo người dùng và vai trò của ca kiểm thử.
    private static UserSession Session(int id = 1, string role = RoleNames.Admin)
    {
        var session = new UserSession(); session.SignIn(new(id, "unit", "Unit User", null, role)); return session;
    }

    // Kiểm tra không cho tên đăng nhập trùng email của tài khoản khác, kể cả khác hoa thường.
    [DataTestMethod]
    [DataRow("STAFF@example.invalid", "new@example.invalid")]
    [DataRow("new-user", "USERNAME@example.invalid")]
    public async Task Create_RejectsCrossNamespaceIdentifiersIgnoringCase(string username, string email)
    {
        await using var db = Context(); var service = new UserService(db, new AuthorizationService(db));
        if (email == "USERNAME@example.invalid")
        {
            (await db.Users.SingleAsync(user => user.UserId == 2)).Username = "username@example.invalid";
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CreateAsync(Session(), new(username, "New", email, "Password1!", "Password1!", RoleNames.Staff)));
        Assert.AreEqual(3, await db.Users.CountAsync()); Assert.AreEqual(0, await db.SystemLogs.CountAsync());
    }

    // Kiểm tra đổi email trùng tên đăng nhập bị từ chối và hồ sơ cũ được giữ nguyên.
    [TestMethod]
    public async Task AccountUpdate_RejectsEmailMatchingAnotherUsernameAndKeepsOldProfile()
    {
        await using var db = Context(); var staff = await db.Users.SingleAsync(x => x.UserId == 2);
        staff.Username = "taken@example.invalid"; await db.SaveChangesAsync();
        var service = new UserService(db, new AuthorizationService(db));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateAccountAsync(Session(), new("Changed", "TAKEN@example.invalid")));
        Assert.AreEqual("Admin", (await db.Users.SingleAsync(x => x.UserId == 1)).FullName);
        Assert.AreEqual("admin@example.invalid", (await db.Users.SingleAsync(x => x.UserId == 1)).Email);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateUserAsync(Session(), 1, new("Changed", "taken@example.invalid", RoleNames.Admin, true)));
        Assert.AreEqual(0, await db.SystemLogs.CountAsync());
    }

    // Kiểm tra định danh mơ hồ từ dữ liệu cũ không được tự chọn một tài khoản để xác thực.
    [TestMethod]
    public async Task Auth_LegacyAmbiguityFailsClearlyWithoutChoosingAnAccount()
    {
        await using var db = Context();
        db.Users.Add(new User { UserId = 4, Username = "staff@example.invalid", Email = "other@example.invalid", FullName = "Legacy", PasswordHash = "unit", RoleId = 2, IsActive = true }); await db.SaveChangesAsync();
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new AuthService(db).AuthenticateAsync("STAFF@example.invalid", "Password1!"));
        StringAssert.Contains(exception.Message, "liên hệ ADMIN");
        Assert.AreEqual(4, await db.Users.CountAsync());
    }

    // Kiểm tra khởi tạo quản trị từ chối định danh trùng và không tạo thêm tài khoản.
    [TestMethod]
    public async Task Bootstrap_RejectsCrossNamespaceIdentifierWithoutCreatingAnotherAccount()
    {
        await using var db = Context(); (await db.Users.SingleAsync(x => x.UserId == 1)).IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new BootstrapAdminService(db).CreateFirstAdminAsync(new("staff@example.invalid", "Bootstrap", "new@example.invalid", "Password1!", "Password1!", RoleNames.Admin)));
        Assert.AreEqual(3, await db.Users.CountAsync()); Assert.AreEqual(0, await db.SystemLogs.CountAsync());
    }

    // Kiểm tra chỉ được hạ quyền ADMIN khi còn một quản trị viên hoạt động khác.
    [TestMethod]
    public async Task LastAdmin_CannotDemoteSelfButCanWhenAnotherActiveAdminExists()
    {
        await using var db = Context(); var service = new UserService(db, new AuthorizationService(db));
        var request = new UpdateUserRequest("Admin", "admin@example.invalid", RoleNames.Staff, true);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateUserAsync(Session(), 1, request));
        Assert.AreEqual(1, (await db.Users.SingleAsync(x => x.UserId == 1)).RoleId);
        db.Users.Add(new User { UserId = 4, Username = "admin2", FullName = "Admin2", PasswordHash = "unit", RoleId = 1, IsActive = true }); await db.SaveChangesAsync();
        await service.UpdateUserAsync(Session(), 1, request);
        Assert.AreEqual(1, await db.Users.CountAsync(x => x.IsActive && x.Role.RoleName == RoleNames.Admin));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.GetListAsync(Session()));
    }

    // Kiểm tra quản lý đang phụ trách kho không được mất quyền trước khi chuyển phân công.
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AssignedManager_CannotLoseRoleOrBeDisabledUntilWarehouseReassigned(bool activeWarehouse)
    {
        await using var db = Context();
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "W", WarehouseName = "Warehouse", ManagerId = 3, IsActive = activeWarehouse }); await db.SaveChangesAsync();
        var authorization = new AuthorizationService(db); var service = new UserService(db, authorization);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateUserAsync(Session(), 3, new("Manager", "manager@example.invalid", RoleNames.Staff, true)));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.DeactivateAsync(Session(), 3));
        Assert.AreEqual(3, (await db.Users.SingleAsync(x => x.UserId == 3)).RoleId);
        await new WarehouseService(db, authorization).UpdateAsync(Session(), 1, new WarehouseRequestDto { WarehouseCode = "W", WarehouseName = "Warehouse", ManagerId = null, IsActive = activeWarehouse });
        await service.UpdateUserAsync(Session(), 3, new("Manager", "manager@example.invalid", RoleNames.Staff, true));
        Assert.AreEqual(2, (await db.Users.SingleAsync(x => x.UserId == 3)).RoleId);
    }

    // Kiểm tra lọc phiếu trước phân trang, truy cập quá 500 dòng và giữ thứ tự ổn định.
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Receipts_PageBeyond500FiltersBeforePagingAndKeepsStableOrder(bool import)
    {
        await using var db = Context();
        db.Warehouses.AddRange(new Warehouse { WarehouseId = 1, WarehouseCode = "W", WarehouseName = "Warehouse", IsActive = true }, new Warehouse { WarehouseId = 2, WarehouseCode = "X", WarehouseName = "Inactive", IsActive = false });
        db.Suppliers.Add(new Supplier { SupplierId = 1, SupplierCode = "S", SupplierName = "Supplier", IsActive = true });
        for (var i = 1; i <= 502; i++)
        {
            var warehouse = i == 502 ? 2 : 1; var status = i % 2 == 0 ? "DRAFT" : "COMPLETED";
            if (import) db.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = i, ReceiptCode = $"R{i:0000}", WarehouseId = warehouse, SupplierId = 1, CreatedBy = 1, ReceiptDate = new DateTime(2026, 10, 15), Status = status });
            else db.ExportReceipts.Add(new ExportReceipt { ExportReceiptId = i, ReceiptCode = $"R{i:0000}", WarehouseId = warehouse, CreatedBy = 1, ReceiptDate = new DateTime(2026, 10, 15), Status = status });
        }
        await db.SaveChangesAsync(); var authorization = new AuthorizationService(db);
        var request = new ReceiptSearchDto { PageNumber = 6, PageSize = 100 };
        if (import)
        {
            var service = new ImportService(db, authorization); var last = await service.GetPageAsync(Session(), request);
            Assert.AreEqual(501, last.TotalCount); Assert.AreEqual(1, last.Items.Count); Assert.AreEqual(1, last.Items[0].ImportReceiptId);
            var filtered = await service.GetPageAsync(Session(), request with { PageNumber = 99, Status = "DRAFT" });
            Assert.AreEqual(250, filtered.TotalCount); Assert.AreEqual(3, filtered.PageNumber); Assert.IsTrue(filtered.Items.All(x => x.Status == "DRAFT"));
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.GetPageAsync(Session(), request with { WarehouseId = 2 }));
        }
        else
        {
            var service = new ExportService(db, authorization); var last = await service.GetPageAsync(Session(), request);
            Assert.AreEqual(501, last.TotalCount); Assert.AreEqual(1, last.Items.Count); Assert.AreEqual(1, last.Items[0].ExportReceiptId);
            var filtered = await service.GetPageAsync(Session(), request with { PageNumber = 99, Status = "DRAFT" });
            Assert.AreEqual(250, filtered.TotalCount); Assert.AreEqual(3, filtered.PageNumber); Assert.IsTrue(filtered.Items.All(x => x.Status == "DRAFT"));
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.GetPageAsync(Session(), request with { WarehouseId = 2 }));
        }
    }

    // Kiểm tra danh sách lựa chọn không âm thầm bỏ các mục sau mốc 200.
    [TestMethod]
    public async Task Lookups_DoNotSilentlyHideItemsAfter200()
    {
        await using var db = Context();
        for (var i = 1; i <= 201; i++)
        {
            db.Categories.Add(new Category { CategoryId = i, CategoryName = $"Category {i:000}", IsActive = true });
            db.Products.Add(new Product { ProductId = i, ProductCode = $"P{i:000}", ProductName = $"Product {i}", Unit = "piece", CategoryId = i, ReorderPoint = 2, MaximumStock = 10, IsActive = true });
            db.Warehouses.Add(new Warehouse { WarehouseId = i, WarehouseCode = $"W{i:000}", WarehouseName = $"Warehouse {i}", IsActive = true });
            db.Suppliers.Add(new Supplier { SupplierId = i, SupplierCode = $"S{i:000}", SupplierName = $"Supplier {i}", IsActive = true });
        }
        await db.SaveChangesAsync(); var authorization = new AuthorizationService(db);
        Assert.AreEqual(201, (await new ProductService(db).GetActiveAsync(Session())).Count);
        Assert.AreEqual(201, (await new WarehouseService(db, authorization).GetActiveAsync(Session())).Count);
        Assert.AreEqual(201, (await new SupplierService(db, authorization).GetActiveAsync(Session())).Count);
        Assert.AreEqual(201, (await new CategoryService(db, authorization).GetActiveAsync(Session())).Count);
    }

    // Kiểm tra phân trang người dùng, thông báo và nhật ký vượt 500 dòng vẫn giữ phạm vi quyền.
    [TestMethod]
    public async Task UsersNotificationsAndLogs_PagePast500AndProtectScope()
    {
        await using var db = Context();
        for (var i = 4; i <= 501; i++) db.Users.Add(new User { UserId = i, Username = $"U{i:000}", FullName = "User", PasswordHash = "unit", RoleId = 2, IsActive = true });
        for (var i = 1; i <= 501; i++)
        {
            db.Notifications.Add(new Notification { NotificationId = i, UserId = 1, Title = "Notice", Message = "Message", NotificationType = "INFO", CreatedAt = new DateTime(2026, 10, 15) });
            db.SystemLogs.Add(new SystemLog { LogId = i, UserId = 1, Action = "UNIT", CreatedAt = new DateTime(2026, 10, 15) });
        }
        db.Notifications.Add(new Notification { NotificationId = 502, UserId = 2, Title = "Private", Message = "Message", NotificationType = "INFO" });
        await db.SaveChangesAsync(); var authorization = new AuthorizationService(db);
        var users = new UserService(db, authorization); var notices = new NotificationService(db); var logs = new SystemLogService(db, authorization);
        var userPage = await users.GetPageAsync(Session(), 6, 100); Assert.AreEqual(501, userPage.TotalCount); Assert.AreEqual(1, userPage.Items.Count);
        var noticePage = await notices.GetPageAsync(Session(), 6, 100); Assert.AreEqual(501, noticePage.TotalCount); Assert.AreEqual(1, noticePage.Items.Count);
        var logPage = await logs.GetPageAsync(Session(), 6, 100); Assert.AreEqual(501, logPage.TotalCount); Assert.AreEqual(1, logPage.Items.Count);
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => logs.GetPageAsync(Session(2, RoleNames.Staff)));
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => users.GetPageAsync(Session(), 0));
    }
}
