namespace SmartInventoryManager.Business.DTOs;

public sealed record AnalyticsStockDto(string ProductCode, string ProductName, string WarehouseCode, string WarehouseName,
    int Quantity, decimal PurchasePrice, decimal EstimatedValue, int SuggestedQuantity, string MovementStatus, DateTime? FirstMovementAt, DateTime? LastExportAt);
public sealed record AbcAnalysisDto(string ProductCode, string ProductName, string WarehouseCode, string WarehouseName,
    decimal ExportValue, decimal ValueShare, decimal CumulativeShare, string Classification);
public sealed record AnalyticsDto(DateTime AsOf, DateTime AbcFrom, DateTime DeadStockFrom, decimal EstimatedInventoryValue,
    IReadOnlyList<AnalyticsStockDto> Stock, IReadOnlyList<AbcAnalysisDto> Abc);
