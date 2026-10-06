namespace SmartInventoryManager.Data.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}