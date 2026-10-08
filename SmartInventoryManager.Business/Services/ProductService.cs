using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class ProductService
{
    private const int MaxLookupResults = 200;
    private readonly InventoryDbContext _dbContext;

    public ProductService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<ProductLookupDto>> GetActiveAsync(
        UserSession session,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để tra cứu sản phẩm.");
        }

        var query = _dbContext.Products.AsNoTracking().Where(product => product.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(product => product.ProductCode.Contains(value) || product.ProductName.Contains(value));
        }
        return await query
            .OrderBy(product => product.ProductCode)
            .Take(MaxLookupResults)
            .Select(product => new ProductLookupDto(
                product.ProductId,
                product.ProductCode,
                product.ProductName,
                product.Unit,
                product.PurchasePrice,
                product.SellingPrice,
                product.ReorderPoint,
                product.MaximumStock))
            .ToListAsync(cancellationToken);
    }
}
