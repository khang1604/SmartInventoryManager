using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Authorization;

public sealed class AuthorizationService
{
    private readonly InventoryDbContext _dbContext;

    // Khởi tạo service phân quyền với DbContext dùng để đối chiếu tài khoản và phạm vi kho.
    public AuthorizationService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // Kiểm tra quyền xem chức năng theo vai trò trong phiên đăng nhập.
    public bool CanView(UserSession session)
    {
        return session.IsAuthenticated;
    }

    // Kiểm tra người dùng đã đăng nhập với vai trò quản trị viên.
    public bool IsAdmin(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    // Kiểm tra người dùng đã đăng nhập với vai trò quản lý kho.
    public bool IsWarehouseManager(UserSession session)
    {
        return HasRole(session, RoleNames.WarehouseManager);
    }

    // Kiểm tra người dùng đã đăng nhập với vai trò nhân viên kho.
    public bool IsStaff(UserSession session)
    {
        return HasRole(session, RoleNames.Staff);
    }

    // Kiểm tra quyền quản lý sản phẩm của người dùng.
    public bool CanManageProduct(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    // Kiểm tra quyền quản lý danh mục của người dùng.
    public bool CanManageCategory(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    // Kiểm tra quyền quản lý nhà cung cấp của người dùng.
    public bool CanManageSupplier(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    // Kiểm tra quyền quản lý tài khoản người dùng.
    public bool CanManageUser(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    // Kiểm tra quyền quản lý vai trò của người dùng.
    public bool CanManageRole(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    // Kiểm tra quyền quản lý kho của người dùng.
    public bool CanManageWarehouse(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    // Kiểm tra quyền tạo phiếu nháp tại kho được yêu cầu.
    public async Task<bool> CanCreateDraftReceiptAsync(UserSession session, int warehouseId)
    {
        if (HasRole(session, RoleNames.Staff))
        {
            return await CanAccessCurrentWarehouseAsync(session, warehouseId);
        }

        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Kiểm tra quyền tạo phiếu nháp tại kho được yêu cầu.
    public Task<bool> CanCreateDraftReceiptAsync(
        UserSession session,
        int warehouseId,
        IReadOnlySet<int> assignedWarehouseIds)
    {
        ArgumentNullException.ThrowIfNull(assignedWarehouseIds);

        if (!IsStaff(session)) return CanCreateDraftReceiptAsync(session, warehouseId);
        // Caller-provided IDs may narrow access, but never grant permission.
        return assignedWarehouseIds.Contains(warehouseId)
            ? CanCreateDraftReceiptAsync(session, warehouseId)
            : Task.FromResult(false);
    }

    // Kiểm tra quyền sửa phiếu nháp theo người lập và phạm vi kho.
    public Task<bool> CanEditDraftReceiptAsync(UserSession session, int warehouseId)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId);
    }

    // Kiểm tra quyền sửa phiếu nháp theo người lập và phạm vi kho.
    public Task<bool> CanEditDraftReceiptAsync(
        UserSession session,
        int warehouseId,
        IReadOnlySet<int> assignedWarehouseIds)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId, assignedWarehouseIds);
    }

    // Kiểm tra quyền xóa phiếu nháp theo người lập và phạm vi kho.
    public Task<bool> CanDeleteDraftReceiptAsync(UserSession session, int warehouseId)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId);
    }

    // Kiểm tra quyền xóa phiếu nháp theo người lập và phạm vi kho.
    public Task<bool> CanDeleteDraftReceiptAsync(
        UserSession session,
        int warehouseId,
        IReadOnlySet<int> assignedWarehouseIds)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId, assignedWarehouseIds);
    }

    // Kiểm tra quyền duyệt phiếu trong phạm vi kho được quản lý.
    public async Task<bool> CanApproveReceiptAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Kiểm tra quyền hoàn tất phiếu trong phạm vi kho được quản lý.
    public Task<bool> CanCompleteReceiptAsync(UserSession session, int warehouseId)
    {
        return CanApproveReceiptAsync(session, warehouseId);
    }

