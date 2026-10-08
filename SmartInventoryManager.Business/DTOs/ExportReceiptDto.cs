namespace SmartInventoryManager.Business.DTOs;

public sealed class ExportReceiptDto
{
    public int ExportReceiptId { get; init; }

    public string ReceiptCode { get; init; } = string.Empty;

    public int WarehouseId { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<ReceiptDetailDto> Details { get; init; } = Array.Empty<ReceiptDetailDto>();
}
