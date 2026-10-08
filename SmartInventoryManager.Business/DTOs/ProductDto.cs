namespace SmartInventoryManager.Business.DTOs;

public sealed record ProductDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    int CategoryId,
    string CategoryName,
    string Unit,
    decimal PurchasePrice,
    decimal SellingPrice,
    int ReorderPoint,
    int MaximumStock,
    bool IsActive,
    DateTime CreatedAt);
