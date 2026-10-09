namespace SmartInventoryManager.Data.Entities;

public sealed class UserWarehouse
{
    public int UserId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
