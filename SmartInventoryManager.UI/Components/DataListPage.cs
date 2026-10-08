using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class DataListPage<T> : UserControl where T : class
{
    private readonly UiServices _services;
    private readonly Func<int, string, DateTime?, DateTime?, Task<IReadOnlyList<T>>> _query;
    private readonly bool _requiresWarehouse;
    private readonly ComboBox _warehouse = LiveUi.Choice("cboWarehouse", "Kho đang xem");
    private readonly TextBox _search = new() { Name = "txtSearch", Width = 210, AccessibleName = "Tìm mã hoặc tên" };
    private readonly DateTimePicker _from = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly DateTimePicker _to = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly ComboBox _type = LiveUi.Choice("cboType", "Loại giao dịch");
    private readonly DataGridView _grid;
    private readonly Label _message = ProductUi.Label("Đang tải dữ liệu…");
    private readonly FlowLayoutPanel _filters = LiveUi.Toolbar();
    private readonly FlowLayoutPanel _actions = LiveUi.Toolbar();
    private readonly string _scope;
    private bool _busy;
    private bool _initialized;
    private bool _hiddenAfterLoad;
    private readonly bool _history;
    private readonly List<(AntdUI.Button Button, Func<T, int, Task<bool>>? Permission)> _selectedActions = new();

    public DataListPage(UiServices services, string scope,
        (string Property, string Caption)[] columns,
        Func<int, string, DateTime?, DateTime?, Task<IReadOnlyList<T>>> query,
        bool requiresWarehouse = false, bool history = false)
    {
        _services = services; _query = query; _scope = scope; _requiresWarehouse = requiresWarehouse; _history = history;
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(1, 4);
        root.Padding = new Padding(20, 12, 20, 16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _filters.Controls.Add(LiveUi.Field("Tìm mã hoặc tên", _search));
        if (requiresWarehouse) _filters.Controls.Add(LiveUi.Field("Kho đang xem *", _warehouse));
        if (history)
        {
            _filters.Controls.AddRange([LiveUi.Field("Từ ngày", _from), LiveUi.Field("Đến ngày", _to)]);
            _type.Items.AddRange(["Tất cả loại", "Nhập kho", "Xuất kho", "Điều chỉnh", "Chuyển kho đến", "Chuyển kho đi"]);
            _type.SelectedIndex = 0;
            _filters.Controls.Add(LiveUi.Field("Loại giao dịch", _type));
        }
        if (typeof(T) == typeof(StockAlertDto))
        {
            _type.Items.AddRange(["Tất cả loại", "Hết hàng", "Sắp hết", "Vượt mức"]);
            _type.SelectedIndex = 0;
            _filters.Controls.Add(LiveUi.Field("Loại cảnh báo", _type));
        }
        Action("Làm mới", RefreshAsync, "arrow-clockwise");
        Action("Bỏ bộ lọc", async () =>
        {
            _search.Clear(); _from.Checked = _to.Checked = false;
            if (_type.Items.Count > 0) _type.SelectedIndex = 0;
            await RefreshAsync();
        });
        _grid = LiveUi.Grid("dgvData", columns);
        _grid.SelectionChanged += async (_, _) => { if (!_busy) await UpdateSelectedActionsAsync(); };
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name is "AlertType" or "TransactionType" && e.Value is string status)
                e.Value = LiveUi.StatusText(status);
        };
        root.Controls.Add(_filters, 0, 0);
        root.Controls.Add(_actions, 0, 1);
        root.Controls.Add(_message, 0, 2);
        root.Controls.Add(LiveUi.TableFrame(_grid), 0, 3);
        Controls.Add(root);
        Load += async (_, _) => await RefreshAsync();
        VisibleChanged += async (_, _) =>
        {
            if (!Visible && _initialized) _hiddenAfterLoad = true;
            else if (Visible && _hiddenAfterLoad)
            {
                _hiddenAfterLoad = false;
                await RefreshAsync();
            }
        };
    }

    public void AddDetailsColumn() => UiDetails.AddColumn(_grid);

    public AntdUI.Button Action(string caption, Func<Task> action, string? icon = null)
    {
        var button = ProductUi.Button("btn" + _actions.Controls.Count, caption, icon);
        button.Click += async (_, _) => { if (!_busy) await action(); };
        _actions.Controls.Add(button);
        return button;
    }

    public void SelectedAction(string caption, Func<T, int, Task> action, string? icon = null,
        Func<T, int, Task<bool>>? permission = null)
    {
        var button = Action(caption, async () =>
        {
            if (_grid.CurrentRow?.DataBoundItem is not T row)
            { _message.Text = "Chọn một dòng để thao tác."; return; }
            var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
            _busy = true;
            _filters.Enabled = _actions.Enabled = _grid.Enabled = false;
            var succeeded = false;
            try { await action(row, warehouseId); succeeded = true; }
            catch (Exception ex) { if (!IsDisposed) _message.Text = LiveUi.Error(ex); }
            finally
            {
                _busy = false;
                if (!IsDisposed) _filters.Enabled = _actions.Enabled = _grid.Enabled = true;
            }
            if (!IsDisposed && succeeded) await RefreshAsync();
        }, icon);
        button.Enabled = false;
        _selectedActions.Add((button, permission));
    }

    private async Task UpdateSelectedActionsAsync()
    {
        foreach (var action in _selectedActions) action.Button.Enabled = false;
        if (_grid.CurrentRow?.DataBoundItem is not T row) return;
        var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
        foreach (var action in _selectedActions)
        {
            try
            {
                var allowed = action.Permission is null || await _services.RunAsync(() => action.Permission(row, warehouseId));
                if (IsDisposed || _busy || !ReferenceEquals(_grid.CurrentRow?.DataBoundItem, row)) return;
                action.Button.Enabled = allowed;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }

    public async Task RefreshAsync()
    {
        if (_busy) return;
        _initialized = true;
        if (_history && _from.Checked && _to.Checked && _from.Value.Date > _to.Value.Date)
        { _message.Text = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc."; return; }
        _busy = true;
        _filters.Enabled = _actions.Enabled = _grid.Enabled = false;
        _message.Text = "Đang tải dữ liệu…";
        try
        {
            if (_requiresWarehouse && _warehouse.DataSource is null)
            {
                var warehouses = await _services.RunAsync(() => _services.Warehouses.GetActiveAsync(_services.Session));
                if (IsDisposed) return;
                _warehouse.DisplayMember = nameof(LookupItemDto.Name);
                _warehouse.ValueMember = nameof(LookupItemDto.Id);
                _warehouse.DataSource = warehouses.ToList();
            }
            var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
            if (_requiresWarehouse && warehouseId == 0)
            { _grid.DataSource = null; _message.Text = "Chưa có kho đang hoạt động để xem dữ liệu."; return; }
            var from = _history && _from.Checked ? _from.Value.Date : (DateTime?)null;
            var to = _history && _to.Checked ? _to.Value.Date : (DateTime?)null;
            var rows = await _services.RunAsync(() => _query(warehouseId, _search.Text.Trim(), from, to));
            if (IsDisposed) return;
            if (_history && _type.SelectedIndex > 0)
            {
                var type = _type.SelectedIndex switch { 1 => "IMPORT", 2 => "EXPORT", 3 => "ADJUSTMENT", 4 => "TRANSFER_IN", _ => "TRANSFER_OUT" };
                rows = rows.Where(row => row is InventoryHistoryDto item && item.TransactionType == type).ToList();
            }
            if (typeof(T) == typeof(StockAlertDto) && _type.SelectedIndex > 0)
            {
                var type = _type.SelectedIndex switch { 1 => "OUT_OF_STOCK", 2 => "LOW_STOCK", _ => "OVERSTOCK" };
                rows = rows.Where(row => row is StockAlertDto item && item.AlertType == type).ToList();
            }
            _grid.DataSource = rows.ToList();
            var warehouse = _requiresWarehouse ? $" • Kho: {(_warehouse.SelectedItem as LookupItemDto)?.Name}" : "";
            var filtered = !string.IsNullOrWhiteSpace(_search.Text) || from.HasValue || to.HasValue || _type.SelectedIndex > 0;
            _message.Text = $"{_scope}{warehouse} • " + (rows.Count == 0
                ? (filtered ? "Không có kết quả phù hợp; thử bỏ bộ lọc." : "Chưa có dữ liệu.")
                : $"{rows.Count:N0} dòng");
        }
        catch (Exception ex) { if (!IsDisposed) { _grid.DataSource = null; _message.Text = LiveUi.LoadError(ex); } }
        finally
        {
            _busy = false;
            if (!IsDisposed) _filters.Enabled = _actions.Enabled = _grid.Enabled = true;
        }
        if (!IsDisposed) await UpdateSelectedActionsAsync();
    }
}
