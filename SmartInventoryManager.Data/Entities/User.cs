namespace SmartInventoryManager.Data.Entities
{
    public class User
    {
        public int UserId { get; set; }

        public string Username { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Email { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Role Role { get; set; } = null!;

        // User quản lý Warehouse
        public ICollection<Warehouse> ManagedWarehouses { get; set; }
            = new List<Warehouse>();

        // User tạo ImportReceipt
        public ICollection<ImportReceipt> CreatedImportReceipts { get; set; }
            = new List<ImportReceipt>();

        // User tạo ExportReceipt
        public ICollection<ExportReceipt> CreatedExportReceipts { get; set; }
            = new List<ExportReceipt>();

        // User tạo InventoryTransaction
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
            = new List<InventoryTransaction>();

        // User resolve StockAlert
        public ICollection<StockAlert> ResolvedStockAlerts { get; set; }
            = new List<StockAlert>();

        // User nhận Notification
        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        // User thực hiện SystemLog
        public ICollection<SystemLog> SystemLogs { get; set; }
            = new List<SystemLog>();
    }
}