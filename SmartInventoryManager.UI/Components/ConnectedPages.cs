using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Forms.Suppliers;

namespace SmartInventoryManager.UI.Components;

internal static class ConnectedPages
{
    // Tạo trang danh sách sản phẩm đã kết nối với các service.
    public static Control Products(UiServices services) => new ProductManagementPage(services);

    // Tạo trang nhà cung cấp đã kết nối với các service.
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

        page.AddDetailsColumn(true);
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

    // Tạo trang kho đã kết nối với các service.
    public static Control Warehouses(UiServices services) => new SmartInventoryManager.Forms.Warehouses.FrmWarehouse(services);

    // Tạo trang tồn kho và các thao tác điều chỉnh, chuyển kho.
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

    // Tạo trang báo cáo tồn kho có bộ lọc và thao tác xuất dữ liệu.
    public static Control InventoryReport(UiServices services)
    {
        DataListPage<InventoryReportRowDto>? page = null;
        page = new DataListPage<InventoryReportRowDto>(services,
            "Báo cáo tồn kho",
            [("ProductCode", "Mã sản phẩm"), ("ProductName", "Tên sản phẩm"),
                ("WarehouseName", "Kho"), ("Unit", "Đơn vị"), ("Quantity", "Số lượng tồn"),
                ("ReorderPoint", "Điểm đặt hàng lại"), ("MaximumStock", "Tồn tối đa"),
                ("StockStatus", "Trạng thái tồn"), ("UpdatedAt", "Cập nhật")],
            async (warehouseId, search, _, _) =>
            {
                var report = await services.Reports.GetInventoryAsync(services.Session, warehouseId, search: search, stockStatus: InventoryStatusFilter(page));
                page?.SetSummary($"Tổng tồn: {report.TotalQuantity:N0} • Ước tính: {report.EstimatedInventoryValue:N2} đ • Cảnh báo mở: {report.OpenAlertCount:N0}");
                return report.Rows;
            }, requiresWarehouse: true);
        page.Action("Xuất Excel", async () => await ExportInventoryAsync(page, services, false), "file-earmark-spreadsheet");
        page.Action("Xuất PDF", async () => await ExportInventoryAsync(page, services, true), "file-earmark-pdf");
        return page;
    }

