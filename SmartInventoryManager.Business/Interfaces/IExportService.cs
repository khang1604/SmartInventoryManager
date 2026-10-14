using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Interfaces;

public interface IExportService
{
    Task<int> CreateDraftAsync(UserSession session, ExportReceiptDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportReceiptListItemDto>> GetListAsync(
        UserSession session,
        int? warehouseId = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ExportReceiptDetailsDto?> GetByIdAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task SubmitAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task DeleteDraftAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task ApproveAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task RejectAsync(UserSession session, int exportReceiptId, string reason, CancellationToken cancellationToken = default);
    Task ResubmitAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task CompleteAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task UpdateDraftAsync(UserSession session, int exportReceiptId, ExportReceiptDto request, CancellationToken cancellationToken = default);
    Task CancelDraftAsync(UserSession session, int exportReceiptId, CancellationToken cancellationToken = default);
    Task CancelAsync(UserSession session, int exportReceiptId, string reason, CancellationToken cancellationToken = default);
}
