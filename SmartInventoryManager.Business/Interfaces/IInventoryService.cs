using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryDto>> GetByWarehouseAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryHistoryDto>> GetHistoryAsync(UserSession session, int warehouseId, int? productId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    Task AdjustAsync(UserSession session, int warehouseId, int productId, int quantityDelta, string reason, CancellationToken cancellationToken = default);

    Task TransferAsync(UserSession session, int sourceWarehouseId, int destinationWarehouseId, int productId, int quantity, string note, CancellationToken cancellationToken = default);
}
