using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal static class ConnectedPages
{
    public static Control Products(UiServices services) => new ProductManagementPage(services);

    public static Control Suppliers(UiServices services) => WithDetails(new DataListPage<LookupItemDto>(services,
        "Nhà cung cấp đang hoạt động • Thông tin mở rộng và thêm/sửa chưa sẵn sàng",
        [("Code", "Mã nhà cung cấp"), ("Name", "Tên nhà cung cấp")],
        (_, search, _, _) => services.Suppliers.GetActiveAsync(services.Session, search)), services.Authorization.CanManageSupplier(services.Session), insetButton: true);

    public static Control Warehouses(UiServices services) => WithDetails(new DataListPage<LookupItemDto>(services,
        "Kho đang hoạt động • Thông tin mở rộng và thêm/sửa chưa sẵn sàng",
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
