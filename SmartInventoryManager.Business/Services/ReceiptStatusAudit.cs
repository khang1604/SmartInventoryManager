using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

// Called only after the workflow has validated actor, scope and transition.
// SaveChanges/transaction ownership remains with the calling receipt service.
internal static class ReceiptStatusAudit
{
    // Cập nhật trạng thái phiếu và thêm bản ghi lịch sử người thực hiện, thời điểm, lý do.
    internal static void Change(InventoryDbContext db, UserSession session, ImportReceipt receipt, string next, string? reason = null)
    {
        var change = Create(session, receipt.Status, next, reason);
        change.ImportReceiptId = receipt.ImportReceiptId;
        receipt.Status = next;
        switch (next)
        {
            case "PENDING_APPROVAL": receipt.SubmittedBy = change.ChangedBy; receipt.SubmittedAt = change.ChangedAt; break;
            case "APPROVED": receipt.ApprovedBy = change.ChangedBy; receipt.ApprovedAt = change.ChangedAt; break;
            case "CANCELLED": receipt.CancelledBy = change.ChangedBy; receipt.CancelledAt = change.ChangedAt; break;
            case "COMPLETED": receipt.CompletedBy = change.ChangedBy; receipt.CompletedAt = change.ChangedAt; break;
        }
        db.ReceiptStatusChanges.Add(change);
    }

    // Cập nhật trạng thái phiếu và thêm bản ghi lịch sử người thực hiện, thời điểm, lý do.
    internal static void Change(InventoryDbContext db, UserSession session, ExportReceipt receipt, string next, string? reason = null)
    {
        var change = Create(session, receipt.Status, next, reason);
        change.ExportReceiptId = receipt.ExportReceiptId;
        receipt.Status = next;
        switch (next)
        {
            case "PENDING_APPROVAL": receipt.SubmittedBy = change.ChangedBy; receipt.SubmittedAt = change.ChangedAt; break;
            case "APPROVED": receipt.ApprovedBy = change.ChangedBy; receipt.ApprovedAt = change.ChangedAt; break;
            case "CANCELLED": receipt.CancelledBy = change.ChangedBy; receipt.CancelledAt = change.ChangedAt; break;
            case "COMPLETED": receipt.CompletedBy = change.ChangedBy; receipt.CompletedAt = change.ChangedAt; break;
        }
        db.ReceiptStatusChanges.Add(change);
    }

    // Tạo bản ghi lịch sử cho trạng thái ban đầu của phiếu.
    private static ReceiptStatusChange Create(UserSession session, string previous, string next, string? reason)
    {
        if (previous == next) throw new InvalidOperationException("Trạng thái phiếu không thay đổi.");
        return new ReceiptStatusChange
        {
            FromStatus = previous, ToStatus = next, ChangedBy = session.CurrentUser!.UserId,
            ChangedAt = DateTime.Now, Reason = reason?.Trim()
        };
    }
}
