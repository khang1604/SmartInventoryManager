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
        AddAction("Tạo phiếu", () => OpenAsync(null, true), "plus-lg");
        AddAction("Xem chi tiết", () => OpenAsync(SelectedId(), false), "eye");
        AddAction("Sửa phiếu", () => OpenAsync(SelectedId(), true), "pencil");
        AddAction("Gửi duyệt", () => TransitionAsync("submit"));
        AddAction("Duyệt", () => TransitionAsync("approve"));
        AddAction("Từ chối", () => TransitionAsync("reject"));
        AddAction("Gửi lại", () => TransitionAsync("resubmit"));
        AddAction("Hoàn tất", () => TransitionAsync("complete"));
        AddAction("Hủy phiếu", () => PostAsync(true));
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
        root.Controls.Add(LiveUi.TableFrame(_grid), 0, 3);
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
                if(_services.Authorization.IsStaff(_services.Session) && _services.Session.CurrentWarehouseId.HasValue) _warehouse.SelectedValue=_services.Session.CurrentWarehouseId.Value;
            }
            var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id;
            if (warehouseId == 0) warehouseId = null;
            var status = _status.SelectedIndex switch { 1 => "DRAFT", 2 => "PENDING_APPROVAL", 3 => "APPROVED", 4 => "REJECTED", 5 => "COMPLETED", 6 => "CANCELLED", _ => null };
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
            || _services.Authorization.IsWarehouseManager(_services.Session)
            || (_services.Authorization.IsStaff(_services.Session) && _services.Session.CurrentWarehouseId.HasValue);
        var id = SelectedId();
        if (!id.HasValue) return;
        try
        {
            var rights = await _services.RunAsync(async () =>
            {
                int warehouseId, creatorId;
                if (_import) { var receipt = await _services.Imports.GetByIdAsync(_services.Session, id.Value); if (receipt is null) return (false,false,false,false); warehouseId=receipt.WarehouseId; creatorId=receipt.CreatedBy; }
                else { var receipt = await _services.Exports.GetByIdAsync(_services.Session, id.Value); if (receipt is null) return (false,false,false,false); warehouseId=receipt.WarehouseId; creatorId=receipt.CreatedBy; }
                return (await _services.Authorization.CanEditReceiptAsync(_services.Session, warehouseId, creatorId, SelectedStatus() ?? ""),
                    await _services.Authorization.CanApproveReceiptAsync(_services.Session, warehouseId),
                    await _services.Authorization.CanCompleteReceiptAsync(_services.Session, warehouseId),
                    await _services.Authorization.CanCancelReceiptAsync(_services.Session, warehouseId, creatorId, SelectedStatus() ?? ""));
            });
            if (IsDisposed || _busy || SelectedId() != id) return;
            void Enable(string text, bool enabled) { foreach(Control button in _actions.Controls) if(button.Text==text) button.Enabled=enabled; }
            var status = SelectedStatus();
            Enable("Xem chi tiết", true);
            Enable("Sửa phiếu", (status is "DRAFT" or "REJECTED") && rights.Item1);
            Enable("Gửi duyệt", status=="DRAFT" && rights.Item1);
            Enable("Duyệt", status=="PENDING_APPROVAL" && rights.Item2);
            Enable("Từ chối", status=="PENDING_APPROVAL" && rights.Item2);
            Enable("Gửi lại", status=="REJECTED" && rights.Item1);
            Enable("Hoàn tất", status=="APPROVED" && rights.Item3);
            Enable("Hủy phiếu", status is "DRAFT" or "PENDING_APPROVAL" or "APPROVED" && rights.Item4);
        }
        catch (Exception ex) { if (!IsDisposed) _message.Text = LiveUi.Error(ex); }
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
        if (transition=="complete" && MessageBox.Show(FindForm(),"Hoàn tất phiếu đã duyệt và cập nhật tồn kho?","Xác nhận",MessageBoxButtons.OKCancel,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.OK) return;
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
            updated=true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            var expected=transition switch { "submit"=>"PENDING_APPROVAL", "approve"=>"APPROVED", "reject"=>"REJECTED", "resubmit"=>"DRAFT", _=>"COMPLETED" };
            try { var actual=await _services.RunAsync(async()=>_import ? (await _services.Imports.GetByIdAsync(_services.Session,id.Value))?.Status : (await _services.Exports.GetByIdAsync(_services.Session,id.Value))?.Status); updated=actual==expected; }
            catch(Exception readError) { System.Diagnostics.Debug.WriteLine(readError); }
            if(!updated && !IsDisposed) _message.Text=LiveUi.Error(ex);
        }
        finally { if (!IsDisposed) Busy(false); }
        if(updated && !IsDisposed) { await LoadAsync(); if(!IsDisposed) _message.Text="Đã cập nhật trạng thái phiếu. " + _message.Text; }
    }

    private async Task PostAsync(bool cancel)
    {
        var id = SelectedId();
        if (!id.HasValue || (cancel && SelectedStatus() is not ("DRAFT" or "REJECTED") && SelectedStatus() != "PENDING_APPROVAL" && SelectedStatus() != "APPROVED"))
        { _message.Text = "Chọn một phiếu hợp lệ để thao tác."; return; }
        var code = _grid.CurrentRow?.Cells["ReceiptCode"].Value?.ToString();
        var action = cancel ? "Hủy phiếu" : "Hoàn tất phiếu và cập nhật tồn kho";
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
                    else throw new InvalidOperationException("Phiếu phải được duyệt trước khi hoàn tất.");
                }
                else
                {
                    if (cancel) await _services.Exports.CancelDraftAsync(_services.Session, id.Value);
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
}
