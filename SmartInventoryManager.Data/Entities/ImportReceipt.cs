namespace SmartInventoryManager.Data.Entities
{
    public class ImportReceipt
    {
        public int ImportReceiptId { get; set; }

        public string ReceiptCode { get; set; } = null!;

        public int SupplierId { get; set; }

        public int WarehouseId { get; set; }

        public int CreatedBy { get; set; }

        public DateTime ReceiptDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string? Note { get; set; }
        public int? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? CancelledBy { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public string? RejectReason { get; set; }

        // Navigation
        public Supplier Supplier { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;

        public User Creator { get; set; } = null!;

        public ICollection<ImportDetail> Details { get; set; }
            = new List<ImportDetail>();
    }
}
