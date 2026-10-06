namespace SmartInventoryManager.Data.Entities
{
    public class Product
    {
        public int ProductId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int CategoryId { get; set; }

        public string Unit { get; set; } = null!;

        public decimal PurchasePrice { get; set; }

        public decimal SellingPrice { get; set; }

        public int ReorderPoint { get; set; }

        public int MaximumStock { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Category Category { get; set; } = null!;

        public ICollection<Inventory> Inventories { get; set; }
            = new List<Inventory>();

        public ICollection<ImportDetail> ImportDetails { get; set; }
            = new List<ImportDetail>();

        public ICollection<ExportDetail> ExportDetails { get; set; }
            = new List<ExportDetail>();

        public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
            = new List<InventoryTransaction>();

        public ICollection<StockAlert> StockAlerts { get; set; }
            = new List<StockAlert>();
    }
}