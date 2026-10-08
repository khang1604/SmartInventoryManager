namespace SmartInventoryManager.Business.DTOs;

public sealed record DashboardDto(
    int? WarehouseId,
    DateTime? From,
    DateTime? To,
    int InventoryLineCount,
    long TotalQuantity,
    int OutOfStockCount,
    int OpenAlertCount,
    int CompletedImportCount,
    decimal CompletedImportAmount,
    int CompletedExportCount,
    decimal CompletedExportAmount,
    IReadOnlyList<DashboardTransactionDto> RecentTransactions);

public sealed record DashboardTransactionDto(
    long TransactionId,
    string ProductCode,
    string ProductName,
    string WarehouseName,
    string TransactionType,
    int Quantity,
    DateTime CreatedAt);
