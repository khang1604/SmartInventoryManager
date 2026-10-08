using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Business.Validators;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class ExportService
{
    private const int MaxListResults = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly ExportValidator _validator = new();

    public ExportService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<int> CreateDraftAsync(
        UserSession session,
        ExportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await _authorization.CanCreateDraftReceiptAsync(session, request.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền tạo phiếu xuất tại kho này.");
        }
        ValidateRequest(request);

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
            ReceiptCode = request.ReceiptCode.Trim(),
            WarehouseId = request.WarehouseId,
            CreatedBy = session.CurrentUser!.UserId,
            ReceiptDate = DateTime.Now,
            Status = "DRAFT",
            Note = request.Note?.Trim(),
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
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem phiếu xuất.");
        }
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
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem phiếu xuất.");
        }
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

    public async Task CompleteAsync(
        UserSession session,
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ExportReceipts
            .Include(exportReceipt => exportReceipt.Details)
            .SingleOrDefaultAsync(exportReceipt => exportReceipt.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        if (!await _authorization.CanCompleteReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hoàn tất phiếu xuất tại kho này.");
        }
        if (receipt.Status == "COMPLETED")
        {
            return;
        }
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được hoàn tất.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var alreadyPosted = await _dbContext.InventoryTransactions.AnyAsync(
            item => item.ReferenceType == InventoryTransactionTypes.ExportReceipt &&
                item.ReferenceId == exportReceiptId,
            cancellationToken);
        if (alreadyPosted)
        {
            receipt.Status = "COMPLETED";
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        foreach (var detail in receipt.Details)
        {
            var inventory = await _dbContext.Inventories
                .SingleOrDefaultAsync(item => item.WarehouseId == receipt.WarehouseId && item.ProductId == detail.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Sản phẩm {detail.ProductId} chưa có tồn kho tại kho xuất.");
            if (inventory.Quantity < detail.Quantity)
            {
                throw new InvalidOperationException($"Sản phẩm {detail.ProductId} không đủ tồn kho để xuất.");
            }
            inventory.Quantity -= detail.Quantity;
            inventory.UpdatedAt = DateTime.Now;
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
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var alertService = new StockAlertService(_dbContext, _authorization);
        foreach (var productId in receipt.Details.Select(detail => detail.ProductId).Distinct())
        {
            await alertService.RefreshAsync(productId, receipt.WarehouseId, cancellationToken);
        }
    }

    public async Task UpdateDraftAsync(
        UserSession session,
        int exportReceiptId,
        ExportReceiptDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var receipt = await _dbContext.ExportReceipts
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được sửa.");
        }
        if (!await _authorization.CanEditDraftReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa phiếu xuất tại kho này.");
        }
        ValidateRequest(request);
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
        receipt.ReceiptCode = request.ReceiptCode.Trim();
        receipt.Note = request.Note?.Trim();
        _dbContext.ExportDetails.RemoveRange(receipt.Details);
        receipt.Details = request.Details.Select(detail => new ExportDetail
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
        int exportReceiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ExportReceipts
            .SingleOrDefaultAsync(item => item.ExportReceiptId == exportReceiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu xuất.");
        if (!await _authorization.CanCancelReceiptAsync(session, receipt.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy phiếu xuất tại kho này.");
        }
        if (receipt.Status != "DRAFT")
        {
            throw new InvalidOperationException("Chỉ phiếu xuất nháp mới được hủy.");
        }
        receipt.Status = "CANCELLED";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ValidateRequest(ExportReceiptDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiptCode) || request.Details.Count == 0 ||
            request.Details.Select(detail => detail.ProductId).Distinct().Count() != request.Details.Count)
        {
            throw new ArgumentException("Mã phiếu và chi tiết phiếu xuất là bắt buộc; sản phẩm không được trùng.", nameof(request));
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
