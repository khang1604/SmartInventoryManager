using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IExportService
{
    // Lấy một trang phiếu xuất theo bộ lọc và phạm vi truy cập.
    Task<PagedResultDto<ExportReceiptListItemDto>> GetPageAsync(UserSession session, ReceiptSearchDto search, CancellationToken cancellationToken = default);
    // Tạo phiếu xuất nháp, tính tổng tiền và cấp mã khi cần.
    Task<int> CreateDraftAsync(UserSession session, ExportReceiptDto request, CancellationToken cancellationToken = default);

    // Lấy danh sách phiếu xuất theo quyền và điều kiện lọc.
    Task<IReadOnlyList<ExportReceiptListItemDto>> GetListAsync(
        UserSession session,
        int? warehouseId = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    // Lấy thông tin chi tiết phiếu xuất theo mã định danh.
    Task<ExportReceiptDetailsDto?> GetByIdAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Kiểm tra dữ liệu rồi chuyển phiếu nháp sang trạng thái chờ duyệt.
    Task SubmitAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Xóa phiếu nháp khi quyền, trạng thái và lịch sử cho phép.
    Task DeleteDraftAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Duyệt phiếu đang chờ xử lý và ghi lịch sử chuyển trạng thái.
    Task ApproveAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Từ chối phiếu và lưu lý do cùng lịch sử xử lý.
    Task RejectAsync(UserSession session, int exportReceiptId, string reason, CancellationToken cancellationToken = default);
    // Đưa phiếu bị từ chối về trạng thái cho phép sửa và gửi lại.
    Task ResubmitAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Hoàn tất phiếu hợp lệ và ghi nhận thay đổi tồn kho.
    Task CompleteAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Cập nhật thông tin và các dòng hàng của phiếu được phép chỉnh sửa.
    Task UpdateDraftAsync(UserSession session, int exportReceiptId, ExportReceiptDto request, CancellationToken cancellationToken = default);
    // Chuyển yêu cầu hủy phiếu nháp sang hàm xử lý hủy chung.
    Task CancelDraftAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    // Hủy phiếu hợp lệ và lưu người thực hiện, thời điểm cùng lý do.
    Task CancelAsync(UserSession session, int exportReceiptId, string reason, CancellationToken cancellationToken = default);
}
