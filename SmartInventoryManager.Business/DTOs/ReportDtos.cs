namespace SmartInventoryManager.Business.DTOs;

public sealed record ImportExportReportRowDto(
    DateTime ReceiptDate,
    string ReceiptCode,
    string ReceiptType,
    string WarehouseName,
    decimal TotalAmount,
    string Status);
