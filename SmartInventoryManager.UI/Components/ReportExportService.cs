using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal static class ReportExportService
{
    public static void ExportInventoryExcel(string filePath, InventoryReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tồn kho");
        sheet.Cell(1, 1).Value = "Báo cáo tồn kho";
        sheet.Cell(2, 1).Value = "Tổng số lượng";
        sheet.Cell(2, 2).Value = report.TotalQuantity;
        sheet.Cell(3, 1).Value = "Cảnh báo đang mở";
        sheet.Cell(3, 2).Value = report.OpenAlertCount;
        var headers = new[] { "Mã sản phẩm", "Tên sản phẩm", "Kho", "Đơn vị", "Số lượng", "Điểm đặt hàng lại", "Tồn tối đa", "Trạng thái", "Cập nhật" };
        for (var index = 0; index < headers.Length; index++)
            sheet.Cell(5, index + 1).Value = headers[index];
        for (var rowIndex = 0; rowIndex < report.Rows.Count; rowIndex++)
        {
            var row = report.Rows[rowIndex];
            var values = new object?[] { row.ProductCode, row.ProductName, row.WarehouseName, row.Unit,
                row.Quantity, row.ReorderPoint, row.MaximumStock, LiveUi.StatusText(row.StockStatus), row.UpdatedAt };
            for (var columnIndex = 0; columnIndex < values.Length; columnIndex++)
                sheet.Cell(rowIndex + 6, columnIndex + 1).Value = XLCellValue.FromObject(values[columnIndex]);
        }
        sheet.Row(5).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public static void ExportInventoryPdf(string filePath, InventoryReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document => document.Page(page =>
        {
            page.Margin(30);
            page.Header().Text("BÁO CÁO TỒN KHO").Bold().FontSize(18);
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                foreach (var header in new[] { "Mã", "Sản phẩm", "Kho", "SL", "Trạng thái", "Cập nhật" })
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(header).Bold();
                foreach (var row in report.Rows)
                {
                    table.Cell().Padding(4).Text(row.ProductCode);
                    table.Cell().Padding(4).Text(row.ProductName);
                    table.Cell().Padding(4).Text(row.WarehouseName);
                    table.Cell().Padding(4).Text(row.Quantity.ToString("N0"));
                    table.Cell().Padding(4).Text(LiveUi.StatusText(row.StockStatus));
                    table.Cell().Padding(4).Text(row.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
                }
            });
            page.Footer().AlignRight().Text($"Tổng số lượng: {report.TotalQuantity:N0} • Cảnh báo mở: {report.OpenAlertCount:N0}");
        })).GeneratePdf(filePath);
    }

    public static void ExportImportExportExcel(string filePath, ImportExportReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Nhập xuất");
        var headers = new[] { "Ngày", "Mã phiếu", "Loại", "Kho", "Tổng tiền", "Trạng thái" };
        for (var index = 0; index < headers.Length; index++)
            sheet.Cell(1, index + 1).Value = headers[index];
        var rows = report.Imports.Select(item => new object?[] { item.ReceiptDate, item.ReceiptCode, "Nhập kho", item.WarehouseName, item.TotalAmount, LiveUi.StatusText(item.Status) })
            .Concat(report.Exports.Select(item => new object?[] { item.ReceiptDate, item.ReceiptCode, "Xuất kho", item.WarehouseName, item.TotalAmount, LiveUi.StatusText(item.Status) }));
        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
                sheet.Cell(rowIndex, columnIndex + 1).Value = XLCellValue.FromObject(row[columnIndex]);
            rowIndex++;
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public static void ExportImportExportPdf(string filePath, ImportExportReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var rows = report.Imports.Select(item => (item.ReceiptDate, item.ReceiptCode, Type: "Nhập kho", item.WarehouseName, item.TotalAmount))
            .Concat(report.Exports.Select(item => (item.ReceiptDate, item.ReceiptCode, Type: "Xuất kho", item.WarehouseName, item.TotalAmount)))
            .OrderByDescending(item => item.ReceiptDate).ToList();
        Document.Create(document => document.Page(page =>
        {
            page.Margin(30);
            page.Header().Text("BÁO CÁO NHẬP XUẤT").Bold().FontSize(18);
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                    columns.RelativeColumn(2); columns.RelativeColumn();
                });
                foreach (var header in new[] { "Ngày", "Mã phiếu", "Loại", "Kho", "Tổng tiền" })
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(header).Bold();
                foreach (var row in rows)
                {
                    table.Cell().Padding(4).Text(row.ReceiptDate.ToString("dd/MM/yyyy"));
                    table.Cell().Padding(4).Text(row.ReceiptCode);
                    table.Cell().Padding(4).Text(row.Type);
                    table.Cell().Padding(4).Text(row.WarehouseName);
                    table.Cell().Padding(4).Text(row.TotalAmount.ToString("N0"));
                }
            });
            page.Footer().AlignRight().Text($"Nhập: {report.ImportTotalAmount:N0} • Xuất: {report.ExportTotalAmount:N0}");
        })).GeneratePdf(filePath);
    }
}
