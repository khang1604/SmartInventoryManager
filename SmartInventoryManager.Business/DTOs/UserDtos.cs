namespace SmartInventoryManager.Business.DTOs;

public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string? Email,
    string Password,
    string ConfirmPassword,
    string RoleName,
    bool IsActive = true);

public sealed record UpdateAccountRequest(string FullName, string? Email);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public sealed record UserListItemDto(
    int UserId,
    string Username,
    string FullName,
    string? Email,
    string RoleName,
    bool IsActive,
    DateTime CreatedAt);
