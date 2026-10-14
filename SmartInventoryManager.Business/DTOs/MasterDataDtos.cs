namespace SmartInventoryManager.Business.DTOs;

public sealed record CategoryDto(int CategoryId, string CategoryName, string? Description, bool IsActive);

public sealed class CategoryRequestDto
{
    public string CategoryName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record SupplierDto(int SupplierId, string SupplierCode, string SupplierName, string? Phone, string? Email, string? Address, string? TaxCode, bool IsActive);

public sealed class SupplierRequestDto
{
    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? TaxCode { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record WarehouseDto(int WarehouseId, string WarehouseCode, string WarehouseName, string? Location, int? ManagerId, bool IsActive)
{
    public string? ManagerName { get; init; }
}

public sealed class WarehouseRequestDto
{
    public string WarehouseCode { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public string? Location { get; init; }
    public int? ManagerId { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record LocationDto(int LocationId, int WarehouseId, string LocationCode, string? LocationName, bool IsActive)
{
    public string WarehouseName { get; init; } = string.Empty;
}

public sealed class LocationRequestDto
{
    public int WarehouseId { get; init; }
    public string LocationCode { get; init; } = string.Empty;
    public string? LocationName { get; init; }
    public bool IsActive { get; init; } = true;
}
