namespace SmartInventoryManager.Data.Entities
{
    public class Notification
    {
        public long NotificationId { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = null!;

        public string Message { get; set; } = null!;

        public string? NotificationType { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}