
namespace SmartInventoryManager.Data.Entities
{
    public class UserWarehouse
    {
        // Foreign keys
        public int UserId { get; set; }

        public int WarehouseId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;
    }
}
