namespace SmartInventoryManager.Business.Events;

public sealed class StockAlertEvent : EventArgs
{
    public StockAlertEvent(
        int productId,
        int warehouseId,
        string alertType,
        long alertId = 0,
        int currentQuantity = 0,
        DateTime? timestamp = null)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        AlertType = alertType;
        AlertId = alertId;
        CurrentQuantity = currentQuantity;
        Timestamp = timestamp ?? DateTime.Now;
    }

    public int ProductId { get; }

    public int WarehouseId { get; }

    public string AlertType { get; }

    public long AlertId { get; }

    public int CurrentQuantity { get; }

    public DateTime Timestamp { get; }
}
