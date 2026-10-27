using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IInventoryService
{
    // Lấy tồn kho theo kho và phạm vi truy cập của người dùng.
    Task<IReadOnlyList<InventoryDto>> GetByWarehouseAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default);

    // Tra cứu lịch sử biến động tồn theo điều kiện lọc và quyền truy cập.
    Task<IReadOnlyList<InventoryHistoryDto>> GetHistoryAsync(UserSession session, int warehouseId, int? productId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    // Điều chỉnh tồn có kiểm tra quyền và cơ chế thử lại khi gặp deadlock.
    Task AdjustAsync(UserSession session, int warehouseId, int productId, int quantityDelta, string reason, CancellationToken cancellationToken = default);

    // Chuyển hàng giữa hai kho với cơ chế thử lại khi gặp deadlock.
    Task TransferAsync(UserSession session, int sourceWarehouseId, int destinationWarehouseId, int productId, int quantity, string note, CancellationToken cancellationToken = default);
}
