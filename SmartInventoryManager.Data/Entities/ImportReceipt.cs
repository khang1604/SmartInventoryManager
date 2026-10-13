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

        // Navigation
        public Supplier Supplier { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;

        public User Creator { get; set; } = null!;

        public ICollection<ImportDetail> Details { get; set; }
            = new List<ImportDetail>();
    }
}