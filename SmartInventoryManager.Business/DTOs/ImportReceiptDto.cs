namespace SmartInventoryManager.Business.DTOs;

public sealed class ImportReceiptDto
{
    public int ImportReceiptId { get; init; }

    public string ReceiptCode { get; init; } = string.Empty;

    public int SupplierId { get; init; }

    public int WarehouseId { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<ReceiptDetailDto> Details { get; init; } = Array.Empty<ReceiptDetailDto>();
}
