namespace SmartInventoryManager.Data.Entities
{
    public class ImportDetail
    {
        public int ImportDetailId { get; set; }

        public int ImportReceiptId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // Navigation
        public ImportReceipt ImportReceipt { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}