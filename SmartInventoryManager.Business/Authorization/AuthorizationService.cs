using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Authorization;

public sealed class AuthorizationService
{
    private readonly InventoryDbContext _dbContext;

    public AuthorizationService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public bool CanView(UserSession session)
    {
        return session.IsAuthenticated;
    }

    public bool IsAdmin(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    public bool IsWarehouseManager(UserSession session)
    {
        return HasRole(session, RoleNames.WarehouseManager);
    }

    public bool IsStaff(UserSession session)
    {
        return HasRole(session, RoleNames.Staff);
    }

    public bool CanManageProduct(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    public bool CanManageCategory(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    public bool CanManageSupplier(UserSession session)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager);
    }

    public bool CanManageUser(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    public bool CanManageRole(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    public bool CanManageWarehouse(UserSession session)
    {
        return HasRole(session, RoleNames.Admin);
    }

    public async Task<bool> CanCreateDraftReceiptAsync(UserSession session, int warehouseId)
    {
        if (HasRole(session, RoleNames.Staff))
        {
            return await CanAccessCurrentWarehouseAsync(session, warehouseId);
        }

        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

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

    public Task<bool> CanEditDraftReceiptAsync(UserSession session, int warehouseId)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId);
    }

    public Task<bool> CanEditDraftReceiptAsync(
        UserSession session,
        int warehouseId,
        IReadOnlySet<int> assignedWarehouseIds)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId, assignedWarehouseIds);
    }

    public Task<bool> CanDeleteDraftReceiptAsync(UserSession session, int warehouseId)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId);
    }

    public Task<bool> CanDeleteDraftReceiptAsync(
        UserSession session,
        int warehouseId,
        IReadOnlySet<int> assignedWarehouseIds)
    {
        return CanCreateDraftReceiptAsync(session, warehouseId, assignedWarehouseIds);
    }

    public async Task<bool> CanApproveReceiptAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    public Task<bool> CanCompleteReceiptAsync(UserSession session, int warehouseId)
    {
        return CanApproveReceiptAsync(session, warehouseId);
    }

    public async Task<bool> CanCancelReceiptAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

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

    public async Task<bool> CanAdjustInventoryAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    public async Task<bool> CanTransferInventoryAsync(UserSession session, int sourceWarehouseId, int destinationWarehouseId)
    {
        return sourceWarehouseId != destinationWarehouseId &&
            HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, sourceWarehouseId) &&
            await CanAccessManagedWarehouseAsync(session, destinationWarehouseId);
    }

    public async Task<bool> CanResolveStockAlertAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanAccessManagedWarehouseAsync(session, warehouseId);
    }

    // Used only to populate the warehouse selector before STAFF has a context.
    public async Task<IReadOnlySet<int>> GetAssignedWarehouseIdsAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!await IsActorActiveAsync(session, cancellationToken))
            return new HashSet<int>();

        var query = _dbContext.Warehouses.AsNoTracking().Where(warehouse => warehouse.IsActive);
        if (IsWarehouseManager(session))
            query = query.Where(warehouse => warehouse.ManagerId == session.CurrentUser!.UserId);
        else if (IsStaff(session))
            query = query.Where(warehouse => _dbContext.UserWarehouses.Any(assignment =>
                assignment.UserId == session.CurrentUser!.UserId && assignment.WarehouseId == warehouse.WarehouseId));
        else if (!IsAdmin(session))
            return new HashSet<int>();

        return (await query.Select(warehouse => warehouse.WarehouseId).ToListAsync(cancellationToken)).ToHashSet();
    }

    public async Task<IReadOnlySet<int>> GetAccessibleWarehouseIdsAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var assigned = await GetAssignedWarehouseIdsAsync(session, cancellationToken);
        if (!IsStaff(session)) return assigned;
        return session.CurrentWarehouseId is int warehouseId && assigned.Contains(warehouseId)
            ? new HashSet<int> { warehouseId }
            : new HashSet<int>();
    }

    public async Task<bool> CanViewWarehouseAsync(UserSession session, int warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouseIds = await GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        return warehouseIds.Contains(warehouseId);
    }

    private async Task<bool> CanAccessManagedWarehouseAsync(UserSession session, int warehouseId)
    {
        return HasRole(session, RoleNames.Admin, RoleNames.WarehouseManager) &&
            await CanViewWarehouseAsync(session, warehouseId);
    }

    private async Task<bool> CanAccessCurrentWarehouseAsync(UserSession session, int warehouseId)
    {
        return IsStaff(session) && session.CurrentWarehouseId == warehouseId &&
            await CanViewWarehouseAsync(session, warehouseId);
    }

    internal async Task EnsureActiveActorAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!await IsActorActiveAsync(session, cancellationToken))
            throw new UnauthorizedAccessException("Phiên làm việc không còn hợp lệ. Vui lòng đăng nhập lại.");
    }

    private async Task<bool> IsActorActiveAsync(UserSession session, CancellationToken cancellationToken)
    {
        if (session is null || !session.IsAuthenticated || session.CurrentUser is null) return false;
        var role = await _dbContext.Users.AsNoTracking()
            .Where(user => user.UserId == session.CurrentUser.UserId && user.IsActive)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync(cancellationToken);
        return role is not null && string.Equals(role.Trim(), session.CurrentUser.RoleName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

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
