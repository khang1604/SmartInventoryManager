namespace SmartInventoryManager.Business.DTOs;

public sealed record AuthenticatedUserDto(
    int UserId,
    string Username,
    string FullName,
    string Email,
    string RoleName);
