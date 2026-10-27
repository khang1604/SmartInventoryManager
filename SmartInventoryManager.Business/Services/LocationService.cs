using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class LocationService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    // Khởi tạo service xử lý vị trí trong kho cùng các thành phần phụ thuộc.
    public LocationService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    // Lấy vị trí trong kho theo điều kiện tra cứu và phạm vi truy cập.
    public async Task<IReadOnlyList<LocationDto>> GetAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated) throw new UnauthorizedAccessException("Bạn cần đăng nhập.");
        var warehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return await _dbContext.Locations.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId))
            .OrderBy(item => item.LocationCode)
            .Select(item => new LocationDto(item.LocationId, item.WarehouseId, item.LocationCode, item.LocationName, item.IsActive)
            {
                WarehouseName = item.Warehouse.WarehouseName
            })
            .ToListAsync(cancellationToken);
    }

    // Kiểm tra dữ liệu rồi tạo mới vị trí trong kho.
    public async Task<int> CreateAsync(UserSession session, LocationRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        Validate(request);
        await EnsureWarehouseAsync(session, request.WarehouseId, cancellationToken);
        if (await _dbContext.Locations.AnyAsync(item => item.WarehouseId == request.WarehouseId && item.LocationCode == request.LocationCode.Trim(), cancellationToken))
            throw new InvalidOperationException("Mã vị trí đã tồn tại trong kho.");
        var entity = new Location { WarehouseId = request.WarehouseId, LocationCode = request.LocationCode.Trim(), LocationName = request.LocationName?.Trim(), IsActive = request.IsActive };
        _dbContext.Locations.Add(entity); await _dbContext.SaveChangesAsync(cancellationToken); return entity.LocationId;
    }

    // Kiểm tra dữ liệu rồi cập nhật thông tin vị trí trong kho.
    public async Task UpdateAsync(UserSession session, int locationId, LocationRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        Validate(request);
        await EnsureWarehouseAsync(session, request.WarehouseId, cancellationToken);
        var entity = await _dbContext.Locations.SingleOrDefaultAsync(item => item.LocationId == locationId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy vị trí.");
        await EnsureWarehouseAsync(session, entity.WarehouseId, cancellationToken);
        if (await _dbContext.Locations.AnyAsync(item => item.LocationId != locationId && item.WarehouseId == request.WarehouseId && item.LocationCode == request.LocationCode.Trim(), cancellationToken))
            throw new InvalidOperationException("Mã vị trí đã tồn tại trong kho.");
        entity.WarehouseId = request.WarehouseId; entity.LocationCode = request.LocationCode.Trim(); entity.LocationName = request.LocationName?.Trim(); entity.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Đổi trạng thái hoạt động của vị trí trong kho sau khi kiểm tra điều kiện liên quan.
    public async Task SetActiveAsync(UserSession session, int locationId, bool isActive, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        var entity = await _dbContext.Locations.SingleOrDefaultAsync(item => item.LocationId == locationId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy vị trí.");
        await EnsureWarehouseAsync(session, entity.WarehouseId, cancellationToken); entity.IsActive = isActive; await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Kiểm tra kho tồn tại và người dùng có quyền thao tác.
    private async Task EnsureWarehouseAsync(UserSession session, int warehouseId, CancellationToken cancellationToken)
    {
        var warehouseIds = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (!warehouseIds.Contains(warehouseId))
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác tại kho đã chọn.");
    }

    // Yêu cầu người dùng có quyền quản lý vị trí trong kho.
    private void EnsureCanManage(UserSession session) { if (!_authorization.CanManageWarehouse(session)) throw new UnauthorizedAccessException("Bạn không có quyền quản lý vị trí."); }
    // Kiểm tra các trường thông tin vị trí trong kho theo ràng buộc nghiệp vụ.
    private static void Validate(LocationRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.WarehouseId <= 0 || string.IsNullOrWhiteSpace(request.LocationCode) || request.LocationCode.Trim().Length > 50)
            throw new ArgumentException("Kho và mã vị trí hợp lệ, không quá 50 ký tự, là bắt buộc.", nameof(request));
        if (request.LocationName?.Trim().Length > 100)
            throw new ArgumentException("Tên vị trí không quá 100 ký tự.", nameof(request));
    }
}