    // Lấy đầy đủ dữ liệu phù hợp bộ lọc rồi xuất báo cáo tồn kho.
    private static async Task ExportInventoryAsync(DataListPage<InventoryReportRowDto> page, UiServices services, bool pdf)
    {
        var report = await services.RunAsync(() => services.Reports.GetInventoryAsync(services.Session, page.SelectedWarehouseId,
            search: page.SearchText, stockStatus: InventoryStatusFilter(page)));
        using var dialog = new SaveFileDialog
        {
            Filter = pdf ? "PDF (*.pdf)|*.pdf" : "Excel (*.xlsx)|*.xlsx",
            FileName = pdf ? "bao-cao-ton-kho.pdf" : "bao-cao-ton-kho.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(page.FindForm()) != DialogResult.OK) return;
        try
        {
            if (pdf) ReportExportService.ExportInventoryPdf(dialog.FileName, report);
            else ReportExportService.ExportInventoryExcel(dialog.FileName, report);
            MessageBox.Show(page.FindForm(), "Đã xuất báo cáo thành công.", "Xuất báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(page.FindForm(), LiveUi.ExportError(exception), "Không thể xuất báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Tạo trang báo cáo nhập xuất có bộ lọc và thao tác xuất dữ liệu.
    public static Control ImportExportReport(UiServices services)
    {
        DataListPage<ImportExportReportRowDto>? page = null;
        page = new DataListPage<ImportExportReportRowDto>(services,
            "Báo cáo nhập xuất",
            [("ReceiptDate", "Ngày"), ("ReceiptCode", "Mã phiếu"), ("ReceiptType", "Loại phiếu"),
                ("WarehouseName", "Kho"), ("TotalAmount", "Tổng tiền"), ("Status", "Trạng thái")],
            async (warehouseId, search, from, to) =>
            {
                var report = await services.Reports.GetImportExportAsync(services.Session, warehouseId, from, to,
                    search: search, receiptType: ReceiptTypeFilter(page));
                page?.SetSummary($"Tổng tiền theo bộ lọc: Nhập {report.ImportTotalAmount:N2} đ • Xuất {report.ExportTotalAmount:N2} đ");
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

    // Lấy đầy đủ dữ liệu phù hợp bộ lọc rồi xuất báo cáo nhập xuất.
    private static async Task ExportImportExportAsync(DataListPage<ImportExportReportRowDto> page, UiServices services, bool pdf)
    {
        var report = await services.RunAsync(() => services.Reports.GetImportExportAsync(services.Session,
            page.SelectedWarehouseId, page.SelectedFrom, page.SelectedTo, search: page.SearchText, receiptType: ReceiptTypeFilter(page)));
        using var dialog = new SaveFileDialog
        {
            Filter = pdf ? "PDF (*.pdf)|*.pdf" : "Excel (*.xlsx)|*.xlsx",
            FileName = pdf ? "bao-cao-nhap-xuat.pdf" : "bao-cao-nhap-xuat.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(page.FindForm()) != DialogResult.OK) return;
        try
        {
            if (pdf) ReportExportService.ExportImportExportPdf(dialog.FileName, report);
            else ReportExportService.ExportImportExportExcel(dialog.FileName, report);
            MessageBox.Show(page.FindForm(), "Đã xuất báo cáo thành công.", "Xuất báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(page.FindForm(), LiveUi.ExportError(exception), "Không thể xuất báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Tạo bộ lọc loại phiếu nhập hoặc xuất cho trang báo cáo.
    private static string? ReceiptTypeFilter(DataListPage<ImportExportReportRowDto>? page) => page?.SelectedType switch
    { 1 => "IMPORT", 2 => "EXPORT", _ => null };

    // Tạo bộ lọc trạng thái tồn kho cho trang dữ liệu.
    private static string? InventoryStatusFilter(DataListPage<InventoryReportRowDto>? page) => page?.SelectedType switch
    { 1 => "OUT_OF_STOCK", 2 => "LOW_STOCK", 3 => "NORMAL", 4 => "OVERSTOCK", _ => null };

    // Tạo trang tra cứu lịch sử biến động tồn kho.
    public static Control History(UiServices services, int? productId = null) => new DataListPage<InventoryHistoryDto>(services,
        "Lịch sử tồn kho",
        [("ProductCode", "Mã sản phẩm"), ("ProductName", "Sản phẩm"), ("TransactionType", "Loại giao dịch"),
            ("MovementCode", "Mã biến động"), ("TransferCode", "Liên kết chuyển kho"), ("Quantity", "Số lượng"),
            ("BeforeQuantity", "Tồn trước"), ("AfterQuantity", "Tồn sau"), ("SignedDifference", "Chênh lệch"), ("ReferenceType", "Chứng từ liên quan"), ("ReferenceId", "Số liên quan"),
            ("CreatorName", "Người tạo"), ("CreatedAt", "Thời gian"), ("Note", "Ghi chú")],
        async (warehouseId, search, from, to) => (await services.Inventory.GetHistoryAsync(services.Session, warehouseId, productId, from, to))
            .Where(row => Match(row.ProductCode, row.ProductName, search)).ToList(), requiresWarehouse: true, history: true);

    // Tạo trang cảnh báo tồn kho và thao tác xác nhận xử lý.
    public static Control Alerts(UiServices services)
    {
        var state = LiveUi.Choice("cboAlertState", "Trạng thái xử lý");
        state.Items.AddRange(["Đang mở", "Đã đóng"]);
        state.SelectedIndex = 0;
        var page = new DataListPage<StockAlertDto>(services, "Cảnh báo tồn kho",
            [("ProductCode", "Mã sản phẩm"), ("ProductName", "Sản phẩm"), ("AlertType", "Loại cảnh báo"),
                ("CurrentQuantity", "Tồn hiện tại"), ("Threshold", "Ngưỡng"), ("CreatedAt", "Thời gian"),
                ("ResolvedAt", "Đóng lúc"), ("ResolvedByName", "Người đóng")],
            async (warehouseId, search, _, _) => (await (state.SelectedIndex == 1
                ? services.Alerts.GetResolvedAsync(services.Session, warehouseId)
                : services.Alerts.GetOpenAsync(services.Session, warehouseId)))
                .Where(row => Match(row.ProductCode, row.ProductName, search)).ToList(), requiresWarehouse: true);
        page.AddFilter("Trạng thái xử lý", state);
        state.SelectedIndexChanged += async (_, _) => await page.RefreshAsync();
        page.SelectedAction("Đóng cảnh báo", async (row, warehouseId) =>
        {
            if (!await services.RunAsync(() => services.Authorization.CanResolveStockAlertAsync(services.Session, warehouseId)))
                throw new UnauthorizedAccessException();
            if (MessageBox.Show(page.FindForm(), $"Đóng cảnh báo của {row.ProductCode} tại kho đang xem?",
                "Xác nhận xử lý", MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
            await services.RunAsync(() => services.Alerts.ResolveAsync(services.Session, row.AlertId));
        }, permission: (row, warehouseId) => row.IsResolved ? Task.FromResult(false)
            : services.Authorization.CanResolveStockAlertAsync(services.Session, warehouseId));
        return page;
    }

    // Bổ sung cột và thao tác xem thông tin chi tiết cho danh sách.
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

    // Đối chiếu từ khóa với dữ liệu dùng trong bộ lọc.
    private static bool Match(string code, string name, string search) => string.IsNullOrWhiteSpace(search)
        || code.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || name.Contains(search, StringComparison.CurrentCultureIgnoreCase);
}
