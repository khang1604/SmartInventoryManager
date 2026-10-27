using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Validators;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class ProductManagementService : IProductService
{
    private const int MaxPageSize = 100;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly ProductValidator _validator = new();
    private readonly StockEventPublisher _publisher;

    // Khởi tạo service xử lý sản phẩm cùng các thành phần phụ thuộc.
    public ProductManagementService(InventoryDbContext dbContext, AuthorizationService authorization, StockEventPublisher? publisher = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _publisher = publisher ?? new StockEventPublisher();
    }

    // Tìm kiếm và phân trang sản phẩm theo các điều kiện được cung cấp.
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

    // Lấy thông tin chi tiết sản phẩm theo mã định danh.
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

    // Kiểm tra dữ liệu rồi tạo mới sản phẩm.
    public async Task<int> CreateAsync(
        UserSession session,
        ProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
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

    // Kiểm tra dữ liệu rồi cập nhật thông tin sản phẩm.
    public async Task UpdateAsync(
        UserSession session,
        int productId,
        ProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
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
        await StockWorkflowEffects.CommitAsync(_dbContext, transaction, session, "UPDATE_PRODUCT", "Product", productId, _publisher, () => { }, cancellationToken);
    }

    // Đổi trạng thái hoạt động của sản phẩm sau khi kiểm tra điều kiện liên quan.
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
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
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
            await new StockAlertService(_dbContext, _authorization, _publisher)
                .ResolveForProductAsync(productId, cancellationToken, persistChanges: false, publishEvent: false);
        }
        await StockWorkflowEffects.CommitAsync(_dbContext, transaction, session, "SET_PRODUCT_ACTIVE", "Product", productId, _publisher, () => { }, cancellationToken);
    }

    // Cập nhật cảnh báo của sản phẩm sau khi thông tin hoặc ngưỡng thay đổi.
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
        var alertService = new StockAlertService(_dbContext, _authorization, _publisher);
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

    // Kiểm tra danh mục tham chiếu và tính duy nhất của mã sản phẩm.
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

    // Yêu cầu người dùng có quyền quản lý sản phẩm.
    private void EnsureCanManage(UserSession session)
    {
        if (!_authorization.CanManageProduct(session))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền quản lý sản phẩm.");
        }
    }

    // Yêu cầu người dùng đã đăng nhập trước khi thực hiện nghiệp vụ.
    private static void EnsureAuthenticated(UserSession session)
    {
        if (session is null || !session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem sản phẩm.");
        }
    }

    // Kiểm tra dữ liệu yêu cầu xử lý sản phẩm trước khi lưu.
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

    // Chuẩn hóa số trang và kích thước trang trong yêu cầu tìm kiếm.
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

    // Chuyển dữ liệu sản phẩm thành DTO trả về cho giao diện.
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
