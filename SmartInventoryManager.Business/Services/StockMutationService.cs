using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Data.Context;
using StockInventory = SmartInventoryManager.Data.Entities.Inventory;

namespace SmartInventoryManager.Business.Services;

public sealed class StockMutationService
{
    private readonly InventoryDbContext _dbContext;

    public StockMutationService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<StockInventory> GetOrCreateAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _dbContext.Inventories.SingleOrDefaultAsync(
            item => item.WarehouseId == warehouseId && item.ProductId == productId,
            cancellationToken);
        if (inventory is not null)
        {
            return inventory;
        }

        inventory = new StockInventory
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 0,
            UpdatedAt = DateTime.Now
        };
        _dbContext.Inventories.Add(inventory);
        return inventory;
    }

    public async Task<StockInventory> IncreaseAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity);
        var inventory = await GetOrCreateAsync(warehouseId, productId, cancellationToken);
        inventory.Quantity = checked(inventory.Quantity + quantity);
        inventory.UpdatedAt = DateTime.Now;
        return inventory;
    }

    public async Task<StockInventory> DecreaseAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity);
        var inventory = await _dbContext.Inventories.SingleOrDefaultAsync(
            item => item.WarehouseId == warehouseId && item.ProductId == productId,
            cancellationToken)
            ?? throw new InvalidOperationException("Sản phẩm chưa có tồn kho tại kho.");
        if (inventory.Quantity < quantity)
        {
            throw new InvalidOperationException("Tồn kho không đủ để thực hiện thao tác.");
        }

        inventory.Quantity -= quantity;
        inventory.UpdatedAt = DateTime.Now;
        return inventory;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải lớn hơn 0.");
        }
    }
}
