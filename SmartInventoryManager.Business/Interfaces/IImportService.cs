using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IImportService
{
    Task<int> CreateDraftAsync(UserSession session, ImportReceiptDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportReceiptListItemDto>> GetListAsync(
        UserSession session,
        int? warehouseId = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ImportReceiptDetailsDto?> GetByIdAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task SubmitAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task DeleteDraftAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task ApproveAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task RejectAsync(UserSession session, int importReceiptId, string reason, CancellationToken cancellationToken = default);
    Task ResubmitAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task CompleteAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task UpdateDraftAsync(UserSession session, int importReceiptId, ImportReceiptDto request, CancellationToken cancellationToken = default);
    Task CancelDraftAsync(UserSession session, int importReceiptId, CancellationToken cancellationToken = default);
    Task CancelAsync(UserSession session, int importReceiptId, string reason, CancellationToken cancellationToken = default);
}
