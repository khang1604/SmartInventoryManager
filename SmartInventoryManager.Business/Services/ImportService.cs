using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Validators;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class ImportService : IImportService
{
    private const int MaxListResults = 500;
    private const int MaxReceiptNoteLength = 500;
    private const int MaxRejectReasonLength = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly InventoryService _inventory;
    private readonly StockEventPublisher _publisher;
    private readonly ImportValidator _validator = new();

    public ImportService(InventoryDbContext dbContext, AuthorizationService authorization, StockEventPublisher? publisher = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _publisher = publisher ?? new StockEventPublisher();
        _inventory = new InventoryService(_dbContext, _authorization, _publisher);
    }

    public event EventHandler<ReceiptCompletedEvent>? ReceiptCompleted;

    public async Task<int> CreateDraftAsync(
        UserSession session,
        ImportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSession(session);
        ValidateReceiptId(request.WarehouseId);
        if (request.SupplierId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.SupplierId));
        }
        ValidateNote(request.Note);

        if (!await _authorization.CanCreateDraftReceiptAsync(session, request.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền tạo phiếu nhập tại kho này.");
        }

        ValidateRequest(request, requireDetails: false);
        var warehouseExists = await _dbContext.Warehouses.AnyAsync(
            warehouse => warehouse.WarehouseId == request.WarehouseId && warehouse.IsActive,
            cancellationToken);
        var supplierExists = await _dbContext.Suppliers.AnyAsync(
            supplier => supplier.SupplierId == request.SupplierId && supplier.IsActive,
            cancellationToken);
        if (!warehouseExists || !supplierExists)
        {
            throw new InvalidOperationException("Kho hoặc nhà cung cấp không tồn tại hoặc đã ngừng hoạt động.");
        }

        var productIds = request.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductIds = await _dbContext.Products
            .Where(product => productIds.Contains(product.ProductId) && product.IsActive)
            .Select(product => product.ProductId)
            .ToListAsync(cancellationToken);
        if (activeProductIds.Count != productIds.Length)
        {
            throw new InvalidOperationException("Có sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }

        var receipt = new ImportReceipt
        {
            ReceiptCode = GenerateReceiptCode(),
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            CreatedBy = session.CurrentUser!.UserId,
            ReceiptDate = DateTime.Now,
            Status = "DRAFT",
            Note = request.Note?.Trim(),
            TotalAmount = request.Details.Sum(detail => detail.Quantity * detail.UnitPrice),
            Details = request.Details.Select(detail => new ImportDetail
            {
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            }).ToList()
        };

        _dbContext.ImportReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return receipt.ImportReceiptId;
    }

    public async Task<IReadOnlyList<ImportReceiptListItemDto>> GetListAsync(
        UserSession session,
        int? warehouseId = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem phiếu nhập.");
        }
        var accessibleWarehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (warehouseId.HasValue && !accessibleWarehouseIds.Contains(warehouseId.Value))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem phiếu nhập tại kho này.");
        }

        var query = _dbContext.ImportReceipts
            .AsNoTracking()
            .Where(receipt => accessibleWarehouseIds.Contains(receipt.WarehouseId));
        if (warehouseId.HasValue)
        {
            query = query.Where(receipt => receipt.WarehouseId == warehouseId.Value);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(receipt => receipt.Status == status.Trim());
        }
        if (from.HasValue)
        {
            query = query.Where(receipt => receipt.ReceiptDate >= from.Value.Date);
        }
        if (to.HasValue)
        {
            if (from.HasValue && to.Value.Date < from.Value.Date)
            {
                throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
            }
            query = query.Where(receipt => receipt.ReceiptDate < to.Value.Date.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(receipt => receipt.ReceiptCode.Contains(normalizedSearch));
        }
        return await query
            .OrderByDescending(receipt => receipt.ReceiptDate)
            .Select(receipt => new ImportReceiptListItemDto(
                receipt.ImportReceiptId,
                receipt.ReceiptCode,
                receipt.Supplier.SupplierName,
                receipt.Warehouse.WarehouseName,
                receipt.Creator.FullName,
                receipt.ReceiptDate,
                receipt.TotalAmount,
                receipt.Status))
            .Take(MaxListResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportReceiptDetailsDto?> GetByIdAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);
        var accessibleWarehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        var receipt = await _dbContext.ImportReceipts
            .AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.Warehouse)
            .Include(item => item.Creator)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId && accessibleWarehouseIds.Contains(item.WarehouseId), cancellationToken);
        var actorIds = receipt is null ? Array.Empty<int>() : new[]
        {
            receipt.CompletedBy ?? 0, receipt.SubmittedBy ?? 0, receipt.ApprovedBy ?? 0, receipt.CancelledBy ?? 0
        };
        var actorNames = await _dbContext.Users.AsNoTracking().Where(user => actorIds.Contains(user.UserId))
            .ToDictionaryAsync(user => user.UserId, user => user.FullName, cancellationToken);
        return receipt is null
            ? null
            : new ImportReceiptDetailsDto(
                receipt.ImportReceiptId,
                receipt.ReceiptCode,
                receipt.SupplierId,
                receipt.Supplier.SupplierName,
                receipt.WarehouseId,
                receipt.Warehouse.WarehouseName,
                receipt.CreatedBy,
                receipt.Creator.FullName,
                receipt.ReceiptDate,
                receipt.TotalAmount,
                receipt.Status,
                receipt.Note,
                receipt.Details.Select(detail => new ReceiptDetailViewDto(
                    detail.ProductId,
                    detail.Product.ProductCode,
                    detail.Product.ProductName,
                    detail.Product.Unit,
                    detail.Quantity,
                    detail.UnitPrice,
                    detail.Quantity * detail.UnitPrice)).ToList())
            {
                Audit = new ReceiptAuditDto(receipt.CompletedBy,
                    actorNames.GetValueOrDefault(receipt.CompletedBy ?? 0),
                    receipt.CompletedAt, receipt.CancelReason, receipt.RejectReason)
                {
                    SubmittedBy = receipt.SubmittedBy, SubmittedByName = actorNames.GetValueOrDefault(receipt.SubmittedBy ?? 0), SubmittedAt = receipt.SubmittedAt,
                    ApprovedBy = receipt.ApprovedBy, ApprovedByName = actorNames.GetValueOrDefault(receipt.ApprovedBy ?? 0), ApprovedAt = receipt.ApprovedAt,
                    CancelledBy = receipt.CancelledBy, CancelledByName = actorNames.GetValueOrDefault(receipt.CancelledBy ?? 0), CancelledAt = receipt.CancelledAt
                }
            };
    }

    public async Task SubmitAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts.Include(item => item.Details).SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được gửi duyệt.");
        }
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi phiếu nhập.");
        }
        await ValidateStoredReceiptAsync(receipt, cancellationToken);
        receipt.TotalAmount = receipt.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "PENDING_APPROVAL");
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteDraftAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
            throw new UnauthorizedAccessException("Bạn không có quyền xóa phiếu nhập nháp tại kho này.");
        if (receipt.Status != "DRAFT")
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được xóa.");

        if (await _dbContext.ReceiptRejections.AnyAsync(x => x.ImportReceiptId == importReceiptId, cancellationToken)
            || await _dbContext.ReceiptStatusChanges.AnyAsync(x => x.ImportReceiptId == importReceiptId, cancellationToken))
            throw new InvalidOperationException("Phiếu có lịch sử xử lý; hãy hủy để giữ audit, không được xóa.");
        _dbContext.ImportDetails.RemoveRange(receipt.Details);
        _dbContext.ImportReceipts.Remove(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền duyệt phiếu nhập tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập đang chờ duyệt mới được duyệt.");
        }
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "APPROVED");
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task RejectAsync(
        UserSession session,
        int importReceiptId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Lý do từ chối là bắt buộc.", nameof(reason));
        }
        if (reason.Trim().Length > MaxRejectReasonLength)
        {
            throw new ArgumentException("Lý do từ chối quá dài.", nameof(reason));
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền từ chối phiếu nhập tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập đang chờ duyệt mới được từ chối.");
        }
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "REJECTED", reason);
        receipt.RejectReason = reason.Trim();
        _dbContext.ReceiptRejections.Add(new ReceiptRejection
        {
            ImportReceiptId = importReceiptId, RejectedBy = session.CurrentUser!.UserId,
            RejectedAt = DateTime.Now, Reason = receipt.RejectReason
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResubmitAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi lại phiếu nhập.");
        }
        if (receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập bị từ chối mới được gửi lại.");
        }
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "DRAFT");
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public Task CompleteAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default)
    {
        var committed = false;
        return DeadlockRetry.RunAsync(() => CompleteCoreAsync(session, importReceiptId, cancellationToken, () => committed = true),
            () => _dbContext.ChangeTracker.Clear(), () => committed, cancellationToken);
    }

    private async Task CompleteCoreAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken, Action onCommitted)
    {
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var receipt = await _dbContext.ImportReceipts
            .Include(importReceipt => importReceipt.Details)
            .SingleOrDefaultAsync(importReceipt => importReceipt.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);

        if (!await _authorization.CanCompleteReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hoàn tất phiếu nhập tại kho này.");
        }

        if (receipt.Status == "COMPLETED")
            throw new InvalidOperationException("Phiếu nhập đã hoàn tất trước đó.");

        if (receipt.Status != "APPROVED")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập đã được duyệt mới được hoàn tất.");
        }

        await ValidateStoredReceiptAsync(receipt, cancellationToken);
        var alreadyPosted = await _dbContext.InventoryTransactions.AnyAsync(
            item => item.ReferenceType == InventoryTransactionTypes.ImportReceipt &&
                item.ReferenceId == importReceiptId,
            cancellationToken);
        if (alreadyPosted)
            throw new InvalidOperationException("Phiếu nhập đã được ghi nhận tồn kho trước đó.");

        var stockEvents = new List<InventoryChangedEvent>();
        foreach (var detail in receipt.Details)
            stockEvents.Add(await _inventory.PostReceiptLineAsync(session, receipt.WarehouseId, detail.ProductId,
                detail.Quantity, true, importReceiptId, receipt.Note, cancellationToken));

        receipt.TotalAmount = receipt.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "COMPLETED");
        var alertService = new StockAlertService(_dbContext, _authorization, _publisher);
        foreach (var productId in receipt.Details.Select(detail => detail.ProductId).Distinct())
            await alertService.RefreshAsync(productId, receipt.WarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await StockWorkflowEffects.CommitAsync(_dbContext, transaction, session, "COMPLETE_IMPORT", "ImportReceipt", importReceiptId, _publisher, onCommitted, cancellationToken);
        foreach (var stockEvent in stockEvents) _publisher.Publish(stockEvent);
        PublishReceiptCompleted(new ReceiptCompletedEvent(
            importReceiptId,
            import: true,
            receipt.WarehouseId,
            receipt.Details.Select(detail => detail.ProductId).Distinct().ToArray()));
    }

    public async Task UpdateDraftAsync(
        UserSession session,
        int importReceiptId,
        ImportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (receipt.Status != "DRAFT" && receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được sửa.");
        }
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa phiếu nhập tại kho này.");
        }
        ValidateRequest(request, requireDetails: false);
        if (request.WarehouseId != receipt.WarehouseId)
        {
            throw new InvalidOperationException("Không được đổi kho của phiếu nhập nháp.");
        }
        var supplierExists = await _dbContext.Suppliers.AnyAsync(
            supplier => supplier.SupplierId == request.SupplierId && supplier.IsActive,
            cancellationToken);
        var productIds = request.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductCount = await _dbContext.Products.CountAsync(
            product => productIds.Contains(product.ProductId) && product.IsActive,
            cancellationToken);
        if (!supplierExists || activeProductCount != productIds.Length)
        {
            throw new InvalidOperationException("Nhà cung cấp hoặc sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }
        receipt.SupplierId = request.SupplierId;
        receipt.Note = request.Note?.Trim();
        _dbContext.ImportDetails.RemoveRange(receipt.Details);
        receipt.Details = request.Details.Select(detail => new ImportDetail
        {
            ProductId = detail.ProductId,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice
        }).ToList();
        if (receipt.Status == "REJECTED")
            ReceiptStatusAudit.Change(_dbContext, session, receipt, "DRAFT");
        receipt.TotalAmount = request.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public Task CancelDraftAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default)
        => CancelAsync(session, importReceiptId, string.Empty, cancellationToken);

    public async Task CancelAsync(
        UserSession session,
        int importReceiptId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(importReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ImportReceipts
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanCancelReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy phiếu nhập tại kho này.");
        }
        if (receipt.Status != "DRAFT" && receipt.Status != "PENDING_APPROVAL" && receipt.Status != "APPROVED")
        {
            throw new InvalidOperationException("Phiếu nhập không ở trạng thái được phép hủy.");
        }
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("Lý do hủy là bắt buộc và không quá 500 ký tự.", nameof(reason));
        ReceiptStatusAudit.Change(_dbContext, session, receipt, "CANCELLED", reason);
        receipt.CancelReason = reason.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private void PublishReceiptCompleted(ReceiptCompletedEvent value)
    {
        if (ReceiptCompleted is null) return;
        foreach (EventHandler<ReceiptCompletedEvent> handler in ReceiptCompleted.GetInvocationList())
            try { handler(this, value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Receipt subscriber failed: {ex.GetType().Name}"); }
    }

    private async Task ValidateStoredReceiptAsync(ImportReceipt receipt, CancellationToken cancellationToken)
    {
        if (receipt.Details.Count == 0)
            throw new InvalidOperationException("Phiếu nhập phải có ít nhất một dòng hàng.");

        var supplierExists = await _dbContext.Suppliers.AnyAsync(
            supplier => supplier.SupplierId == receipt.SupplierId && supplier.IsActive,
            cancellationToken);
        var warehouseExists = await _dbContext.Warehouses.AnyAsync(
            warehouse => warehouse.WarehouseId == receipt.WarehouseId && warehouse.IsActive,
            cancellationToken);
        var productIds = receipt.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductCount = await _dbContext.Products.CountAsync(
            product => productIds.Contains(product.ProductId) && product.IsActive,
            cancellationToken);
        if (!supplierExists || !warehouseExists || activeProductCount != productIds.Distinct().Count() ||
            productIds.Length != productIds.Distinct().Count() ||
            receipt.Details.Any(detail => detail.Quantity <= 0 || detail.UnitPrice < 0))
        {
            throw new InvalidOperationException("Phiếu nhập có kho, nhà cung cấp, sản phẩm hoặc số lượng không hợp lệ.");
        }
    }

    private void ValidateRequest(ImportReceiptDto request, bool requireDetails)
    {
        if ((requireDetails && request.Details.Count == 0) ||
            request.Details.Select(detail => detail.ProductId).Distinct().Count() != request.Details.Count)
        {
            throw new ArgumentException("Chi tiết phiếu nhập không được trùng và phải có ít nhất một dòng khi chuyển trạng thái.", nameof(request));
        }
        var errors = request.Details
            .SelectMany(detail => _validator.ValidateDetail(detail.ProductId, detail.Quantity, detail.UnitPrice))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }
    }

    private static string GenerateReceiptCode() => $"PN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..38];

    private static void ValidateReceiptId(int receiptId)
    {
        if (receiptId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiptId));
        }
    }

    private static void ValidateNote(string? note)
    {
        if (note?.Trim().Length > MaxReceiptNoteLength)
        {
            throw new ArgumentException("Ghi chú phiếu nhập không quá 500 ký tự.", nameof(note));
        }
    }

    private static void ValidateSession(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentUser is null)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện thao tác.");
        }
    }
}
