using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Forms.Suppliers;

namespace SmartInventoryManager.UI.Components;

internal static class ConnectedPages
{
    public static Control Products(UiServices services) => new ProductManagementPage(services);

    public static Control Suppliers(UiServices services)
    {
        var page = new DataListPage<SupplierDto>(services,
            "Nhà cung cấp",
            [("SupplierCode", "Mã nhà cung cấp"), ("SupplierName", "Tên nhà cung cấp"),
                ("Phone", "Số điện thoại"), ("Email", "Email"), ("IsActive", "Trạng thái")],
            async (_, search, _, _) =>
            {
                var rows = await services.Suppliers.GetAsync(services.Session);
                if (string.IsNullOrWhiteSpace(search)) return rows;
                var value = search.Trim();
                return rows.Where(row =>
                    row.SupplierCode.Contains(value, StringComparison.CurrentCultureIgnoreCase)
                    || row.SupplierName.Contains(value, StringComparison.CurrentCultureIgnoreCase)
                    || (row.Phone?.Contains(value, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (row.Email?.Contains(value, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();
            });

        if (!services.Authorization.CanManageSupplier(services.Session)) return page;

        page.Action("Thêm nhà cung cấp", async () =>
        {
            using var detail = new FrmSupplierDetail(services);
            detail.ShowDialog(page.FindForm());
            await page.RefreshAsync();
        }, "plus-lg");
        page.SelectedAction("Sửa", async (row, _) =>
        {
            using var detail = new FrmSupplierDetail(services, row);
            detail.ShowDialog(page.FindForm());
            await Task.CompletedTask;
        }, "pencil", (_, _) => Task.FromResult(services.Authorization.CanManageSupplier(services.Session)));
        page.SelectedAction("Đổi trạng thái", async (row, _) =>
        {
            var action = row.IsActive ? "ngừng hoạt động" : "kích hoạt lại";
            if (MessageBox.Show(page.FindForm(), $"Bạn có chắc muốn {action} nhà cung cấp {row.SupplierCode}?",
                    "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
            await services.RunAsync(() => services.Suppliers.SetActiveAsync(services.Session, row.SupplierId, !row.IsActive));
        }, "arrow-repeat", (_, _) => Task.FromResult(services.Authorization.CanManageSupplier(services.Session)));
        return page;
    }

    public static Control Warehouses(UiServices services) => WithDetails(new DataListPage<LookupItemDto>(services,
        "Kho đang hoạt động",
        [("Code", "Mã kho"), ("Name", "Tên kho")],
        async (_, search, _, _) => (await services.Warehouses.GetActiveAsync(services.Session))
            .Where(row => Match(row.Code, row.Name, search)).ToList()), services.Authorization.CanManageWarehouse(services.Session));

    public static Control Inventory(UiServices services)
    {
        var page = new DataListPage<InventoryDto>(services,
            "Tồn hiện tại theo kho",
            [("ProductCode", "Mã sản phẩm"), ("ProductName", "Tên sản phẩm"), ("Unit", "Đơn vị"), ("Quantity", "Số lượng tồn"), ("ReorderPoint", "Ngưỡng đặt hàng"), ("MaximumStock", "Tồn tối đa"), ("StockStatus", "Trạng thái tồn"), ("UpdatedAt", "Cập nhật")],
            async (warehouseId, search, _, _) => (await services.Inventory.GetByWarehouseAsync(services.Session, warehouseId))
                .Where(row => Match(row.ProductCode, row.ProductName, search)).ToList(), requiresWarehouse: true);
        page.SelectedAction("Điều chỉnh tồn", async (row, warehouseId) =>
        {
            if (!await services.RunAsync(() => services.Authorization.CanAdjustInventoryAsync(services.Session, warehouseId)))
                throw new UnauthorizedAccessException();
            using var form = new StockOperationForm(services, row, false);
            form.ShowDialog(page.FindForm());
        }, permission: (_, warehouseId) => services.Authorization.CanAdjustInventoryAsync(services.Session, warehouseId));
        page.SelectedAction("Chuyển kho", async (row, _) =>
        {
            using var form = new StockOperationForm(services, row, true);
            form.ShowDialog(page.FindForm());
            await Task.CompletedTask;
        }, permission: (_, warehouseId) => services.Authorization.CanAdjustInventoryAsync(services.Session, warehouseId));
        page.SelectedAction("Xem lịch sử", async (row, _) =>
        {
            using var form = new Form { Text = $"Lịch sử: {row.ProductCode} • {row.ProductName}", ClientSize = new Size(1050, 650), StartPosition = FormStartPosition.CenterParent };
            WorkflowUi.StyleForm(form);
            form.Controls.Add(History(services, row.ProductId));
            form.ShowDialog(page.FindForm());
            await Task.CompletedTask;
        }, "file-earmark-text");
        return page;
    }

    public static Control InventoryReport(UiServices services)
    {
        var page = new DataListPage<InventoryReportRowDto>(services,
            "Báo cáo tồn kho",
            [("ProductCode", "Mã sản phẩm"), ("ProductName", "Tên sản phẩm"),
                ("WarehouseName", "Kho"), ("Unit", "Đơn vị"), ("Quantity", "Số lượng tồn"),
                ("ReorderPoint", "Điểm đặt hàng lại"), ("MaximumStock", "Tồn tối đa"),
                ("StockStatus", "Trạng thái tồn"), ("UpdatedAt", "Cập nhật")],
            async (warehouseId, search, _, _) =>
            {
                var report = await services.Reports.GetInventoryAsync(services.Session, warehouseId);
                return report.Rows.Where(row => Match(row.ProductCode, row.ProductName, search)).ToList();
            }, requiresWarehouse: true);
        page.Action("Xuất Excel", async () => await ExportInventoryAsync(page, services, false), "file-earmark-spreadsheet");
        page.Action("Xuất PDF", async () => await ExportInventoryAsync(page, services, true), "file-earmark-pdf");
        return page;
    }

    private static async Task ExportInventoryAsync(DataListPage<InventoryReportRowDto> page, UiServices services, bool pdf)
    {
        var report = await services.RunAsync(() => services.Reports.GetInventoryAsync(services.Session, page.SelectedWarehouseId));
        using var dialog = new SaveFileDialog
        {
            Filter = pdf ? "PDF (*.pdf)|*.pdf" : "Excel (*.xlsx)|*.xlsx",
            FileName = pdf ? "bao-cao-ton-kho.pdf" : "bao-cao-ton-kho.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(page.FindForm()) != DialogResult.OK) return;
        if (pdf) ReportExportService.ExportInventoryPdf(dialog.FileName, report);
        else ReportExportService.ExportInventoryExcel(dialog.FileName, report);
    }

    public static Control ImportExportReport(UiServices services)
    {
        var page = new DataListPage<ImportExportReportRowDto>(services,
            "Báo cáo nhập xuất",
            [("ReceiptDate", "Ngày"), ("ReceiptCode", "Mã phiếu"), ("ReceiptType", "Loại phiếu"),
                ("WarehouseName", "Kho"), ("TotalAmount", "Tổng tiền"), ("Status", "Trạng thái")],
            async (warehouseId, search, from, to) =>
            {
                var report = await services.Reports.GetImportExportAsync(services.Session, warehouseId, from, to);
                var rows = report.Imports.Select(item => new ImportExportReportRowDto(item.ReceiptDate, item.ReceiptCode,
                    "IMPORT", item.WarehouseName, item.TotalAmount, item.Status))
                    .Concat(report.Exports.Select(item => new ImportExportReportRowDto(item.ReceiptDate, item.ReceiptCode,
                        "EXPORT", item.WarehouseName, item.TotalAmount, item.Status)));
                return rows.Where(row => string.IsNullOrWhiteSpace(search) || row.ReceiptCode.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                    .OrderByDescending(row => row.ReceiptDate).ToList();
            }, requiresWarehouse: true, history: true);
        page.Action("Xuất Excel", async () => await ExportImportExportAsync(page, services, false), "file-earmark-spreadsheet");
        page.Action("Xuất PDF", async () => await ExportImportExportAsync(page, services, true), "file-earmark-pdf");
        return page;
    }

    private static async Task ExportImportExportAsync(DataListPage<ImportExportReportRowDto> page, UiServices services, bool pdf)
    {
        var report = await services.RunAsync(() => services.Reports.GetImportExportAsync(services.Session,
            page.SelectedWarehouseId, page.SelectedFrom, page.SelectedTo));
        using var dialog = new SaveFileDialog
        {
            Filter = pdf ? "PDF (*.pdf)|*.pdf" : "Excel (*.xlsx)|*.xlsx",
            FileName = pdf ? "bao-cao-nhap-xuat.pdf" : "bao-cao-nhap-xuat.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(page.FindForm()) != DialogResult.OK) return;
        if (pdf) ReportExportService.ExportImportExportPdf(dialog.FileName, report);
        else ReportExportService.ExportImportExportExcel(dialog.FileName, report);
    }

    public static Control History(UiServices services, int? productId = null) => new DataListPage<InventoryHistoryDto>(services,
        "Lịch sử tồn kho",
        [("ProductCode", "Mã sản phẩm"), ("ProductName", "Sản phẩm"), ("TransactionType", "Loại giao dịch"),
            ("Quantity", "Số lượng"), ("ReferenceType", "Loại tham chiếu"), ("ReferenceId", "ID tham chiếu"),
            ("CreatorName", "Người tạo"), ("CreatedAt", "Thời gian"), ("Note", "Ghi chú")],
        async (warehouseId, search, from, to) => (await services.Inventory.GetHistoryAsync(services.Session, warehouseId, productId, from, to))
            .Where(row => Match(row.ProductCode, row.ProductName, search)).ToList(), requiresWarehouse: true, history: true);

    public static Control Alerts(UiServices services)
    {
        var page = new DataListPage<StockAlertDto>(services, "Cảnh báo đang mở",
            [("ProductCode", "Mã sản phẩm"), ("ProductName", "Sản phẩm"), ("AlertType", "Loại cảnh báo"),
                ("CurrentQuantity", "Tồn hiện tại"), ("Threshold", "Ngưỡng"), ("CreatedAt", "Thời gian")],
            async (warehouseId, search, _, _) => (await services.Alerts.GetOpenAsync(services.Session, warehouseId))
                .Where(row => Match(row.ProductCode, row.ProductName, search)).ToList(), requiresWarehouse: true);
        page.SelectedAction("Đóng cảnh báo", async (row, warehouseId) =>
        {
            if (!await services.RunAsync(() => services.Authorization.CanResolveStockAlertAsync(services.Session, warehouseId)))
                throw new UnauthorizedAccessException();
            if (MessageBox.Show(page.FindForm(), $"Đóng cảnh báo của {row.ProductCode} tại kho đang xem?",
                "Xác nhận xử lý", MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
            await services.RunAsync(() => services.Alerts.ResolveAsync(services.Session, row.AlertId));
        }, permission: (_, warehouseId) => services.Authorization.CanResolveStockAlertAsync(services.Session, warehouseId));
        return page;
    }

    private static Control WithDetails(DataListPage<LookupItemDto> page, bool canManage, bool insetButton = false)
    {
        page.AddDetailsColumn(insetButton);
        if (canManage)
        {
            foreach (var caption in new[] { "Thêm", "Sửa", "Ngừng hoạt động" })
            {
                var button = page.Action(caption, () => Task.CompletedTask);
                button.Enabled = false;
                button.AccessibleDescription = "Chưa có chức năng lưu dữ liệu cho mục này.";
            }
        }
        return page;
    }

    private static bool Match(string code, string name, string search) => string.IsNullOrWhiteSpace(search)
        || code.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || name.Contains(search, StringComparison.CurrentCultureIgnoreCase);
}
