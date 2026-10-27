using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IStockAlertService
{
    // Lấy các cảnh báo tồn kho chưa được xử lý trong phạm vi cho phép.
    Task<IReadOnlyList<StockAlertDto>> GetOpenAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default);

    // Lấy các cảnh báo tồn kho đã được xử lý trong phạm vi cho phép.
    Task<IReadOnlyList<StockAlertDto>> GetResolvedAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default);

    // Đối chiếu tồn kho để cập nhật cảnh báo theo tùy chọn lưu và phát sự kiện.
    Task RefreshAsync(
        int productId,
        int warehouseId,
        CancellationToken cancellationToken = default,
        bool persistChanges = true,
        bool publishEvent = true);

    // Ghi nhận người dùng đã xử lý cảnh báo tồn kho.
    Task ResolveAsync(UserSession session, long alertId, CancellationToken cancellationToken = default);
}
