using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IProductService
{
    Task<PagedResultDto<ProductDto>> SearchAsync(
        UserSession session,
        ProductSearchDto search,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(
        UserSession session,
        int productId,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        UserSession session,
        ProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserSession session,
        int productId,
        ProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        UserSession session,
        int productId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
