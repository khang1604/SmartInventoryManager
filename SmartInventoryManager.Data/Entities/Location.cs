namespace SmartInventoryManager.Data.Entities
{
    public class Location
    {
        public int LocationId { get; set; }

        public int WarehouseId { get; set; }

        public string LocationCode { get; set; } = null!;

        public string? LocationName { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public Warehouse Warehouse { get; set; } = null!;
    }
}