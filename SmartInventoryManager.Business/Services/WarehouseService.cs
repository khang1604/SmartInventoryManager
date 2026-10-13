using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class WarehouseService
{
    private const int MaxLookupResults = 200;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public WarehouseService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để tra cứu kho.");
        }
        var warehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return await _dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.IsActive && warehouseIds.Contains(warehouse.WarehouseId))
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Take(MaxLookupResults)
            .Select(warehouse => new LookupItemDto(warehouse.WarehouseId, warehouse.WarehouseCode, warehouse.WarehouseName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated) throw new UnauthorizedAccessException("Bạn cần đăng nhập.");
        var warehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return await _dbContext.Warehouses.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId))
            .OrderBy(item => item.WarehouseCode)
            .Select(item => new WarehouseDto(item.WarehouseId, item.WarehouseCode, item.WarehouseName, item.Location, item.ManagerId, item.IsActive)).ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(UserSession session, WarehouseRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session); Validate(request);
        var code = request.WarehouseCode.Trim();
        if (await _dbContext.Warehouses.AnyAsync(item => item.WarehouseCode == code, cancellationToken)) throw new InvalidOperationException("Mã kho đã tồn tại.");
        var entity = new Warehouse { WarehouseCode = code, WarehouseName = request.WarehouseName.Trim(), Location = request.Location?.Trim(), ManagerId = request.ManagerId, IsActive = request.IsActive, CreatedAt = DateTime.Now };
        await ValidateManagerAsync(entity.ManagerId, cancellationToken); _dbContext.Warehouses.Add(entity); await _dbContext.SaveChangesAsync(cancellationToken); return entity.WarehouseId;
    }

    public async Task UpdateAsync(UserSession session, int warehouseId, WarehouseRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateId(warehouseId, nameof(warehouseId));
        EnsureCanManage(session); Validate(request);
        var entity = await _dbContext.Warehouses.SingleOrDefaultAsync(item => item.WarehouseId == warehouseId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy kho.");
        var code = request.WarehouseCode.Trim();
        if (await _dbContext.Warehouses.AnyAsync(item => item.WarehouseId != warehouseId && item.WarehouseCode == code, cancellationToken)) throw new InvalidOperationException("Mã kho đã tồn tại.");
        await ValidateManagerAsync(request.ManagerId, cancellationToken); entity.WarehouseCode = code; entity.WarehouseName = request.WarehouseName.Trim(); entity.Location = request.Location?.Trim(); entity.ManagerId = request.ManagerId; entity.IsActive = request.IsActive; await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(UserSession session, int warehouseId, bool isActive, CancellationToken cancellationToken = default)
    {
        ValidateId(warehouseId, nameof(warehouseId));
        EnsureCanManage(session); var entity = await _dbContext.Warehouses.SingleOrDefaultAsync(item => item.WarehouseId == warehouseId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy kho."); entity.IsActive = isActive; await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateId(int id, string parameterName)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(parameterName);
    }

    private void EnsureCanManage(UserSession session) { if (!_authorization.CanManageWarehouse(session)) throw new UnauthorizedAccessException("Bạn không có quyền quản lý kho."); }
    private static void Validate(WarehouseRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.WarehouseCode) || request.WarehouseCode.Trim().Length > 50)
            throw new ArgumentException("Mã kho là bắt buộc và không quá 50 ký tự.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.WarehouseName) || request.WarehouseName.Trim().Length > 200)
            throw new ArgumentException("Tên kho là bắt buộc và không quá 200 ký tự.", nameof(request));
        if (request.Location?.Trim().Length > 255)
            throw new ArgumentException("Địa điểm không quá 255 ký tự.", nameof(request));
    }
    private async Task ValidateManagerAsync(int? managerId, CancellationToken cancellationToken)
    {
        if (!managerId.HasValue) return;

        var isValidManager = await _dbContext.Users
            .Where(item => item.UserId == managerId.Value && item.IsActive)
            .AnyAsync(item => item.Role.RoleName == RoleNames.Admin || item.Role.RoleName == RoleNames.WarehouseManager, cancellationToken);
        if (!isValidManager)
            throw new InvalidOperationException("Người quản lý phải là tài khoản quản trị hoặc quản lý kho đang hoạt động.");
    }
}
