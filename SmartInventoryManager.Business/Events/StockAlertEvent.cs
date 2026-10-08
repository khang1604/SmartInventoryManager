namespace SmartInventoryManager.Business.Events;

public sealed class StockAlertEvent : EventArgs
{
    public StockAlertEvent(int productId, int warehouseId, string alertType)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        AlertType = alertType;
    }

    public int ProductId { get; }

    public int WarehouseId { get; }

    public string AlertType { get; }
}
