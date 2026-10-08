namespace SmartInventoryManager.Business.DTOs;

public sealed record NotificationDto(
    long NotificationId,
    string Title,
    string Message,
    string? NotificationType,
    bool IsRead,
    DateTime CreatedAt);
