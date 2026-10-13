namespace SmartInventoryManager.Data.Entities
{
    public class Warehouse
    {
        public int WarehouseId { get; set; }

        public string WarehouseCode { get; set; } = null!;

        public string WarehouseName { get; set; } = null!;

        public string? Location { get; set; }

        public int? ManagerId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? Manager { get; set; }


        public ICollection<UserWarehouse> UserWarehouses { get; set; }
            = new List<UserWarehouse>();


        public ICollection<Location> Locations { get; set; }
            = new List<Location>();

        public ICollection<Inventory> Inventories { get; set; }
            = new List<Inventory>();

        public ICollection<ImportReceipt> ImportReceipts { get; set; }
            = new List<ImportReceipt>();

        public ICollection<ExportReceipt> ExportReceipts { get; set; }
            = new List<ExportReceipt>();

        public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
            = new List<InventoryTransaction>();

        public ICollection<StockAlert> StockAlerts { get; set; }
            = new List<StockAlert>();
    }
}