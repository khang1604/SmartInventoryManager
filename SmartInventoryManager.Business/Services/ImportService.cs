using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Business.Validators;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;
using InventoryEntity = SmartInventoryManager.Data.Entities.Inventory;

namespace SmartInventoryManager.Business.Services;

public sealed class ImportService
{
    private const int MaxListResults = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly ImportValidator _validator = new();

    public ImportService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<int> CreateDraftAsync(
        UserSession session,
        ImportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await _authorization.CanCreateDraftReceiptAsync(session, request.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền tạo phiếu nhập tại kho này.");
        }

        ValidateRequest(request);
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
            ReceiptCode = request.ReceiptCode.Trim(),
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            CreatedBy = session.CurrentUser!.UserId,
            ReceiptDate = DateTime.Now,
            Status = "DRAFT",
            Note = request.Note?.Trim(),
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
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem phiếu nhập.");
        }
        var accessibleWarehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        var receipt = await _dbContext.ImportReceipts
            .AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.Warehouse)
            .Include(item => item.Creator)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Product)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId && accessibleWarehouseIds.Contains(item.WarehouseId), cancellationToken);
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
                    detail.Quantity * detail.UnitPrice)).ToList());
    }

    public async Task SubmitAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi phiếu nhập.");
        }
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được gửi duyệt.");
        }
        receipt.Status = "PENDING_APPROVAL";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDraftAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanDeleteDraftReceiptAsync(session, receipt.WarehouseId))
            throw new UnauthorizedAccessException("Bạn không có quyền xóa phiếu nhập nháp tại kho này.");
        if (receipt.Status != "DRAFT")
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được xóa.");

        _dbContext.ImportDetails.RemoveRange(receipt.Details);
        _dbContext.ImportReceipts.Remove(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền duyệt phiếu nhập tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập đang chờ duyệt mới được duyệt.");
        }
        receipt.Status = "APPROVED";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        UserSession session,
        int importReceiptId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Lý do từ chối là bắt buộc.", nameof(reason));
        }
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanApproveReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền từ chối phiếu nhập tại kho này.");
        }
        if (receipt.Status != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập đang chờ duyệt mới được từ chối.");
        }
        receipt.Status = "REJECTED";
        receipt.Note = $"Lý do từ chối: {reason.Trim()}";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResubmitAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts.SingleOrDefaultAsync(
            item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền gửi lại phiếu nhập.");
        }
        if (receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập bị từ chối mới được gửi lại.");
        }
        receipt.Status = "DRAFT";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts
            .Include(importReceipt => importReceipt.Details)
            .SingleOrDefaultAsync(importReceipt => importReceipt.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");

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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var alreadyPosted = await _dbContext.InventoryTransactions.AnyAsync(
            item => item.ReferenceType == InventoryTransactionTypes.ImportReceipt &&
                item.ReferenceId == importReceiptId,
            cancellationToken);
        if (alreadyPosted)
            throw new InvalidOperationException("Phiếu nhập đã được ghi nhận tồn kho trước đó.");

        foreach (var detail in receipt.Details)
        {
            var inventory = await GetOrCreateInventoryAsync(receipt.WarehouseId, detail.ProductId, cancellationToken);
            inventory.Quantity = checked(inventory.Quantity + detail.Quantity);
            inventory.UpdatedAt = DateTime.Now;
            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = detail.ProductId,
                WarehouseId = receipt.WarehouseId,
                TransactionType = InventoryTransactionTypes.Import,
                Quantity = detail.Quantity,
                ReferenceType = InventoryTransactionTypes.ImportReceipt,
                ReferenceId = importReceiptId,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = DateTime.Now,
                Note = receipt.Note
            });
        }

        receipt.TotalAmount = receipt.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        receipt.Status = "COMPLETED";
        var alertService = new StockAlertService(_dbContext, _authorization);
        foreach (var productId in receipt.Details.Select(detail => detail.ProductId).Distinct())
            await alertService.RefreshAsync(productId, receipt.WarehouseId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateDraftAsync(
        UserSession session,
        int importReceiptId,
        ImportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var receipt = await _dbContext.ImportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (receipt.Status != "DRAFT" && receipt.Status != "REJECTED")
        {
            throw new InvalidOperationException("Chỉ phiếu nhập nháp mới được sửa.");
        }
        if (!await _authorization.CanEditReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa phiếu nhập tại kho này.");
        }
        ValidateRequest(request);
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
        receipt.ReceiptCode = request.ReceiptCode.Trim();
        receipt.SupplierId = request.SupplierId;
        receipt.Note = request.Note?.Trim();
        _dbContext.ImportDetails.RemoveRange(receipt.Details);
        receipt.Details = request.Details.Select(detail => new ImportDetail
        {
            ProductId = detail.ProductId,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice
        }).ToList();
        receipt.TotalAmount = request.Details.Sum(detail => detail.Quantity * detail.UnitPrice);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelDraftAsync(
        UserSession session,
        int importReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ImportReceipts
            .SingleOrDefaultAsync(item => item.ImportReceiptId == importReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        if (!await _authorization.CanCancelReceiptAsync(session, receipt.WarehouseId, receipt.CreatedBy, receipt.Status))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy phiếu nhập tại kho này.");
        }
        if (receipt.Status != "DRAFT" && receipt.Status != "PENDING_APPROVAL" && receipt.Status != "APPROVED")
        {
            throw new InvalidOperationException("Phiếu nhập không ở trạng thái được phép hủy.");
        }
        receipt.Status = "CANCELLED";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<InventoryEntity> GetOrCreateInventoryAsync(int warehouseId, int productId, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.Inventories
            .SingleOrDefaultAsync(item => item.WarehouseId == warehouseId && item.ProductId == productId, cancellationToken);
        if (inventory is not null)
        {
            return inventory;
        }

        inventory = new InventoryEntity
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = 0,
            UpdatedAt = DateTime.Now
        };
        _dbContext.Inventories.Add(inventory);
        return inventory;
    }

    private void ValidateRequest(ImportReceiptDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiptCode) || request.Details.Count == 0 ||
            request.Details.Select(detail => detail.ProductId).Distinct().Count() != request.Details.Count)
        {
            throw new ArgumentException("Mã phiếu và chi tiết phiếu nhập là bắt buộc; sản phẩm không được trùng.", nameof(request));
        }
        var errors = request.Details
            .SelectMany(detail => _validator.ValidateDetail(detail.ProductId, detail.Quantity, detail.UnitPrice))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }
    }
}
