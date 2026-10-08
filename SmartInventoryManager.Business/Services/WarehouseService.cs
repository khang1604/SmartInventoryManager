using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class WarehouseService
{
    private readonly InventoryDbContext _dbContext;

    public WarehouseService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.IsActive)
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Select(warehouse => new LookupItemDto(warehouse.WarehouseId, warehouse.WarehouseCode, warehouse.WarehouseName))
            .ToListAsync(cancellationToken);
    }
}
