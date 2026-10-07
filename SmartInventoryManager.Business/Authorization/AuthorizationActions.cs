namespace SmartInventoryManager.Business.Authorization;

public static class AuthorizationActions
{
    public const string View = "VIEW";
    public const string CreateDraftReceipt = "CREATE_DRAFT_RECEIPT";
    public const string EditDraftReceipt = "EDIT_DRAFT_RECEIPT";
    public const string DeleteDraftReceipt = "DELETE_DRAFT_RECEIPT";
    public const string ApproveReceipt = "APPROVE_RECEIPT";
    public const string AdjustInventory = "ADJUST_INVENTORY";
    public const string TransferInventory = "TRANSFER_INVENTORY";
    public const string ResolveStockAlert = "RESOLVE_STOCK_ALERT";
}
