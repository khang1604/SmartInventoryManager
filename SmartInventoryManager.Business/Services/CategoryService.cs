using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class CategoryService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public CategoryService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAsync(UserSession session, bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(session);
        var query = _dbContext.Categories.AsNoTracking();
        if (!includeInactive) query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.CategoryName)
            .Select(item => new CategoryDto(item.CategoryId, item.CategoryName, item.Description, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(UserSession session, string? search = null, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(session);
        var query = _dbContext.Categories.AsNoTracking().Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.CategoryName.Contains(value));
        }
        return await query.OrderBy(item => item.CategoryName).Take(200)
            .Select(item => new LookupItemDto(item.CategoryId, item.CategoryName, item.CategoryName))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(UserSession session, CategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        Validate(request);
        if (await _dbContext.Categories.AnyAsync(item => item.CategoryName == request.CategoryName.Trim(), cancellationToken))
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");
        var entity = new Category { CategoryName = request.CategoryName.Trim(), Description = request.Description?.Trim(), IsActive = request.IsActive };
        _dbContext.Categories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.CategoryId;
    }

    public async Task UpdateAsync(UserSession session, int categoryId, CategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateId(categoryId, nameof(categoryId));
        EnsureCanManage(session);
        Validate(request);
        var entity = await _dbContext.Categories.SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");
        if (await _dbContext.Categories.AnyAsync(item => item.CategoryId != categoryId && item.CategoryName == request.CategoryName.Trim(), cancellationToken))
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");
        entity.CategoryName = request.CategoryName.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(UserSession session, int categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        ValidateId(categoryId, nameof(categoryId));
        EnsureCanManage(session);
        var entity = await _dbContext.Categories.SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");
        if (!isActive && await _dbContext.Products.AnyAsync(item => item.CategoryId == categoryId && item.IsActive, cancellationToken))
            throw new InvalidOperationException("Không thể ngừng danh mục đang có sản phẩm hoạt động.");
        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureCanManage(UserSession session)
    {
        if (!_authorization.CanManageCategory(session)) throw new UnauthorizedAccessException("Bạn không có quyền quản lý danh mục.");
    }

    private static void EnsureAuthenticated(UserSession session)
    {
        if (session is null || !session.IsAuthenticated) throw new UnauthorizedAccessException("Bạn cần đăng nhập.");
    }

    private static void Validate(CategoryRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.CategoryName) || request.CategoryName.Trim().Length > 100)
            throw new ArgumentException("Tên danh mục là bắt buộc và không quá 100 ký tự.", nameof(request));
        if (request.Description?.Trim().Length > 255)
            throw new ArgumentException("Mô tả danh mục không quá 255 ký tự.", nameof(request));
    }

    private static void ValidateId(int id, string parameterName)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(parameterName);
    }
}
