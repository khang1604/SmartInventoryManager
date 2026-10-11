using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;
using InventoryEntity = SmartInventoryManager.Data.Entities.Inventory;

namespace SmartInventoryManager.Business.Services;

public sealed class InventoryService : IInventoryService
{
    private const int MaxReasonLength = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly StockMutationService _stockMutation;

    public InventoryService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _stockMutation = new StockMutationService(_dbContext);
    }

    public event EventHandler<InventoryChangedEvent>? InventoryChanged;

    public async Task<IReadOnlyList<InventoryDto>> GetByWarehouseAsync(
        UserSession session,
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        if (!await _authorization.CanViewWarehouseAsync(session, warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem tồn kho tại kho này.");
        }

        return await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.WarehouseId == warehouseId)
            .OrderBy(inventory => inventory.Product.ProductCode)
            .Select(inventory => new InventoryDto(
                inventory.InventoryId,
                inventory.ProductId,
                inventory.Product.ProductCode,
                inventory.Product.ProductName,
                inventory.Product.Unit,
                inventory.WarehouseId,
                inventory.Quantity,
                inventory.Product.ReorderPoint,
                inventory.Product.MaximumStock,
                StockStatusCalculator.Calculate(
                    inventory.Quantity,
                    inventory.Product.ReorderPoint,
                    inventory.Product.MaximumStock),
                inventory.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryHistoryDto>> GetHistoryAsync(
        UserSession session,
        int warehouseId,
        int? productId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        if (productId.HasValue && productId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
        {
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
        }
        if (!await _authorization.CanViewWarehouseAsync(session, warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem lịch sử tồn kho tại kho này.");
        }

        var query = _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(transaction => transaction.WarehouseId == warehouseId)
            .AsQueryable();
        if (productId.HasValue)
        {
            query = query.Where(transaction => transaction.ProductId == productId.Value);
        }
        if (from.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= from.Value.Date);
        }
        if (to.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt < to.Value.Date.AddDays(1));
        }
        return await query
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new InventoryHistoryDto(
                transaction.TransactionId,
                transaction.ProductId,
                transaction.Product.ProductCode,
                transaction.Product.ProductName,
                transaction.WarehouseId,
                transaction.TransactionType,
                transaction.Quantity,
                transaction.ReferenceType,
                transaction.ReferenceId,
                transaction.Creator.FullName,
                transaction.CreatedAt,
                transaction.Note))
            .ToListAsync(cancellationToken);
    }

    public async Task AdjustAsync(
        UserSession session,
        int warehouseId,
        int productId,
        int quantityDelta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        ValidateProductId(productId);
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Số lượng điều chỉnh phải khác 0.");
        }
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength)
        {
            throw new ArgumentException("Lý do điều chỉnh là bắt buộc và không quá 500 ký tự.", nameof(reason));
        }
        if (!await _authorization.CanAdjustInventoryAsync(session, warehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền điều chỉnh tồn kho tại kho này.");
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var productExists = await _dbContext.Products.AnyAsync(
            product => product.ProductId == productId && product.IsActive,
            cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }

        var inventory = await _stockMutation.GetOrCreateAsync(warehouseId, productId, cancellationToken);
        var newQuantity = checked(inventory.Quantity + quantityDelta);
        if (newQuantity < 0)
        {
            throw new InvalidOperationException("Số lượng điều chỉnh không được làm tồn kho âm.");
        }
        inventory.Quantity = newQuantity;
        inventory.UpdatedAt = DateTime.Now;
        _dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            TransactionType = InventoryTransactionTypes.Adjustment,
            Quantity = Math.Abs(quantityDelta),
            ReferenceType = InventoryTransactionTypes.Manual,
            CreatedBy = session.CurrentUser!.UserId,
            CreatedAt = DateTime.Now,
            Note = reason.Trim()
        });
        await new StockAlertService(_dbContext, _authorization)
            .RefreshAsync(productId, warehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        InventoryChanged?.Invoke(this, new InventoryChangedEvent(productId, warehouseId));
    }

    public async Task TransferAsync(
        UserSession session,
        int sourceWarehouseId,
        int destinationWarehouseId,
        int productId,
        int quantity,
        string note,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(sourceWarehouseId);
        ValidateWarehouseId(destinationWarehouseId);
        ValidateProductId(productId);
        if (sourceWarehouseId == destinationWarehouseId)
        {
            throw new ArgumentException("Kho nguồn và kho đích phải khác nhau.");
        }
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng chuyển phải lớn hơn 0.");
        }
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Lý do chuyển kho là bắt buộc.", nameof(note));
        }
        if (note.Trim().Length > 500)
        {
            throw new ArgumentException("Ghi chú chuyển kho không quá 500 ký tự.", nameof(note));
        }
        if (!await _authorization.CanTransferInventoryAsync(session, sourceWarehouseId, destinationWarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền chuyển hàng giữa hai kho này.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var productExists = await _dbContext.Products.AnyAsync(
            product => product.ProductId == productId && product.IsActive,
            cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }
        var source = await _dbContext.Inventories
            .SingleOrDefaultAsync(item => item.WarehouseId == sourceWarehouseId && item.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("Sản phẩm chưa có tồn tại kho nguồn.");
        if (source.Quantity < quantity)
        {
            throw new InvalidOperationException("Tồn kho tại kho nguồn không đủ để chuyển.");
        }
        await _stockMutation.DecreaseAsync(sourceWarehouseId, productId, quantity, cancellationToken);
        var destination = await _stockMutation.IncreaseAsync(destinationWarehouseId, productId, quantity, cancellationToken);
        var now = DateTime.Now;
        _dbContext.InventoryTransactions.AddRange(
            new InventoryTransaction
            {
                ProductId = productId,
                WarehouseId = sourceWarehouseId,
                TransactionType = InventoryTransactionTypes.TransferOut,
                Quantity = quantity,
                ReferenceType = InventoryTransactionTypes.Manual,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = now,
                Note = note.Trim()
            },
            new InventoryTransaction
            {
                ProductId = productId,
                WarehouseId = destinationWarehouseId,
                TransactionType = InventoryTransactionTypes.TransferIn,
                Quantity = quantity,
                ReferenceType = InventoryTransactionTypes.Manual,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = now,
                Note = note.Trim()
            });
        var alertService = new StockAlertService(_dbContext, _authorization);
        await alertService.RefreshAsync(productId, sourceWarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await alertService.RefreshAsync(productId, destinationWarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        InventoryChanged?.Invoke(this, new InventoryChangedEvent(productId, sourceWarehouseId));
        InventoryChanged?.Invoke(this, new InventoryChangedEvent(productId, destinationWarehouseId));
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
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 0,
            UpdatedAt = DateTime.Now
        };
        _dbContext.Inventories.Add(inventory);
        return inventory;
    }

    private static void ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }

    private static void ValidateWarehouseId(int warehouseId)
    {
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
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
