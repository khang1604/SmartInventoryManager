using SmartInventoryManager.Business.Inventory;

namespace SmartInventoryManager.Business.DTOs;

public sealed record InventoryDto(
    int InventoryId,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    int WarehouseId,
    int Quantity,
    int ReorderPoint,
    int MaximumStock,
    StockStatus StockStatus,
    DateTime UpdatedAt);
