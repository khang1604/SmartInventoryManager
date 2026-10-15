namespace SmartInventoryManager.Data.Entities
{
    public class InventoryTransaction
    {
        public long TransactionId { get; set; }

        public int ProductId { get; set; }

        public int WarehouseId { get; set; }

        public string TransactionType { get; set; } = null!;

        public int Quantity { get; set; }

        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Note { get; set; }
        // Existing movements remain unknown; never fabricate historical quantities.
        public int? BeforeQuantity { get; set; }
        public int? AfterQuantity { get; set; }
        public int? SignedDifference { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;

        public User Creator { get; set; } = null!;
    }
}
