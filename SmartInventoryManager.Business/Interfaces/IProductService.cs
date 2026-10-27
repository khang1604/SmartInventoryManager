using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IProductService
{
    // Tìm kiếm và phân trang sản phẩm theo các điều kiện được cung cấp.
    Task<PagedResultDto<ProductDto>> SearchAsync(
        UserSession session,
        ProductSearchDto search,
        CancellationToken cancellationToken = default);

    // Lấy thông tin chi tiết sản phẩm theo mã định danh.
    Task<ProductDto?> GetByIdAsync(
        UserSession session,
        int productId,
        CancellationToken cancellationToken = default);

    // Kiểm tra dữ liệu rồi tạo mới sản phẩm.
    Task<int> CreateAsync(
        UserSession session,
        ProductRequestDto request,
        CancellationToken cancellationToken = default);

    // Kiểm tra dữ liệu rồi cập nhật thông tin sản phẩm.
    Task UpdateAsync(
        UserSession session,
        int productId,
        ProductRequestDto request,
        CancellationToken cancellationToken = default);

    // Đổi trạng thái hoạt động của sản phẩm sau khi kiểm tra điều kiện liên quan.
    Task SetActiveAsync(
        UserSession session,
        int productId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
