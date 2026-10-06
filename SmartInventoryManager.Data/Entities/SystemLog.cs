namespace SmartInventoryManager.Data.Entities
{
    public class SystemLog
    {
        public long LogId { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = null!;

        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? User { get; set; }
    }
}