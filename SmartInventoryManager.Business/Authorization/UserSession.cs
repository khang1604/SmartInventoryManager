using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Authorization;

public sealed class UserSession
{
    public AuthenticatedUserDto? CurrentUser { get; private set; }

    public int? CurrentWarehouseId { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public void SignIn(AuthenticatedUserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user;
        CurrentWarehouseId = null;
    }

    public void SetCurrentWarehouseFromAssignments(IReadOnlyCollection<int> warehouseIds)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        if (!IsAuthenticated || !string.Equals(CurrentUser!.RoleName.Trim(), RoleNames.Staff, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentWarehouseId = warehouseIds.Count == 1 ? warehouseIds.Single() : null;
    }

    public void SetCurrentWarehouse(int warehouseId)
    {
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
        }

        if (!IsAuthenticated)
        {
            throw new InvalidOperationException("Bạn cần đăng nhập trước khi chọn kho hiện tại.");
        }

        CurrentWarehouseId = warehouseId;
    }

    public void SignOut()
    {
        CurrentUser = null;
        CurrentWarehouseId = null;
    }
}
