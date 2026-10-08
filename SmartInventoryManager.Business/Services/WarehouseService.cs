using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class WarehouseService
{
    private const int MaxLookupResults = 200;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public WarehouseService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để tra cứu kho.");
        }
        var warehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return await _dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.IsActive && warehouseIds.Contains(warehouse.WarehouseId))
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Take(MaxLookupResults)
            .Select(warehouse => new LookupItemDto(warehouse.WarehouseId, warehouse.WarehouseCode, warehouse.WarehouseName))
            .ToListAsync(cancellationToken);
    }
}
