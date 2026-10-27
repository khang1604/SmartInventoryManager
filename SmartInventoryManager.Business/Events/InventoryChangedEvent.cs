namespace SmartInventoryManager.Business.Events;

public sealed class InventoryChangedEvent : EventArgs
{
    // Khởi tạo dữ liệu sự kiện biến động tồn kho.
    public InventoryChangedEvent(
        int productId,
        int warehouseId,
        int previousQuantity = 0,
        int currentQuantity = 0,
        string? transactionType = null,
        int? actorId = null,
        DateTime? timestamp = null)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        PreviousQuantity = previousQuantity;
        CurrentQuantity = currentQuantity;
        TransactionType = transactionType;
        ActorId = actorId;
        Timestamp = timestamp ?? DateTime.Now;
    }

    public int ProductId { get; }

    public int WarehouseId { get; }

    public int PreviousQuantity { get; }

    public int CurrentQuantity { get; }

    public string? TransactionType { get; }

    public int? ActorId { get; }

    public DateTime Timestamp { get; }
}
