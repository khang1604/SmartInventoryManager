namespace SmartInventoryManager.Data.Entities
{
    public class StockAlert
    {
        public long AlertId { get; set; }

        public int ProductId { get; set; }

        public int WarehouseId { get; set; }

        public string AlertType { get; set; } = null!;

        public int CurrentQuantity { get; set; }

        public int? Threshold { get; set; }

        public bool IsResolved { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedBy { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;

        public User? Resolver { get; set; }
    }
}