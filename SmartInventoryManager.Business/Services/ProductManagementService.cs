using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Validators;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class ProductManagementService : IProductService
{
    private const int MaxPageSize = 100;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly ProductValidator _validator = new();

    public ProductManagementService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<PagedResultDto<ProductDto>> SearchAsync(
        UserSession session,
        ProductSearchDto search,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        EnsureAuthenticated(session);
        var page = NormalizePage(search.PageNumber, search.PageSize);
        var query = _dbContext.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var value = search.Search.Trim();
            query = query.Where(product => product.ProductCode.Contains(value) || product.ProductName.Contains(value));
        }
        if (search.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == search.CategoryId.Value);
        }
        if (search.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == search.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .Include(product => product.Category)
            .OrderBy(product => product.ProductCode)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
        var items = products.Select(ToDto).ToList();
        return new PagedResultDto<ProductDto>(items, page.PageNumber, page.PageSize, totalCount);
    }

    public async Task<ProductDto?> GetByIdAsync(
        UserSession session,
        int productId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(session);
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);
        return product is null ? null : ToDto(product);
    }

    public async Task<int> CreateAsync(
        UserSession session,
        ProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        ValidateRequest(request);
        await EnsureReferencesAndCodeAsync(request, null, cancellationToken);
        var product = new Product
        {
            ProductCode = request.ProductCode.Trim(),
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            Unit = request.Unit.Trim(),
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            ReorderPoint = request.ReorderPoint,
            MaximumStock = request.MaximumStock,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product.ProductId;
    }

    public async Task UpdateAsync(
        UserSession session,
        int productId,
        ProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        EnsureCanManage(session);
        ValidateRequest(request);
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");
        await EnsureReferencesAndCodeAsync(request, productId, cancellationToken);
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        var thresholdsChanged = product.ReorderPoint != request.ReorderPoint
            || product.MaximumStock != request.MaximumStock;
        product.ProductCode = request.ProductCode.Trim();
        product.ProductName = request.ProductName.Trim();
        product.CategoryId = request.CategoryId;
        product.Unit = request.Unit.Trim();
        product.PurchasePrice = request.PurchasePrice;
        product.SellingPrice = request.SellingPrice;
        product.ReorderPoint = request.ReorderPoint;
        product.MaximumStock = request.MaximumStock;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (thresholdsChanged)
        {
            await RefreshProductAlertsAsync(productId, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task SetActiveAsync(
        UserSession session,
        int productId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
        EnsureCanManage(session);
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        product.IsActive = isActive;
        if (isActive)
        {
            await RefreshProductAlertsAsync(productId, cancellationToken);
        }
        else
        {
            await new StockAlertService(_dbContext, _authorization)
                .ResolveForProductAsync(productId, cancellationToken, persistChanges: false, publishEvent: false);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task RefreshProductAlertsAsync(int productId, CancellationToken cancellationToken)
    {
        var warehouseIds = await _dbContext.Inventories
            .Where(item => item.ProductId == productId)
            .Select(item => item.WarehouseId)
            .Union(_dbContext.StockAlerts
                .Where(item => item.ProductId == productId && !item.IsResolved)
                .Select(item => item.WarehouseId))
            .Distinct()
            .ToListAsync(cancellationToken);
        var alertService = new StockAlertService(_dbContext, _authorization);
        foreach (var warehouseId in warehouseIds)
        {
            await alertService.RefreshAsync(
                productId,
                warehouseId,
                cancellationToken,
                persistChanges: false,
                publishEvent: false);
        }
    }

    private async Task EnsureReferencesAndCodeAsync(ProductRequestDto request, int? productId, CancellationToken cancellationToken)
    {
        var categoryExists = await _dbContext.Categories.AnyAsync(
            category => category.CategoryId == request.CategoryId && category.IsActive,
            cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("Danh mục không tồn tại hoặc đã ngừng hoạt động.");
        }
        var normalizedCode = request.ProductCode.Trim();
        var duplicateCode = await _dbContext.Products.AnyAsync(
            product => product.ProductCode == normalizedCode && (!productId.HasValue || product.ProductId != productId.Value),
            cancellationToken);
        if (duplicateCode)
        {
            throw new InvalidOperationException("Mã sản phẩm đã tồn tại.");
        }
    }

    private void EnsureCanManage(UserSession session)
    {
        if (!_authorization.CanManageProduct(session))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền quản lý sản phẩm.");
        }
    }

    private static void EnsureAuthenticated(UserSession session)
    {
        if (session is null || !session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem sản phẩm.");
        }
    }

    private void ValidateRequest(ProductRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = _validator.Validate(
            request.ProductCode,
            request.ProductName,
            request.Unit,
            request.PurchasePrice,
            request.SellingPrice,
            request.ReorderPoint,
            request.MaximumStock);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }
    }

    private static (int PageNumber, int PageSize) NormalizePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }
        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Kích thước trang phải từ 1 đến {MaxPageSize}.");
        }
        return (pageNumber, pageSize);
    }

    private static ProductDto ToDto(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (product.Category is null)
        {
            throw new InvalidOperationException(
                $"Không thể đọc sản phẩm {product.ProductId}: danh mục {product.CategoryId} không tồn tại hoặc chưa được tải.");
        }

        return new(
            product.ProductId,
            product.ProductCode,
            product.ProductName,
            product.CategoryId,
            product.Category.CategoryName,
            product.Unit,
            product.PurchasePrice,
            product.SellingPrice,
            product.ReorderPoint,
            product.MaximumStock,
            product.IsActive,
            product.CreatedAt);
    }
}
