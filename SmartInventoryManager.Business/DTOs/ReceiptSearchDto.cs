namespace SmartInventoryManager.Business.DTOs;

public sealed record ReceiptSearchDto
{
    public int? WarehouseId { get; init; }
    public string? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
