using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmWarehouseAssignments : Form
{
    private readonly UiServices _services;
    private readonly UserListItemDto _user;
    private readonly ComboBox _warehouse;
    private readonly Label _current;
    private readonly Label _message;
    private readonly AntdUI.Button _save;
    private bool _busy;

    public FrmWarehouseAssignments(UiServices services, UserListItemDto user)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(user);
        _services = services;
        _user = user;
        Text = "Phân kho — " + user.Username;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(480, 380);
        ClientSize = new Size(600, 360);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        WorkflowUi.StyleForm(this);
        var layout = ProductUi.Layout(1, 7);
        layout.Padding = new Padding(24);
        foreach (var height in new[] { 40, 28, 60, 28, 42 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var identity = ProductUi.Label($"{user.FullName} ({user.Username})", 12, true);
        identity.Name = "lblAssignmentUser";
        layout.Controls.Add(identity, 0, 0);
        layout.Controls.Add(ProductUi.Label("Kho đang được phân công"), 0, 1);
        _current = ProductUi.Label("Đang tải…");
        _current.Name = "lblCurrentAssignment";
        _current.AutoEllipsis = false;
        layout.Controls.Add(_current, 0, 2);
        layout.Controls.Add(ProductUi.Label("Kho làm việc mới"), 0, 3);
        _warehouse = LiveUi.Choice("cboAssignmentWarehouse", "Kho làm việc mới");
        _warehouse.DisplayMember = "Name";
        _warehouse.ValueMember = "Id";
        _warehouse.Dock = DockStyle.Top;
        _warehouse.Margin = new Padding(0, 4, 0, 0);
        _warehouse.Enabled = false;
        layout.Controls.Add(_warehouse, 0, 4);
        _message = ProductUi.Label("");
        _message.Name = "lblAssignmentMessage";
        _message.AutoEllipsis = false;
        layout.Controls.Add(_message, 0, 5);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnAssignmentClose", "Đóng");
        close.Click += (_, _) => Close();
        _save = ProductUi.Button("btnAssignmentSave", "Lưu phân công", "building", true);
        _save.Enabled = false;
        _save.Click += async (_, _) => await SaveAsync();
        actions.Controls.Add(close);
        actions.Controls.Add(_save);
        layout.Controls.Add(actions, 0, 6);
        Controls.Add(layout);
        _warehouse.SelectedIndexChanged += (_, _) => UpdateSave();
        LiveUi.OnLookupSearch(_warehouse, UpdateSave);
        Shown += async (_, _) => await LoadAsync();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private void UpdateSave() => _save.Enabled = !_busy && _warehouse.Enabled
        && !LiveUi.HasPendingSearch(_warehouse) && _warehouse.SelectedItem is LookupItemDto { Id: > 0 };

    private async Task LoadAsync()
    {
        _busy = true;
        UpdateSave();
        try
        {
            if (_user.RoleName != RoleNames.Staff || !_user.IsActive)
                throw new InvalidOperationException("Chỉ phân kho cho tài khoản nhân viên đang hoạt động.");
            var data = await _services.RunAsync(async () => (
                Assignments: await _services.Assignments.GetAsync(_services.Session, _user.UserId),
                Warehouses: await _services.Warehouses.GetAllAsync(_services.Session)));
            var active = data.Assignments.Where(item => item.IsActive).ToList();
            _current.Text = active.Count == 0 ? "Chưa phân công" : string.Join(", ", active.Select(item =>
                item.WarehouseName + (data.Warehouses.Any(w => w.WarehouseId == item.WarehouseId && w.IsActive) ? "" : " (ngừng hoạt động)")));
            _warehouse.DataSource = new[] { new LookupItemDto(0, "", "Chọn kho") }
                .Concat(data.Warehouses.Where(row => row.IsActive)
                    .Select(row => new LookupItemDto(row.WarehouseId, row.WarehouseCode, row.WarehouseName))).ToList();
            if (active.Count == 1 && data.Warehouses.Any(w => w.WarehouseId == active[0].WarehouseId && w.IsActive))
                _warehouse.SelectedValue = active[0].WarehouseId;
            _warehouse.Enabled = true;
            _message.Text = active.Count > 1 ? "Có nhiều phân công cũ. Chọn một kho để giữ làm kho làm việc."
                : "Mỗi nhân viên được phân công một kho làm việc.";
        }
        catch (Exception ex)
        {
            _current.Text = "Chưa tải được phân công";
            _message.Text = LiveUi.Error(ex);
        }
        finally { _busy = false; UpdateSave(); }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;
        if (LiveUi.HasPendingSearch(_warehouse) || _warehouse.SelectedItem is not LookupItemDto { Id: > 0 } selected)
        {
            _message.Text = "Vui lòng chọn kho trong danh sách.";
            _warehouse.Focus();
            return;
        }
        _busy = true;
        _warehouse.Enabled = false;
        UpdateSave();
        try
        {
            await _services.RunAsync(() => _services.Assignments.SetActiveAsync(_services.Session, _user.UserId, selected.Id, true));
            _current.Text = selected.Name;
            _message.Text = "Đã lưu phân công.";
        }
        catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
        finally { _busy = false; _warehouse.Enabled = true; UpdateSave(); }
    }
}