    // Kiểm tra quyền hủy phiếu theo phạm vi kho và người lập.
    public async Task<bool> CanCancelReceiptAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Kiểm tra quyền hủy phiếu theo phạm vi kho và người lập.
    public async Task<bool> CanCancelReceiptAsync(
        UserSession session,
        int warehouseId,
        int createdBy,
        string status)
    {
        if (string.Equals(status, "DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            if (HasRole(session, RoleNames.Staff))
            {
                return session.CurrentUser?.UserId == createdBy &&
                    await CanAccessCurrentWarehouseAsync(session, warehouseId);
            }

            return await CanCreateDraftReceiptAsync(session, warehouseId);
        }

        return string.Equals(status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase)
            ? await CanApproveReceiptAsync(session, warehouseId)
            : false;
    }

    // Kiểm tra quyền sửa phiếu theo trạng thái và người sở hữu.
    public async Task<bool> CanEditReceiptAsync(
        UserSession session,
        int warehouseId,
        int createdBy,
        string status)
    {
        if (!string.Equals(status, "DRAFT", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HasRole(session, RoleNames.Staff))
        {
            return session.CurrentUser?.UserId == createdBy &&
                await CanAccessCurrentWarehouseAsync(session, warehouseId);
        }

        return await CanCreateDraftReceiptAsync(session, warehouseId);
    }

    // Kiểm tra quyền điều chỉnh số lượng tồn tại kho.
    public async Task<bool> CanAdjustInventoryAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Kiểm tra quyền chuyển hàng ở cả kho nguồn và kho đích.
    public async Task<bool> CanTransferInventoryAsync(UserSession session, int sourceWarehouseId, int destinationWarehouseId)
    {
        return sourceWarehouseId != destinationWarehouseId &&
            HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, sourceWarehouseId) &&
            await CanAccessManagedWarehouseAsync(session, destinationWarehouseId);
    }

    // Kiểm tra quyền xác nhận xử lý cảnh báo tại kho.
    public async Task<bool> CanResolveStockAlertAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Used only to populate the warehouse selector before STAFF has a context.
    // Lấy các kho được phân công để nhân viên chọn kho làm việc.
    public async Task<IReadOnlySet<int>> GetAssignedWarehouseIdsAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!await IsActorActiveAsync(session, cancellationToken))
            return new HashSet<int>();

        var query = _dbContext.Warehouses.AsNoTracking().Where(warehouse => warehouse.IsActive);
        if (IsWarehouseManager(session))
            query = query.Where(warehouse => warehouse.ManagerId == session.CurrentUser!.UserId);
        else if (IsStaff(session))
            query = query.Where(warehouse => _dbContext.UserWarehouses.Any(assignment =>
                assignment.IsActive && assignment.UserId == session.CurrentUser!.UserId && assignment.WarehouseId == warehouse.WarehouseId));
        else if (!IsAdmin(session))
            return new HashSet<int>();

        return (await query.Select(warehouse => warehouse.WarehouseId).ToListAsync(cancellationToken)).ToHashSet();
    }

    // Lấy các kho người dùng được phép thao tác theo phiên hiện tại.
    public async Task<IReadOnlySet<int>> GetAccessibleWarehouseIdsAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var assigned = await GetAssignedWarehouseIdsAsync(session, cancellationToken);
        if (!IsStaff(session)) return assigned;
        return session.CurrentWarehouseId is int warehouseId && assigned.Contains(warehouseId)
            ? new HashSet<int> { warehouseId }
            : new HashSet<int>();
    }

    // Directory visibility does not grant operational access to an inactive warehouse.
    // Lấy phạm vi xem danh sách kho, bao gồm quyền tra cứu kho ngừng hoạt động.
    public async Task<IReadOnlySet<int>> GetWarehouseDirectoryIdsAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (IsStaff(session)) return await GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (!await IsActorActiveAsync(session, cancellationToken)) return new HashSet<int>();
        var query = _dbContext.Warehouses.AsNoTracking();
        if (IsWarehouseManager(session)) query = query.Where(warehouse => warehouse.ManagerId == session.CurrentUser!.UserId);
        else if (!IsAdmin(session)) return new HashSet<int>();
        return (await query.Select(warehouse => warehouse.WarehouseId).ToListAsync(cancellationToken)).ToHashSet();
    }

    // Kiểm tra quyền xem dữ liệu của kho được yêu cầu.
    public async Task<bool> CanViewWarehouseAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouseIds = await GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return warehouseIds.Contains(warehouseId);
    }

    // Kiểm tra quyền truy cập kho thuộc phạm vi quản lý.
    private async Task<bool> CanAccessManagedWarehouseAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanViewWarehouseAsync(session, warehouseId);
    }

    // Kiểm tra quyền truy cập kho đang chọn và phân công còn hiệu lực.
    private async Task<bool> CanAccessCurrentWarehouseAsync(UserSession session, int warehouseId)
    {
        return IsStaff(session) && session.CurrentWarehouseId == warehouseId &&
            await CanViewWarehouseAsync(session, warehouseId);
    }

    // Yêu cầu người thao tác vẫn còn hoạt động và giữ vai trò hợp lệ.
    internal async Task EnsureActiveActorAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!await IsActorActiveAsync(session, cancellationToken))
            throw new UnauthorizedAccessException("Phiên làm việc không còn hợp lệ. Vui lòng đăng nhập lại.");
    }

    // Đối chiếu tài khoản và vai trò hiện hành với phiên đăng nhập.
    private async Task<bool> IsActorActiveAsync(UserSession session, CancellationToken cancellationToken)
    {
        if (session is null || !session.IsAuthenticated || session.CurrentUser is null) return false;
        var role = await _dbContext.Users.AsNoTracking()
            .Where(user => user.UserId == session.CurrentUser.UserId && user.IsActive)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync(cancellationToken);
        return role is not null && string.Equals(role.Trim(), session.CurrentUser.RoleName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // Kiểm tra người dùng có vai trò được yêu cầu.
    private static bool HasRole(UserSession session, params string[] roles)
    {
        return session is not null && session.IsAuthenticated &&
            session.CurrentUser is not null &&
            roles.Any(role => string.Equals(
                session.CurrentUser.RoleName.Trim(),
                role,
                StringComparison.OrdinalIgnoreCase));
    }
}
