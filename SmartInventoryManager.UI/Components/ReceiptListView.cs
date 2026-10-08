using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Forms.Imports;
using SmartInventoryManager.Forms.Exports;

namespace SmartInventoryManager.UI.Components;

internal sealed class ReceiptListView : UserControl
{
    private readonly UiServices _services;
    private readonly bool _import;
    private readonly ComboBox _warehouse = LiveUi.Choice("cboWarehouse", "Kho");
    private readonly ComboBox _status = LiveUi.Choice("cboStatus", "Trạng thái phiếu");
    private readonly TextBox _search = new() { Name = "txtSearch", Width = 180, MaxLength = 50 };
    private readonly DateTimePicker _from = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly DateTimePicker _to = new() { Width = 150, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
    private readonly Label _message = ProductUi.Label("Chọn bộ lọc và nhấn Làm mới.");
    private readonly DataGridView _grid;
    private readonly FlowLayoutPanel _filters = LiveUi.Toolbar();
    private readonly FlowLayoutPanel _actions = LiveUi.Toolbar();
    private bool _busy;
    private bool _initialized;
    private bool _hiddenAfterLoad;

    public ReceiptListView(UiServices services, bool import)
    {
        _services = services;
        _import = import;
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(1, 4);
        root.Padding = new Padding(20, 12, 20, 16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _status.Items.AddRange(["Tất cả trạng thái", "Nháp", "Hoàn tất", "Đã hủy"]);
        _status.SelectedIndex = 0;
        _filters.Controls.AddRange([LiveUi.Field("Mã phiếu", _search), LiveUi.Field("Kho", _warehouse),
            LiveUi.Field("Trạng thái", _status), LiveUi.Field("Từ ngày (tùy chọn)", _from), LiveUi.Field("Đến ngày (tùy chọn)", _to)]);
        AddAction("Làm mới", LoadAsync, "arrow-clockwise");
        AddAction("Bỏ bộ lọc", async () =>
        {
            _search.Clear(); _status.SelectedIndex = 0; _warehouse.SelectedIndex = 0;
            _from.Checked = _to.Checked = false;
            await LoadAsync();
        });
        AddAction("Tạo phiếu", () => OpenAsync(null, true), "plus-lg");
        AddAction("Xem chi tiết", () => OpenAsync(SelectedId(), false), "eye");
        AddAction("Sửa nháp", () => OpenAsync(SelectedId(), true), "pencil");
        AddAction("Hoàn tất", () => PostAsync(false));
        AddAction("Hủy nháp", () => PostAsync(true));
        _actions.WrapContents = true;
        _actions.AutoScroll = false;
        foreach (Control button in _actions.Controls) button.Width = 130;
        _actions.Layout += (_, _) =>
        {
            var bottom = _actions.Controls.Cast<Control>().Select(c => c.Bottom + c.Margin.Bottom).DefaultIfEmpty(44).Max();
            var height = Math.Max(54, bottom + 8);
            if (root.RowStyles[1].Height != height) root.RowStyles[1].Height = height;
        };
        var columns = new List<(string, string)> { ("ReceiptCode", "Mã phiếu") };
        if (import) columns.Add(("SupplierName", "Nhà cung cấp"));
        columns.AddRange([("WarehouseName", "Kho"), ("CreatorName", "Người tạo"),
            ("ReceiptDate", "Ngày lập"), ("TotalAmount", "Tổng tiền (đ)"), ("Status", "Trạng thái")]);
        _grid = LiveUi.Grid("dgvReceipts", columns.ToArray());
        LiveUi.Column(_grid, "TotalAmount").DefaultCellStyle.Format = "N2";
        LiveUi.Column(_grid, "TotalAmount").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        LiveUi.Column(_grid, "ReceiptDate").DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is string value)
                e.Value = LiveUi.StatusText(value);
        };
        _grid.SelectionChanged += async (_, _) => { if (!_busy) await UpdateActionsAsync(); };
        for (int i = 2; i < _actions.Controls.Count; i++) _actions.Controls[i].Enabled = false;
        root.Controls.Add(_filters, 0, 0);
        root.Controls.Add(_actions, 0, 1);
        root.Controls.Add(_message, 0, 2);
        root.Controls.Add(_grid, 0, 3);
        Controls.Add(root);
        Load += async (_, _) => await LoadAsync();
        VisibleChanged += async (_, _) =>
        {
            if (!Visible && _initialized) _hiddenAfterLoad = true;
            else if (Visible && _hiddenAfterLoad)
            {
                _hiddenAfterLoad = false;
                await LoadAsync();
            }
        };
    }

