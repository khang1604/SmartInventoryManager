using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Authorization;

public sealed class UserSession
{
    public AuthenticatedUserDto? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public void SignIn(AuthenticatedUserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user;
    }

    public void SignOut()
    {
        CurrentUser = null;
    }
}
