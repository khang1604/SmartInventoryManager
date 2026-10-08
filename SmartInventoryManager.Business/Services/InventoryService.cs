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
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public InventoryService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public event EventHandler<InventoryChangedEvent>? InventoryChanged;

    public async Task<IReadOnlyList<InventoryDto>> GetByWarehouseAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.WarehouseId == warehouseId)
            .OrderBy(inventory => inventory.Product.ProductCode)
            .Select(inventory => new InventoryDto(
                inventory.InventoryId,
                inventory.ProductId,
                inventory.Product.ProductCode,
                inventory.Product.ProductName,
                inventory.WarehouseId,
                inventory.Quantity,
                inventory.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryHistoryDto>> GetHistoryAsync(
        int warehouseId,
        int? productId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
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
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Số lượng điều chỉnh phải khác 0.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Lý do điều chỉnh là bắt buộc.", nameof(reason));
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

        var inventory = await GetOrCreateInventoryAsync(warehouseId, productId, cancellationToken);
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
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await new StockAlertService(_dbContext, _authorization)
            .RefreshAsync(productId, warehouseId, cancellationToken);
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
        var destination = await GetOrCreateInventoryAsync(destinationWarehouseId, productId, cancellationToken);
        source.Quantity -= quantity;
        source.UpdatedAt = DateTime.Now;
        destination.Quantity = checked(destination.Quantity + quantity);
        destination.UpdatedAt = DateTime.Now;
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
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var alertService = new StockAlertService(_dbContext, _authorization);
        await alertService.RefreshAsync(productId, sourceWarehouseId, cancellationToken);
        await alertService.RefreshAsync(productId, destinationWarehouseId, cancellationToken);
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
}
