namespace SmartInventoryManager.Data.Entities;

public sealed class ReceiptRejection
{
    public long ReceiptRejectionId { get; set; }
    public int? ImportReceiptId { get; set; }
    public int? ExportReceiptId { get; set; }
    public int RejectedBy { get; set; }
    public DateTime RejectedAt { get; set; }
    public string Reason { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
