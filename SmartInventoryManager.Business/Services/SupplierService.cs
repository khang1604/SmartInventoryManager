using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class SupplierService
{
    private readonly InventoryDbContext _dbContext;

    public SupplierService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(supplier => supplier.SupplierCode.Contains(value) || supplier.SupplierName.Contains(value));
        }
        return await query
            .OrderBy(supplier => supplier.SupplierCode)
            .Select(supplier => new LookupItemDto(supplier.SupplierId, supplier.SupplierCode, supplier.SupplierName))
            .ToListAsync(cancellationToken);
    }
}
