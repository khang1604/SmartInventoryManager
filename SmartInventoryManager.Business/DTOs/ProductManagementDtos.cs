namespace SmartInventoryManager.Business.DTOs;

public sealed class ProductRequestDto
{
    public string ProductCode { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public string Unit { get; init; } = string.Empty;

    public decimal PurchasePrice { get; init; }

    public decimal SellingPrice { get; init; }

    public int ReorderPoint { get; init; }

    public int MaximumStock { get; init; }
}

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
