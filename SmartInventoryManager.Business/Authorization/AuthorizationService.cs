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
            return false;
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

        if (!HasRole(session, RoleNames.Staff))
        {
            return CanCreateDraftReceiptAsync(session, warehouseId);
        }

        return Task.FromResult(
            session.IsAuthenticated &&
            assignedWarehouseIds.Contains(warehouseId));
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

    private async Task<bool> CanAccessManagedWarehouseAsync(UserSession session, int warehouseId)
    {
        if (!session.IsAuthenticated || session.CurrentUser is null)
        {
            return false;
        }

        if (string.Equals(session.CurrentUser.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return await _dbContext.Warehouses
                .AsNoTracking()
                .AnyAsync(warehouse => warehouse.WarehouseId == warehouseId && warehouse.IsActive);
        }

        if (!string.Equals(session.CurrentUser.RoleName, RoleNames.WarehouseManager, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _dbContext.Warehouses
            .AsNoTracking()
            .AnyAsync(warehouse =>
                warehouse.WarehouseId == warehouseId &&
                warehouse.ManagerId == session.CurrentUser.UserId &&
                warehouse.IsActive);
    }

    private static bool HasRole(UserSession session, params string[] roles)
    {
        return session.IsAuthenticated &&
            session.CurrentUser is not null &&
            roles.Any(role => string.Equals(
                session.CurrentUser.RoleName,
                role,
                StringComparison.OrdinalIgnoreCase));
    }
}
