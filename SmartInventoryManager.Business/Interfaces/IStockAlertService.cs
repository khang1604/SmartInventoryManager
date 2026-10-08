using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IStockAlertService
{
    Task<IReadOnlyList<StockAlertDto>> GetOpenAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default);

    Task RefreshAsync(int productId, int warehouseId, CancellationToken cancellationToken = default);

    Task ResolveAsync(UserSession session, long alertId, CancellationToken cancellationToken = default);
}
