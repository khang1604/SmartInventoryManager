namespace SmartInventoryManager.Business.DTOs;

public sealed class ProductSearchDto
{
    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
