namespace SmartInventoryManager.Business.DTOs;

public sealed record ReceiptDetailViewDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record ImportReceiptListItemDto(
    int ImportReceiptId,
    string ReceiptCode,
    string SupplierName,
    string WarehouseName,
    string CreatorName,
    DateTime ReceiptDate,
    decimal TotalAmount,
    string Status);

public sealed record ExportReceiptListItemDto(
    int ExportReceiptId,
    string ReceiptCode,
    string WarehouseName,
    string CreatorName,
    DateTime ReceiptDate,
    decimal TotalAmount,
    string Status);

public sealed record ImportReceiptDetailsDto(
    int ImportReceiptId,
    string ReceiptCode,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    int CreatedBy,
    string CreatorName,
    DateTime ReceiptDate,
    decimal TotalAmount,
    string Status,
    string? Note,
    IReadOnlyList<ReceiptDetailViewDto> Details);

public sealed record ExportReceiptDetailsDto(
    int ExportReceiptId,
    string ReceiptCode,
    int WarehouseId,
    string WarehouseName,
    int CreatedBy,
    string CreatorName,
    DateTime ReceiptDate,
    decimal TotalAmount,
    string Status,
    string? Note,
    IReadOnlyList<ReceiptDetailViewDto> Details);

public sealed record InventoryHistoryDto(
    long TransactionId,
    int ProductId,
    string ProductCode,
    string ProductName,
    int WarehouseId,
    string TransactionType,
    int Quantity,
    string? ReferenceType,
    int? ReferenceId,
    string CreatorName,
    DateTime CreatedAt,
    string? Note);

public sealed record LookupItemDto(int Id, string Code, string Name);

public sealed record ProductLookupDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    decimal PurchasePrice,
    decimal SellingPrice,
    int ReorderPoint,
    int MaximumStock);

public sealed record InventoryReportRowDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    string WarehouseName,
    int Quantity,
    int ReorderPoint,
    int MaximumStock,
    string StockStatus,
    DateTime UpdatedAt);

public sealed record InventoryReportDto(
    long TotalQuantity,
    int OpenAlertCount,
    IReadOnlyList<InventoryReportRowDto> Rows);

public sealed record ImportExportReportDto(
    DateTime? From,
    DateTime? To,
    int? WarehouseId,
    IReadOnlyList<ImportReceiptListItemDto> Imports,
    IReadOnlyList<ExportReceiptListItemDto> Exports,
    decimal ImportTotalAmount,
    decimal ExportTotalAmount);