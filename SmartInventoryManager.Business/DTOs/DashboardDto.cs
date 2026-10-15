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
    IReadOnlyList<DashboardTransactionDto> RecentTransactions)
{
    public IReadOnlyList<DashboardDailyQuantityDto> DailyQuantities { get; init; } = [];
    public IReadOnlyList<DashboardLowestStockDto> LowestStock { get; init; } = [];
    public IReadOnlyList<DashboardStatusCountDto> StockDistribution { get; init; } = [];
    public decimal EstimatedInventoryValue { get; init; }
}

public sealed record DashboardDailyQuantityDto(DateTime Date, long ImportQuantity, long ExportQuantity);
public sealed record DashboardLowestStockDto(string ProductCode, string WarehouseCode, int Quantity);
public sealed record DashboardStatusCountDto(string Status, int Count);

public sealed record DashboardTransactionDto(
    long TransactionId,
    string ProductCode,
    string ProductName,
    string WarehouseName,
    string TransactionType,
    int Quantity,
    DateTime CreatedAt)
{
    public string? ReferenceCode { get; init; }
    public string ActorName { get; init; } = string.Empty;
}
