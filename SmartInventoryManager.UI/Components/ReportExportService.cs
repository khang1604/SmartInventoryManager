using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal static class ReportExportService
{
    public static void ExportReceiptPdf(string path, string title, string code, DateTime date, string status,
        string warehouse, string creator, string? supplier, string? note, IReadOnlyList<ReceiptDetailViewDto> details, decimal total)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Text(title).Bold().FontSize(18);
            page.Content().PaddingVertical(15).Column(column =>
            {
                column.Spacing(8);
                column.Item().Text($"Mã phiếu: {code} • Ngày: {date:dd/MM/yyyy HH:mm}");
                column.Item().Text($"Trạng thái: {LiveUi.StatusText(status)}");
                column.Item().Text($"Kho: {warehouse}");
                column.Item().Text($"Người tạo: {creator}");
                if (!string.IsNullOrWhiteSpace(supplier)) column.Item().Text($"Nhà cung cấp: {supplier}");
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                    table.Header(header =>
                    {
                        foreach (var name in new[] { "Sản phẩm", "Đơn vị", "SL", "Đơn giá (đ)", "Thành tiền (đ)" })
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(name).Bold();
                    });
                    foreach (var row in details)
                    {
                        table.Cell().ShowEntire().Padding(5).Text($"{row.ProductCode}\n{row.ProductName}");
                        table.Cell().ShowEntire().Padding(5).Text(row.Unit);
                        table.Cell().ShowEntire().Padding(5).AlignRight().Text(row.Quantity.ToString("N0"));
                        table.Cell().ShowEntire().Padding(5).AlignRight().Text(row.UnitPrice.ToString("N2"));
                        table.Cell().ShowEntire().Padding(5).AlignRight().Text(row.LineTotal.ToString("N2"));
                    }
                });
                column.Item().AlignRight().Text($"Tổng tiền: {total:N2} đ").Bold();
                if (!string.IsNullOrWhiteSpace(note)) column.Item().Text($"Ghi chú: {note}");
            });
            page.Footer().AlignRight().Text(text => { text.Span("Trang "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
        })).GeneratePdf(path);
    }

    public static void ExportInventoryExcel(string filePath, InventoryReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tồn kho");
        sheet.Cell(1, 1).Value = "Báo cáo tồn kho";
        sheet.Cell(2, 1).Value = "Tổng số lượng khớp bộ lọc";
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
        sheet.Column(9).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        sheet.Columns(5, 7).Style.NumberFormat.Format = "#,##0";
        sheet.Row(5).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public static void ExportInventoryPdf(string filePath, InventoryReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.DefaultTextStyle(style => style.FontSize(9));
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
                foreach (var header in new[] { "Mã", "Sản phẩm", "Kho", "SL / đơn vị", "Trạng thái", "Cập nhật" })
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(header).Bold();
                foreach (var row in report.Rows)
                {
                    table.Cell().Padding(4).Text(row.ProductCode);
                    table.Cell().Padding(4).Text(row.ProductName);
                    table.Cell().Padding(4).Text(row.WarehouseName);
                    table.Cell().Padding(4).Text($"{row.Quantity:N0} {row.Unit}");
                    table.Cell().Padding(4).Text(LiveUi.StatusText(row.StockStatus));
                    table.Cell().Padding(4).Text(row.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
                }
            });
            page.Footer().AlignRight().Text($"Tổng số lượng khớp bộ lọc: {report.TotalQuantity:N0} • Cảnh báo mở: {report.OpenAlertCount:N0}");
        })).GeneratePdf(filePath);
    }

    public static void ExportImportExportExcel(string filePath, ImportExportReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Nhập xuất");
        var headers = new[] { "Ngày", "Mã phiếu", "Loại", "Kho", "Tổng tiền (đ)", "Trạng thái" };
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
        sheet.Column(1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        sheet.Column(5).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(rowIndex + 1, 1).Value = "Tổng tiền khớp bộ lọc (đ)";
        sheet.Cell(rowIndex + 2, 1).Value = "Nhập"; sheet.Cell(rowIndex + 2, 5).Value = report.ImportTotalAmount;
        sheet.Cell(rowIndex + 3, 1).Value = "Xuất"; sheet.Cell(rowIndex + 3, 5).Value = report.ExportTotalAmount;
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
            page.Size(PageSizes.A4.Landscape());
            page.DefaultTextStyle(style => style.FontSize(9));
            page.Margin(30);
            page.Header().Text("BÁO CÁO NHẬP XUẤT").Bold().FontSize(18);
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                    columns.RelativeColumn(2); columns.RelativeColumn();
                });
                foreach (var header in new[] { "Ngày", "Mã phiếu", "Loại", "Kho", "Tổng tiền (đ)" })
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(header).Bold();
                foreach (var row in rows)
                {
                    table.Cell().Padding(4).Text(row.ReceiptDate.ToString("dd/MM/yyyy"));
                    table.Cell().Padding(4).Text(row.ReceiptCode);
                    table.Cell().Padding(4).Text(row.Type);
                    table.Cell().Padding(4).Text(row.WarehouseName);
                    table.Cell().Padding(4).Text(row.TotalAmount.ToString("N2"));
                }
            });
            page.Footer().AlignRight().Text($"Tổng tiền khớp bộ lọc (đ) • Nhập: {report.ImportTotalAmount:N0} • Xuất: {report.ExportTotalAmount:N0}");
        })).GeneratePdf(filePath);
    }
}
