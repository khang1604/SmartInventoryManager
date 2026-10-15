using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class AnalyticsPage : Form
{
    private readonly UiServices _services;
    private readonly ComboBox _warehouse = LiveUi.Choice("cboAnalyticsWarehouse", "Kho phân tích");
    private readonly NumericUpDown _a = new() { Minimum = 1, Maximum = 98, Value = 80, Width = 90, AccessibleName = "Ngưỡng nhóm A (%)" };
    private readonly NumericUpDown _b = new() { Minimum = 2, Maximum = 99, Value = 95, Width = 90, AccessibleName = "Ngưỡng nhóm B (%)" };
    private readonly Label _message = ProductUi.Label("Chọn kho và nhấn Làm mới.");
    private readonly DataGridView _valuation = LiveUi.Grid("dgvEstimatedValue", [("ProductCode", "Mã"), ("ProductName", "Sản phẩm"), ("WarehouseName", "Kho"), ("Quantity", "Tồn"), ("PurchasePrice", "Giá nhập hiện tại (đ)"), ("EstimatedValue", "Giá trị ước tính (đ)")]);
    private readonly DataGridView _movement = LiveUi.Grid("dgvDeadStock", [("ProductCode", "Mã"), ("ProductName", "Sản phẩm"), ("WarehouseName", "Kho"), ("Quantity", "Tồn"), ("MovementStatus", "Luân chuyển"), ("FirstMovementAt", "Theo dõi từ"), ("LastExportAt", "Xuất gần nhất")]);
    private readonly DataGridView _reorder = LiveUi.Grid("dgvReorder", [("ProductCode", "Mã"), ("ProductName", "Sản phẩm"), ("WarehouseName", "Kho"), ("Quantity", "Tồn"), ("SuggestedQuantity", "SL đề xuất nhập")]);
    private readonly DataGridView _abc = LiveUi.Grid("dgvAbc", [("ProductCode", "Mã"), ("ProductName", "Sản phẩm"), ("WarehouseName", "Kho"), ("ExportValue", "Giá trị xuất (đ)"), ("ValueShare", "Tỷ trọng"), ("CumulativeShare", "Tích lũy"), ("Classification", "Nhóm ABC")]);
    private AnalyticsDto? _data;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private bool _busy;

    public AnalyticsPage(UiServices services)
    {
        _services = services; WorkflowUi.StyleForm(this);
        var root = ProductUi.Layout(1, 3); root.Padding = new Padding(12);
        LiveUi.KeepTableVisible(this, root);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var filters = LiveUi.Toolbar(); filters.WrapContents = true;
        filters.Controls.AddRange([LiveUi.Field("Kho", _warehouse), LiveUi.Field("A đến (%)", _a), LiveUi.Field("B đến (%)", _b)]);
        var refresh = ProductUi.Button("btnRefreshAnalytics", "Làm mới"); refresh.Click += async (_, _) => await LoadAsync();
        var export = ProductUi.Button("btnExportAnalytics", "Xuất Excel"); export.Click += (_, _) => Export();
        filters.Controls.AddRange([refresh, export]); root.Controls.Add(filters, 0, 0); root.Controls.Add(_message, 0, 1);
        LiveUi.FitWrappedToolbar(filters, root, 0);
        var tabs = _tabs;
        foreach (var (title, grid) in new[] { ("Giá trị ước tính", _valuation), ("Luân chuyển 90 ngày", _movement), ("Đề xuất nhập", _reorder), ("ABC theo giá trị xuất", _abc) })
        {
            var tab = new TabPage(title); tab.Controls.Add(LiveUi.TableFrame(grid)); tabs.TabPages.Add(tab);
        }
        root.Controls.Add(tabs, 0, 2); Controls.Add(root);
        foreach (var grid in new[] { _valuation, _movement, _reorder, _abc })
            grid.CellFormatting += (_, e) => { if (grid.Columns[e.ColumnIndex].Name is "MovementStatus" or "Classification" && e.Value is string value) e.Value = AnalyticsStatus(value); };
        foreach (var field in new[] { "FirstMovementAt", "LastExportAt" }) LiveUi.Column(_movement, field).DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        LiveUi.Column(_abc, "ValueShare").DefaultCellStyle.Format = "P2"; LiveUi.Column(_abc, "CumulativeShare").DefaultCellStyle.Format = "P2";
        LiveUi.Column(_abc, "ExportValue").DefaultCellStyle.Format = "N2"; LiveUi.Column(_valuation, "EstimatedValue").DefaultCellStyle.Format = "N2";
        Shown += async (_, _) => await LoadAsync();
        tabs.SelectedIndexChanged += (_, _) => UpdateSummary();
        _warehouse.SelectedIndexChanged += (_, _) => InvalidateData();
        _a.ValueChanged += (_, _) => InvalidateData();
        _b.ValueChanged += (_, _) => InvalidateData();
    }

    internal static string AnalyticsStatus(string value) => value switch
    {
        "DEAD_STOCK" => "Không luân chuyển",
        "MOVING" => "Có xuất trong 90 ngày",
        "INSUFFICIENT_HISTORY" => "Chưa đủ dữ liệu",
        "NO_STOCK" => "Không có tồn",
        "UNCLASSIFIED" => "Không đủ giá trị phân nhóm",
        _ => value
    };

    private async Task LoadAsync()
    {
        if (_busy) return; _busy = true; Enabled = false; _data = null; _message.Text = "Đang tải phân tích…";
        try
        {
            if (_warehouse.DataSource is null)
            {
                var choices = await _services.RunAsync(() => _services.Warehouses.GetActiveAsync(_services.Session));
                if (_services.Authorization.IsStaff(_services.Session)) choices = choices.Where(x => x.Id == _services.Session.CurrentWarehouseId).ToList();
                _warehouse.DisplayMember = "Name"; _warehouse.ValueMember = "Id";
                _warehouse.DataSource = new[] { new LookupItemDto(0, "", "Tất cả kho được phép") }.Concat(choices).ToList();
            }
            var selected = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
            _data = await _services.RunAsync(() => _services.Analytics.GetAsync(_services.Session, selected == 0 ? null : selected, _a.Value / 100, _b.Value / 100));
            _valuation.DataSource = _data.Stock.ToList(); _movement.DataSource = _data.Stock.Where(x => x.Quantity > 0).ToList();
            _reorder.DataSource = _data.Stock.Where(x => x.SuggestedQuantity > 0).ToList(); _abc.DataSource = _data.Abc.ToList();
            UpdateSummary();
        }
        catch (Exception ex) { foreach (var grid in new[] { _valuation, _movement, _reorder, _abc }) grid.DataSource = null; _message.Text = ex is ArgumentException ? "Ngưỡng ABC phải thỏa 0 < A < B < 100%." : LiveUi.LoadError(ex); }
        finally { _busy = false; if (!IsDisposed) Enabled = true; }
    }

    private void InvalidateData()
    {
        if (_busy || _data is null) return;
        _data = null; foreach (var grid in new[] { _valuation, _movement, _reorder, _abc }) grid.DataSource = null;
        _message.Text = "Bộ lọc đã đổi. Nhấn Làm mới trước khi xem hoặc xuất dữ liệu.";
    }

    private void UpdateSummary()
    {
        if (_data is null) return;
        var count = _tabs.SelectedIndex switch { 1 => _data.Stock.Count(x => x.Quantity > 0), 2 => _data.Stock.Count(x => x.SuggestedQuantity > 0), 3 => _data.Abc.Count, _ => _data.Stock.Count };
        var empty = count == 0 ? (_tabs.SelectedIndex == 3 ? "Không có phiếu xuất hoàn tất trong khoảng ABC." : "Không có dữ liệu khớp phạm vi của tab này.") : $"{count} dòng";
        _message.Text = $"Ước tính: {_data.EstimatedInventoryValue.ToString("N2", LiveUi.Culture)} đ • ABC: {_data.AbcFrom:dd/MM/yyyy}–{_data.AsOf:dd/MM/yyyy} • {empty}\nLuân chuyển từ {_data.DeadStockFrom:dd/MM/yyyy}; đề xuất tham khảo, không tự tạo phiếu. Không phải giá vốn kế toán.";
    }

    private void Export()
    {
        if (_busy || _data is null) { _message.Text = "Làm mới dữ liệu trước khi xuất."; return; }
        using var dialog = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", DefaultExt = "xlsx", AddExtension = true, FileName = "Inventory-Analytics.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { ReportExportService.ExportAnalyticsExcel(dialog.FileName, _data, _a.Value / 100, _b.Value / 100); _message.Text = "Đã xuất dữ liệu phân tích theo phạm vi vừa tải."; }
        catch (Exception ex) { _message.Text = LiveUi.ExportError(ex); }
    }
}
