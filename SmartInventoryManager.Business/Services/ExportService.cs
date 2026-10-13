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

public sealed class ExportService : IExportService
{
    private const int MaxListResults = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly StockMutationService _stockMutation;
    private readonly ExportValidator _validator = new();

    public ExportService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _stockMutation = new StockMutationService(_dbContext);
    }

    public event EventHandler<ReceiptCompletedEvent>? ReceiptCompleted;

    public async Task<int> CreateDraftAsync(
        UserSession session,
        ExportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSession(session);
        ValidateReceiptId(request.WarehouseId);
        ValidateNote(request.Note);
        if (!await _authorization.CanCreateDraftReceiptAsync(session, request.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền tạo phiếu xuất tại kho này.");
        }
        ValidateRequest(request, requireDetails: false);

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(
            warehouse => warehouse.WarehouseId == request.WarehouseId && warehouse.IsActive,
            cancellationToken);
        var productIds = request.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductIds = await _dbContext.Products
            .Where(product => productIds.Contains(product.ProductId) && product.IsActive)
            .Select(product => product.ProductId)
            .ToListAsync(cancellationToken);
        if (!warehouseExists || activeProductIds.Count != productIds.Length)
        {
            throw new InvalidOperationException("Kho hoặc sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }

        var receipt = new ExportReceipt
        {
            ReceiptCode = GenerateReceiptCode(),
            WarehouseId = request.WarehouseId,
            CreatedBy = session.CurrentUser!.UserId,
            ReceiptDate = DateTime.Now,
            Status = "DRAFT",
            Note = request.Note?.Trim(),
            TotalAmount = request.Details.Sum(detail => detail.Quantity * detail.UnitPrice),
            Details = request.Details.Select(detail => new ExportDetail
            {
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            }).ToList()
        };
        _dbContext.ExportReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return receipt.ExportReceiptId;
    }

    public async Task<IReadOnlyList<ExportReceiptListItemDto>> GetListAsync(
        UserSession session,
        int? warehouseId = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        var accessibleWarehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (warehouseId.HasValue && !accessibleWarehouseIds.Contains(warehouseId.Value))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem phiếu xuất tại kho này.");
        }

        var query = _dbContext.ExportReceipts
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
            .Select(receipt => new ExportReceiptListItemDto(
                receipt.ExportReceiptId,
                receipt.ReceiptCode,
                receipt.Warehouse.WarehouseName,
                receipt.Creator.FullName,
                receipt.ReceiptDate,
                receipt.TotalAmount,
                receipt.Status))
            .Take(MaxListResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExportReceiptDetailsDto?> GetByIdAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        var accessibleWarehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        var receipt = await _dbContext.ExportReceipts
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Include(item => item.Creator)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId && accessibleWarehouseIds.Contains(item.WarehouseId), cancellationToken);
        return receipt is null
            ? null
            : new ExportReceiptDetailsDto(
                receipt.ExportReceiptId,
                receipt.ReceiptCode,
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
                    detail.Quantity * detail.UnitPrice)).ToList());
    }

    public async Task SubmitAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts.Include(item => item.Details).SingleOrDefaultAsync(
            item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được gửi duyệt.");
        }
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi phiếu xuất.");
        }
        await ValidateStoredReceiptAsync(receipt, cancellationToken);
        receipt.TotalAmount = receipt.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        receipt.Status = "PENDING_APPROVAL";
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteDraftAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
            throw new UnauthorizedAccessException("Bạn không có quyền xóa phiếu xuất nháp tại kho này.");
        if (receipt.Status != "DRAFT")
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được xóa.");

        _dbContext.ExportDetails.RemoveRange(receipt.Details);
        _dbContext.ExportReceipts.Remove(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task ApproveAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts.SingleOrDefaultAsync(
            item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền duyệt phiếu xuất tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất đang chờ duyệt mới được duyệt.");
        }
        receipt.Status = "APPROVED";
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task RejectAsync(UserSession session, int exportReceiptId, string reason, CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Lý do từ chối là bắt buộc.", nameof(reason));
        }
        if (reason.Trim().Length > 500)
        {
            throw new ArgumentException("Lý do từ chối quá dài.", nameof(reason));
        }
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts.SingleOrDefaultAsync(
            item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền từ chối phiếu xuất tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất đang chờ duyệt mới được từ chối.");
        }
        receipt.Status = "REJECTED";
        receipt.Note = $"Lý do từ chối: {reason.Trim()}";
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResubmitAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts.SingleOrDefaultAsync(
            item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi lại phiếu xuất.");
        }
        if (receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất bị từ chối mới được gửi lại.");
        }
        receipt.Status = "DRAFT";
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var receipt = await _dbContext.ExportReceipts
            .Include(exportReceipt => exportReceipt.Details)
            .SingleOrDefaultAsync(exportReceipt => exportReceipt.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanCompleteReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hoàn tất phiếu xuất tại kho này.");
        }
        if (receipt.Status == "COMPLETED")
            throw new InvalidOperationException("Phiếu xuất đã hoàn tất trước đó.");
        if (receipt.Status != "APPROVED")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất đã được duyệt mới được hoàn tất.");
        }

        await ValidateStoredReceiptAsync(receipt, cancellationToken);
        var alreadyPosted = await _dbContext.InventoryTransactions.AnyAsync(
            item => item.ReferenceType == InventoryTransactionTypes.ExportReceipt &&
                item.ReferenceId == exportReceiptId,
            cancellationToken);
        if (alreadyPosted)
            throw new InvalidOperationException("Phiếu xuất đã được ghi nhận tồn kho trước đó.");

        foreach (var detail in receipt.Details)
        {
            var availableQuantity = await _dbContext.Inventories
                .Where(item => item.WarehouseId == receipt.WarehouseId && item.ProductId == detail.ProductId)
                .Select(item => (int?)item.Quantity)
                .SingleOrDefaultAsync(cancellationToken);
            if (!availableQuantity.HasValue)
            {
                throw new InvalidOperationException($"Sản phẩm {detail.ProductId} chưa có tồn kho tại kho xuất.");
            }
            if (availableQuantity.Value < detail.Quantity)
            {
                throw new InvalidOperationException($"Sản phẩm {detail.ProductId} không đủ tồn kho để xuất.");
            }
        }

        foreach (var detail in receipt.Details)
        {
            await _stockMutation.DecreaseAsync(receipt.WarehouseId, detail.ProductId, detail.Quantity, cancellationToken);
            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = detail.ProductId,
                WarehouseId = receipt.WarehouseId,
                TransactionType = InventoryTransactionTypes.Export,
                Quantity = detail.Quantity,
                ReferenceType = InventoryTransactionTypes.ExportReceipt,
                ReferenceId = exportReceiptId,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = DateTime.Now,
                Note = receipt.Note
            });
        }

        receipt.TotalAmount = receipt.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        receipt.Status = "COMPLETED";
        var alertService = new StockAlertService(_dbContext, _authorization);
        foreach (var productId in receipt.Details.Select(detail => detail.ProductId).Distinct())
            await alertService.RefreshAsync(productId, receipt.WarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        ReceiptCompleted?.Invoke(this, new ReceiptCompletedEvent(
            exportReceiptId,
            import: false,
            receipt.WarehouseId,
            receipt.Details.Select(detail => detail.ProductId).Distinct().ToArray()));
    }

    public async Task UpdateDraftAsync(
        UserSession session,
        int exportReceiptId,
        ExportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        ValidateNote(request.Note);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (receipt.Status != "DRAFT" && receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được sửa.");
        }
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa phiếu xuất tại kho này.");
        }

        ValidateRequest(request, requireDetails: false);
        if (request.WarehouseId != receipt.WarehouseId)
        {
            throw new InvalidOperationException("Không được đổi kho của phiếu xuất nháp.");
        }
        var productIds = request.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductCount = await _dbContext.Products.CountAsync(
            product => productIds.Contains(product.ProductId) && product.IsActive,
            cancellationToken);
        if (activeProductCount != productIds.Length)
        {
            throw new InvalidOperationException("Có sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }
        receipt.Note = request.Note?.Trim();
        _dbContext.ExportDetails.RemoveRange(receipt.Details);
        receipt.Details = request.Details.Select(detail => new ExportDetail
        {
            ProductId = detail.ProductId,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice
        }).ToList();
        if (receipt.Status == "REJECTED")
            receipt.Status = "DRAFT";
        receipt.TotalAmount = request.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task CancelDraftAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateReceiptId(exportReceiptId);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var receipt = await _dbContext.ExportReceipts
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        // Re-read scalar state even if this receipt was already tracked by an earlier operation.
        await _dbContext.Entry(receipt).ReloadAsync(cancellationToken);
        if (!await _authorization.CanCancelReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy phiếu xuất tại kho này.");
        }
        if (receipt.Status != "DRAFT" && receipt.Status != "PENDING_APPROVAL" && receipt.Status != "APPROVED")
        {
            throw new InvalidOperationException("Phiếu xuất không ở trạng thái được phép hủy.");
        }
        receipt.Status = "CANCELLED";
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private async Task ValidateStoredReceiptAsync(ExportReceipt receipt, CancellationToken cancellationToken)
    {
        if (receipt.Details.Count == 0)
            throw new InvalidOperationException("Phiếu xuất phải có ít nhất một dòng hàng.");

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(
            warehouse => warehouse.WarehouseId == receipt.WarehouseId && warehouse.IsActive,
            cancellationToken);
        var productIds = receipt.Details.Select(detail => detail.ProductId).ToArray();
        var activeProductCount = await _dbContext.Products.CountAsync(
            product => productIds.Contains(product.ProductId) && product.IsActive,
            cancellationToken);
        if (!warehouseExists || activeProductCount != productIds.Distinct().Count() ||
            productIds.Length != productIds.Distinct().Count() ||
            receipt.Details.Any(detail => detail.Quantity <= 0 || detail.UnitPrice < 0))
        {
            throw new InvalidOperationException("Phiếu xuất có kho, sản phẩm hoặc số lượng không hợp lệ.");
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

    private static void ValidateReceiptId(int receiptId)
    {
        if (receiptId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiptId));
        }
    }

    private static void ValidateNote(string? note)
    {
        if (note?.Trim().Length > 500)
        {
            throw new ArgumentException("Ghi chú không quá 500 ký tự.", nameof(note));
        }
    }

    private void ValidateRequest(ExportReceiptDto request, bool requireDetails)
    {
        if ((requireDetails && request.Details.Count == 0) ||
            request.Details.Select(detail => detail.ProductId).Distinct().Count() != request.Details.Count)
        {
            throw new ArgumentException("Chi tiết phiếu xuất không được trùng và phải có ít nhất một dòng khi chuyển trạng thái.", nameof(request));
        }
        var errors = request.Details
            .SelectMany(detail => _validator.ValidateDetail(detail.ProductId, detail.Quantity, detail.UnitPrice))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }
    }

    private static string GenerateReceiptCode() => $"PX{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..38];
}
