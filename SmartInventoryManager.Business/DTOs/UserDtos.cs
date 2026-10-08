namespace SmartInventoryManager.Business.DTOs;

public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string? Email,
    string Password,
    string ConfirmPassword,
    string RoleName,
    bool IsActive = true);

public sealed record UserListItemDto(
    int UserId,
    string Username,
    string FullName,
    string? Email,
    string RoleName,
    bool IsActive,
    DateTime CreatedAt);
