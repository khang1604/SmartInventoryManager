namespace SmartInventoryManager.Business.DTOs;

public sealed record InventoryDto(
    int InventoryId,
    int ProductId,
    string ProductCode,
    string ProductName,
    int WarehouseId,
    int Quantity,
    DateTime UpdatedAt);
