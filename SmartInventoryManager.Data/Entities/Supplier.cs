namespace SmartInventoryManager.Data.Entities
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        public string SupplierCode { get; set; } = null!;

        public string SupplierName { get; set; } = null!;

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? TaxCode { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<ImportReceipt> ImportReceipts { get; set; }
            = new List<ImportReceipt>();
    }
}