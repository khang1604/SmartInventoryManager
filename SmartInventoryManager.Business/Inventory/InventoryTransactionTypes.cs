namespace SmartInventoryManager.Business.Inventory;

internal static class InventoryTransactionTypes
{
    public const string Import = "IMPORT";
    public const string Export = "EXPORT";
    public const string Adjustment = "ADJUSTMENT";
    public const string TransferIn = "TRANSFER_IN";
    public const string TransferOut = "TRANSFER_OUT";
    public const string ImportReceipt = "IMPORT_RECEIPT";
    public const string ExportReceipt = "EXPORT_RECEIPT";
    public const string Manual = "MANUAL";
}