namespace SmartInventoryManager.Data.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = null!;

        public string? Description { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; }
            = new List<User>();
    }
}