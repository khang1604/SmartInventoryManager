namespace SmartInventoryManager.Business.DTOs;

public sealed record StockAlertDto(
    long AlertId,
    int ProductId,
    string ProductCode,
    string ProductName,
    int WarehouseId,
    string AlertType,
    int CurrentQuantity,
    int? Threshold,
    bool IsResolved,
    DateTime CreatedAt,
    DateTime? ResolvedAt)
{
    public string? ResolvedByName { get; init; }
}
