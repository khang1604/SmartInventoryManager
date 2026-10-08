namespace SmartInventoryManager.Business.Events;

public sealed class InventoryChangedEvent : EventArgs
{
    public InventoryChangedEvent(int productId, int warehouseId)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
    }

    public int ProductId { get; }

    public int WarehouseId { get; }
}
