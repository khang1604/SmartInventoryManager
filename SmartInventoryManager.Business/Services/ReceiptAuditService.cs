using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class ReceiptAuditService(InventoryDbContext db, AuthorizationService authorization)
{
    public async Task<ReceiptRejectionPageDto> GetRejectionsAsync(UserSession session, bool import, int receiptId, int page = 1, CancellationToken ct = default)
    {
        if (receiptId <= 0 || page <= 0 || page > int.MaxValue / 20) throw new ArgumentOutOfRangeException(nameof(page));
        var scope = await authorization.GetAccessibleWarehouseIdsAsync(session, ct);
        var allowed = import
            ? await db.ImportReceipts.AnyAsync(x => x.ImportReceiptId == receiptId && scope.Contains(x.WarehouseId), ct)
            : await db.ExportReceipts.AnyAsync(x => x.ExportReceiptId == receiptId && scope.Contains(x.WarehouseId), ct);
        if (!allowed) throw new UnauthorizedAccessException("Không có quyền xem lịch sử phiếu này.");
        var query = db.ReceiptRejections.AsNoTracking().Where(x => import ? x.ImportReceiptId == receiptId : x.ExportReceiptId == receiptId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.RejectedAt).ThenByDescending(x => x.ReceiptRejectionId).Skip((page - 1) * 20).Take(20)
            .Select(x => new ReceiptRejectionDto(x.ReceiptRejectionId, x.Actor.FullName, x.RejectedAt, x.Reason)).ToListAsync(ct);
        return new(total, page, 20, rows);
    }
}