    private void AddAction(string text, Func<Task> action, string? icon = null)
    {
        var button = ProductUi.Button("btn" + _actions.Controls.Count, text, icon);
        button.Click += async (_, _) => { if (!_busy) await action(); };
        _actions.Controls.Add(button);
    }

    private int? SelectedId() => _grid.CurrentRow?.DataBoundItem switch
    {
        ImportReceiptListItemDto item => item.ImportReceiptId,
        ExportReceiptListItemDto item => item.ExportReceiptId,
        _ => null
    };

    private string? SelectedStatus() => _grid.CurrentRow?.DataBoundItem switch
    {
        ImportReceiptListItemDto item => item.Status,
        ExportReceiptListItemDto item => item.Status,
        _ => null
    };

    private void Busy(bool value)
    {
        _busy = value;
        _actions.Enabled = _filters.Enabled = _grid.Enabled = !value;
    }

    private async Task LoadAsync()
    {
        if (_busy) return;
        _initialized = true;
        if (_from.Checked && _to.Checked && _from.Value.Date > _to.Value.Date)
        { _message.Text = "Ngày bắt đầu không được sau ngày kết thúc."; return; }
        Busy(true);
        _message.Text = "Đang tải danh sách phiếu…";
        try
        {
            if (_warehouse.DataSource is null)
            {
                var warehouses = await _services.RunAsync(() => _services.Warehouses.GetActiveAsync(_services.Session));
                if (IsDisposed) return;
                _warehouse.DisplayMember = nameof(LookupItemDto.Name);
                _warehouse.ValueMember = nameof(LookupItemDto.Id);
                _warehouse.DataSource = new[] { new LookupItemDto(0, "", "Tất cả kho") }.Concat(warehouses).ToList();
            }
            var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id;
            if (warehouseId == 0) warehouseId = null;
            var status = _status.SelectedIndex switch { 1 => "DRAFT", 2 => "COMPLETED", 3 => "CANCELLED", _ => null };
            var from = _from.Checked ? _from.Value.Date : (DateTime?)null;
            var to = _to.Checked ? _to.Value.Date : (DateTime?)null;
            var search = _search.Text.Trim();
            if (_import)
            {
                var rows = await _services.RunAsync(() => _services.Imports.GetListAsync(_services.Session, warehouseId, status, from, to, search));
                if (IsDisposed) return;
                _grid.DataSource = rows.ToList();
            }
            else
            {
                var rows = await _services.RunAsync(() => _services.Exports.GetListAsync(_services.Session, warehouseId, status, from, to, search));
                if (IsDisposed) return;
                _grid.DataSource = rows.ToList();
            }
            var filtered = warehouseId.HasValue || status is not null || from.HasValue || to.HasValue || search.Length > 0;
            _message.Text = _grid.Rows.Count == 0
                ? (filtered ? "Không có phiếu phù hợp bộ lọc. Thử bỏ bộ lọc." : "Chưa có phiếu. Chọn Tạo phiếu để bắt đầu.")
                : $"{_grid.Rows.Count:N0} phiếu • Chọn một dòng để xem hoặc thao tác.";
        }
        catch (Exception ex) { if (!IsDisposed) { _grid.DataSource = null; _message.Text = LiveUi.LoadError(ex); } }
        finally { if (!IsDisposed) Busy(false); }
        if (!IsDisposed) await UpdateActionsAsync();
    }

