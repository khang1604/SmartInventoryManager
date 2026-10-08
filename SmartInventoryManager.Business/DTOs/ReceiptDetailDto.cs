namespace SmartInventoryManager.Business.DTOs;

public sealed record ReceiptDetailDto(int ProductId, int Quantity, decimal UnitPrice);