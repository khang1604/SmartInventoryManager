namespace SmartInventoryManager.Business.Events;

public sealed class ReceiptCompletedEvent : EventArgs
{
    // Khởi tạo dữ liệu sự kiện hoàn tất phiếu.
    public ReceiptCompletedEvent(int receiptId, bool import, int warehouseId, IReadOnlyList<int> productIds)
    {
        ReceiptId = receiptId;
        Import = import;
        WarehouseId = warehouseId;
        ProductIds = productIds;
    }

    public int ReceiptId { get; }

    public bool Import { get; }

    public int WarehouseId { get; }

    public IReadOnlyList<int> ProductIds { get; }
}