    private async Task UpdateActionsAsync()
    {
        for (int i = 2; i < _actions.Controls.Count; i++) _actions.Controls[i].Enabled = false;
        _actions.Controls[2].Enabled = _services.Authorization.IsAdmin(_services.Session)
            || _services.Authorization.IsWarehouseManager(_services.Session);
        var id = SelectedId();
        if (!id.HasValue) return;
        try
        {
            var rights = await _services.RunAsync(async () =>
            {
                int? warehouseId = _import
                    ? (await _services.Imports.GetByIdAsync(_services.Session, id.Value))?.WarehouseId
                    : (await _services.Exports.GetByIdAsync(_services.Session, id.Value))?.WarehouseId;
                if (!warehouseId.HasValue) return (false, false, false);
                return (await _services.Authorization.CanEditDraftReceiptAsync(_services.Session, warehouseId.Value),
                    await _services.Authorization.CanCompleteReceiptAsync(_services.Session, warehouseId.Value),
                    await _services.Authorization.CanCancelReceiptAsync(_services.Session, warehouseId.Value));
            });
            if (IsDisposed || _busy || SelectedId() != id) return;
            _actions.Controls[3].Enabled = true;
            var draft = SelectedStatus() == "DRAFT";
            _actions.Controls[4].Enabled = draft && rights.Item1;
            _actions.Controls[5].Enabled = draft && rights.Item2;
            _actions.Controls[6].Enabled = draft && rights.Item3;
        }
        catch (Exception ex) { if (!IsDisposed) _message.Text = LiveUi.Error(ex); }
    }

    private async Task OpenAsync(int? id, bool edit)
    {
        if (!id.HasValue && !edit) { _message.Text = "Chọn phiếu cần xem."; return; }
        if (id.HasValue && edit && SelectedStatus() != "DRAFT")
        { _message.Text = "Chỉ phiếu nháp được sửa."; return; }
        using Form detail = _import ? new FrmImportDetail(_services, id, edit) : new FrmExportDetail(_services, id, edit);
        detail.ShowDialog(FindForm());
        await LoadAsync();
    }

    private async Task PostAsync(bool cancel)
    {
        var id = SelectedId();
        if (!id.HasValue || SelectedStatus() != "DRAFT")
        { _message.Text = "Chọn một phiếu nháp để thao tác."; return; }
        var code = _grid.CurrentRow?.Cells["ReceiptCode"].Value?.ToString();
        var action = cancel ? "Hủy phiếu nháp" : "Hoàn tất phiếu và cập nhật tồn kho";
        if (MessageBox.Show(FindForm(), $"{action} {code}?", "Xác nhận", MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
        Busy(true);
        _message.Text = "Đang xử lý phiếu…";
        var committed = false;
        try
        {
            await _services.RunAsync(async () =>
            {
                if (_import)
                {
                    if (cancel) await _services.Imports.CancelDraftAsync(_services.Session, id.Value);
                    else await _services.Imports.CompleteAsync(_services.Session, id.Value);
                }
                else
                {
                    if (cancel) await _services.Exports.CancelDraftAsync(_services.Session, id.Value);
                    else await _services.Exports.CompleteAsync(_services.Session, id.Value);
                }
            });
            committed = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            // CompleteAsync có thể lỗi cập nhật cảnh báo sau commit: đối chiếu trạng thái trước khi báo lỗi.
            try
            {
                var actual = await _services.RunAsync(async () => _import
                    ? (await _services.Imports.GetByIdAsync(_services.Session, id.Value))?.Status
                    : (await _services.Exports.GetByIdAsync(_services.Session, id.Value))?.Status);
                committed = actual == (cancel ? "CANCELLED" : "COMPLETED");
            }
            catch (Exception readError) { System.Diagnostics.Debug.WriteLine(readError); }
            if (!committed && !IsDisposed) _message.Text = LiveUi.Error(ex);
        }
        finally { if (!IsDisposed) Busy(false); }
        if (IsDisposed || !committed) return;
        await LoadAsync();
        if (!IsDisposed) _message.Text = $"Phiếu đã {(cancel ? "hủy" : "hoàn tất")}. " + _message.Text;
    }
}
