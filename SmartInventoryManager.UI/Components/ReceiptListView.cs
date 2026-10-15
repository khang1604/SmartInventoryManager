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
    private AntdUI.Button _create = null!;
    private bool _busy;
    private bool _initialized;
    private bool _hiddenAfterLoad;
    private int _actionsVersion;
    private int _page = 1;
    private (int?, string?, DateTime?, DateTime?, string, int)? _lastFilter;
    private readonly AntdUI.Button _previous = ProductUi.Button("btnPreviousPage", "Trang trước", "chevron-left");
    private readonly AntdUI.Button _next = ProductUi.Button("btnNextPage", "Trang sau", "chevron-right");

    public ReceiptListView(UiServices services, bool import)
    {
        _services = services;
        _import = import;
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(1, 5);
        LiveUi.KeepTableVisible(this, root);
        root.Padding = new Padding(20, 12, 20, 16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var pager = LiveUi.Toolbar();
        _previous.Enabled = _next.Enabled = false;
        pager.Controls.AddRange([_previous, _next]);
        _previous.Click += async (_, _) => { if (_busy || _page <= 1) return; _page--; await LoadAsync(); };
        _next.Click += async (_, _) => { if (_busy || !_next.Enabled) return; _page++; await LoadAsync(); };
        _status.Items.AddRange(["Tất cả trạng thái", "Nháp", "Chờ duyệt", "Đã duyệt", "Bị từ chối", "Hoàn tất", "Đã hủy"]);
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
        _create = AddAction("Tạo phiếu", () => OpenAsync(null, true), "plus-lg");
        AddAction("Xem chi tiết", () => OpenAsync(SelectedId(), false), "eye");
        AddAction("Xuất PDF phiếu", ExportReceiptAsync);
        AddAction("Lịch sử từ chối", async () => { var id = SelectedId(); if (id.HasValue) { using var form = new ReceiptRejectionHistoryForm(_services, _import, id.Value); form.ShowDialog(FindForm()); } });
        AddAction("Lịch sử xử lý", async () => { var id = SelectedId(); if (id.HasValue) { using var form = new ReceiptRejectionHistoryForm(_services, _import, id.Value, statusHistory: true); form.ShowDialog(FindForm()); } });
        AddAction("Sửa phiếu", () => OpenAsync(SelectedId(), true), "pencil");
        AddAction("Gửi duyệt", () => TransitionAsync("submit"));
        AddAction("Duyệt", () => TransitionAsync("approve"));
        AddAction("Từ chối", () => TransitionAsync("reject"));
        AddAction("Gửi lại", () => TransitionAsync("resubmit"));
        AddAction("Hoàn tất", () => TransitionAsync("complete"));
        AddAction("Hủy phiếu", () => PostAsync(true));
        AddAction("Xóa nháp", () => DeleteDraftAsync(), "trash");
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
        _grid.SelectionChanged += async (_, _) =>
        {
            if (!_busy)
            {
                await UpdateActionsAsync();
            }
        };
        for (int i = 2; i < _actions.Controls.Count; i++) _actions.Controls[i].Enabled = false;
        root.Controls.Add(_filters, 0, 0);
        LiveUi.FitWrappedToolbar(_filters, root, 0);
        root.Controls.Add(_actions, 0, 1);
        root.Controls.Add(_message, 0, 2);
        root.Controls.Add(LiveUi.TableFrame(_grid), 0, 3);
        root.Controls.Add(pager, 0, 4);
        Controls.Add(root);
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await LoadAsync();
        };
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

    private AntdUI.Button AddAction(string text, Func<Task> action, string? icon = null)
    {
        var button = ProductUi.Button("btn" + _actions.Controls.Count, text, icon);
        button.Click += async (_, _) => { if (!_busy) await action(); };
        _actions.Controls.Add(button);
        return button;
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
        _previous.Parent!.Enabled = !value;
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
                if (_services.Authorization.IsStaff(_services.Session) && _services.Session.CurrentWarehouseId.HasValue) _warehouse.SelectedValue = _services.Session.CurrentWarehouseId.Value;
            }
            var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id;
            if (warehouseId == 0) warehouseId = null;
            var status = _status.SelectedIndex switch { 1 => "DRAFT", 2 => "PENDING_APPROVAL", 3 => "APPROVED", 4 => "REJECTED", 5 => "COMPLETED", 6 => "CANCELLED", _ => null };
            var from = _from.Checked ? _from.Value.Date : (DateTime?)null;
            var to = _to.Checked ? _to.Value.Date : (DateTime?)null;
            var search = _search.Text.Trim();
            var size = Math.Clamp(UiPreferences.Current.PageSize, 1, 100);
            var filter = (warehouseId, status, from, to, search, size);
            if (_lastFilter != filter) { _page = 1; _lastFilter = filter; }
            var request = new ReceiptSearchDto { WarehouseId = warehouseId, Status = status, From = from, To = to, Search = search, PageNumber = _page, PageSize = size };
            int total;
            if (_import)
            {
                var result = await _services.RunAsync(() => _services.Imports.GetPageAsync(_services.Session, request));
                if (IsDisposed) return;
                _grid.DataSource = result.Items.ToList(); _page = result.PageNumber; total = result.TotalCount;
            }
            else
            {
                var result = await _services.RunAsync(() => _services.Exports.GetPageAsync(_services.Session, request));
                if (IsDisposed) return;
                _grid.DataSource = result.Items.ToList(); _page = result.PageNumber; total = result.TotalCount;
            }
            _previous.Enabled = _page > 1; _next.Enabled = (long)_page * size < total;
            var filtered = warehouseId.HasValue || status is not null || from.HasValue || to.HasValue || search.Length > 0;
            _message.Text = _grid.Rows.Count == 0
                ? (filtered ? "Không có phiếu phù hợp bộ lọc. Thử bỏ bộ lọc." : "Chưa có phiếu. Chọn Tạo phiếu để bắt đầu.")
                : $"Trang {_page:N0}/{Math.Max(1, (total + (long)size - 1) / size):N0} • Tổng {total:N0} phiếu • {_grid.Rows.Count:N0} phiếu trên trang.";
        }
        catch (Exception ex) { if (!IsDisposed) { _grid.DataSource = null; _message.Text = LiveUi.LoadError(ex); } }
        finally { if (!IsDisposed) Busy(false); }
        if (!IsDisposed) await UpdateActionsAsync();
    }

    private async Task UpdateActionsAsync()
    {
        var version = ++_actionsVersion;
        for (int i = 2; i < _actions.Controls.Count; i++) _actions.Controls[i].Enabled = false;
        _create.Enabled = _services.Authorization.IsAdmin(_services.Session)
            || _services.Authorization.IsWarehouseManager(_services.Session)
            || (_services.Authorization.IsStaff(_services.Session) && _services.Session.CurrentWarehouseId.HasValue);
        var id = SelectedId();
        if (!id.HasValue) return;

        EnableAction("Xem chi tiết", true);
        EnableAction("Xuất PDF phiếu", true);
        EnableAction("Lịch sử từ chối", true);
        EnableAction("Lịch sử xử lý", true);
        try
        {
            var rights = await _services.RunAsync(async () =>
            {
                int warehouseId, creatorId;
                if (_import) { var receipt = await _services.Imports.GetByIdAsync(_services.Session, id.Value); if (receipt is null) return (false, false, false, false); warehouseId = receipt.WarehouseId; creatorId = receipt.CreatedBy; }
                else { var receipt = await _services.Exports.GetByIdAsync(_services.Session, id.Value); if (receipt is null) return (false, false, false, false); warehouseId = receipt.WarehouseId; creatorId = receipt.CreatedBy; }
                return (await _services.Authorization.CanEditReceiptAsync(_services.Session, warehouseId, creatorId, SelectedStatus() ?? ""),
                    await _services.Authorization.CanApproveReceiptAsync(_services.Session, warehouseId),
                    await _services.Authorization.CanCompleteReceiptAsync(_services.Session, warehouseId),
                    await _services.Authorization.CanCancelReceiptAsync(_services.Session, warehouseId, creatorId, SelectedStatus() ?? ""));
            });
            if (IsDisposed || _busy || version != _actionsVersion || SelectedId() != id) return;
            var status = SelectedStatus()?.Trim().ToUpperInvariant();
            EnableAction("Sửa phiếu", (status is "DRAFT" or "REJECTED") && rights.Item1);
            EnableAction("Gửi duyệt", status == "DRAFT" && rights.Item1);
            EnableAction("Duyệt", status == "PENDING_APPROVAL" && rights.Item2);
            EnableAction("Từ chối", status == "PENDING_APPROVAL" && rights.Item2);
            EnableAction("Gửi lại", status == "REJECTED" && rights.Item1);
            EnableAction("Hoàn tất", status == "APPROVED" && rights.Item3);
            EnableAction("Hủy phiếu", status is "DRAFT" or "PENDING_APPROVAL" or "APPROVED" && rights.Item4);
            EnableAction("Xóa nháp", status == "DRAFT" && rights.Item1);
        }
        catch (Exception ex) { if (!IsDisposed) _message.Text = LiveUi.Error(ex); }
    }

    private async Task ExportReceiptAsync()
    {
        var id = SelectedId();
        if (_busy || !id.HasValue) return;
        using var dialog = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", DefaultExt = "pdf", AddExtension = true, FileName = $"{(_import ? "Import" : "Export")}-{id.Value}.pdf" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _busy = true;
        try
        {
            if (_import)
            {
                var receipt = await _services.RunAsync(() => _services.Imports.GetByIdAsync(_services.Session, id.Value)) ?? throw new KeyNotFoundException("Không tìm thấy phiếu trong phạm vi kho.");
                ReportExportService.ExportReceiptPdf(dialog.FileName, "PHIẾU NHẬP KHO", receipt.ReceiptCode, receipt.ReceiptDate, receipt.Status,
                    receipt.WarehouseName, receipt.CreatorName, receipt.SupplierName, receipt.Note, receipt.Details, receipt.TotalAmount, receipt.Audit);
            }
            else
            {
                var receipt = await _services.RunAsync(() => _services.Exports.GetByIdAsync(_services.Session, id.Value)) ?? throw new KeyNotFoundException("Không tìm thấy phiếu trong phạm vi kho.");
                ReportExportService.ExportReceiptPdf(dialog.FileName, "PHIẾU XUẤT KHO", receipt.ReceiptCode, receipt.ReceiptDate, receipt.Status,
                    receipt.WarehouseName, receipt.CreatorName, null, receipt.Note, receipt.Details, receipt.TotalAmount, receipt.Audit);
            }
            _message.Text = "Đã xuất PDF phiếu. Mở file để xem hoặc in A4.";
        }
        catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
        finally { _busy = false; await UpdateActionsAsync(); }
    }

    private void EnableAction(string text, bool enabled)
    {
        foreach (Control button in _actions.Controls)
        {
            if (button.Text == text)
            {
                button.Enabled = enabled;
            }
        }
    }

    private async Task OpenAsync(int? id, bool edit)
    {
        if (!id.HasValue && !edit) { _message.Text = "Chọn phiếu cần xem."; return; }
        if (id.HasValue && edit && SelectedStatus() is not ("DRAFT" or "REJECTED"))
        { _message.Text = "Chỉ phiếu nháp hoặc bị từ chối được sửa."; return; }
        using Form detail = _import ? new FrmImportDetail(_services, id, edit) : new FrmExportDetail(_services, id, edit);
        detail.ShowDialog(FindForm());
        await LoadAsync();
    }

    private async Task TransitionAsync(string transition)
    {
        if (_busy) return;
        var id = SelectedId();
        if (!id.HasValue) { _message.Text = "Chọn một phiếu để thao tác."; return; }
        if (transition == "complete" && MessageBox.Show(FindForm(), "Hoàn tất phiếu đã duyệt và cập nhật tồn kho?", "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
        var reason = transition == "reject" ? Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do từ chối:", "Từ chối phiếu", "") : string.Empty;
        if (transition == "reject" && string.IsNullOrWhiteSpace(reason)) return;
        Busy(true);
        var updated = false;
        try
        {
            await _services.RunAsync(async () =>
            {
                if (_import)
                {
                    if (transition == "submit") await _services.Imports.SubmitAsync(_services.Session, id.Value);
                    else if (transition == "approve") await _services.Imports.ApproveAsync(_services.Session, id.Value);
                    else if (transition == "reject") await _services.Imports.RejectAsync(_services.Session, id.Value, reason);
                    else if (transition == "resubmit") await _services.Imports.ResubmitAsync(_services.Session, id.Value);
                    else await _services.Imports.CompleteAsync(_services.Session, id.Value);
                }
                else
                {
                    if (transition == "submit") await _services.Exports.SubmitAsync(_services.Session, id.Value);
                    else if (transition == "approve") await _services.Exports.ApproveAsync(_services.Session, id.Value);
                    else if (transition == "reject") await _services.Exports.RejectAsync(_services.Session, id.Value, reason);
                    else if (transition == "resubmit") await _services.Exports.ResubmitAsync(_services.Session, id.Value);
                    else await _services.Exports.CompleteAsync(_services.Session, id.Value);
                }
            });
            updated = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            var expected = transition switch { "submit" => "PENDING_APPROVAL", "approve" => "APPROVED", "reject" => "REJECTED", "resubmit" => "DRAFT", _ => "COMPLETED" };
            try { var actual = await _services.RunAsync(async () => _import ? (await _services.Imports.GetByIdAsync(_services.Session, id.Value))?.Status : (await _services.Exports.GetByIdAsync(_services.Session, id.Value))?.Status); updated = actual == expected; }
            catch (Exception readError) { System.Diagnostics.Debug.WriteLine(readError); }
            if (!updated && !IsDisposed) _message.Text = LiveUi.Error(ex);
        }
        finally { if (!IsDisposed) Busy(false); }
        if (updated && !IsDisposed) { await LoadAsync(); if (!IsDisposed) _message.Text = "Đã cập nhật trạng thái phiếu. " + _message.Text; }
    }

    private async Task PostAsync(bool cancel)
    {
        var id = SelectedId();
        var status = SelectedStatus()?.Trim().ToUpperInvariant();
        if (!id.HasValue || (cancel && status is not ("DRAFT" or "PENDING_APPROVAL" or "APPROVED")))
        { _message.Text = "Chọn một phiếu hợp lệ để thao tác."; return; }
        var code = _grid.CurrentRow?.Cells["ReceiptCode"].Value?.ToString();
        var action = cancel ? "Hủy phiếu" : "Hoàn tất phiếu và cập nhật tồn kho";
        if (MessageBox.Show(FindForm(), $"{action} {code}?", "Xác nhận", MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
        var reason = Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do hủy phiếu (bắt buộc):", "Hủy phiếu", "");
        if (string.IsNullOrWhiteSpace(reason)) { _message.Text = "Chưa nhập lý do; phiếu chưa bị hủy."; return; }
        Busy(true);
        _message.Text = "Đang xử lý phiếu…";
        var committed = false;
        try
        {
            await _services.RunAsync(async () =>
            {
                if (_import)
                {
                    if (cancel) await _services.Imports.CancelAsync(_services.Session, id.Value, reason);
                    else throw new InvalidOperationException("Phiếu phải được duyệt trước khi hoàn tất.");
                }
                else
                {
                    if (cancel) await _services.Exports.CancelAsync(_services.Session, id.Value, reason);
                    else throw new InvalidOperationException("Phiếu phải được duyệt trước khi hoàn tất.");
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

    private async Task DeleteDraftAsync()
    {
        var id = SelectedId();
        if (!id.HasValue || !string.Equals(SelectedStatus()?.Trim(), "DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            _message.Text = "Chỉ được xóa phiếu đang ở trạng thái nháp.";
            return;
        }

        var code = _grid.CurrentRow?.Cells["ReceiptCode"].Value?.ToString();
        if (MessageBox.Show(FindForm(), $"Xóa vĩnh viễn phiếu nháp {code}?", "Xác nhận xóa phiếu",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.OK)
            return;

        Busy(true);
        try
        {
            await _services.RunAsync(() => _import
                ? _services.Imports.DeleteDraftAsync(_services.Session, id.Value)
                : _services.Exports.DeleteDraftAsync(_services.Session, id.Value));
            Busy(false);
            await LoadAsync();
            if (!IsDisposed) _message.Text = "Đã xóa phiếu nháp.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _message.Text = LiveUi.Error(ex);
        }
        finally
        {
            if (!IsDisposed) Busy(false);
        }
    }
}
