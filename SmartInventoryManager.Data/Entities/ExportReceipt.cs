namespace SmartInventoryManager.Data.Entities
{
    public class ExportReceipt
    {
        public int ExportReceiptId { get; set; }

        public string ReceiptCode { get; set; } = null!;

        public int WarehouseId { get; set; }

        public int CreatedBy { get; set; }

        public DateTime ReceiptDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string? Note { get; set; }
        public int? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancelReason { get; set; }
        public string? RejectReason { get; set; }

        // Navigation
        public Warehouse Warehouse { get; set; } = null!;

        public User Creator { get; set; } = null!;

        public ICollection<ExportDetail> Details { get; set; }
            = new List<ExportDetail>();
    }
}
