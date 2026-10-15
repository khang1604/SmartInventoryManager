namespace SmartInventoryManager.Data.Entities;

public sealed class ReceiptStatusChange
{
    public long ReceiptStatusChangeId { get; set; }
    public int? ImportReceiptId { get; set; }
    public int? ExportReceiptId { get; set; }
    public string FromStatus { get; set; } = null!;
    public string ToStatus { get; set; } = null!;
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public User Actor { get; set; } = null!;
}
