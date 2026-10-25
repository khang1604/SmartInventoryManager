using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmWarehouseAssignments : Form
{
    private readonly UiServices _services;
    private readonly UserListItemDto _user;
    private readonly AntdUI.SelectMultiple _warehouse;
    private readonly TextBox _current;
    private readonly Label _message;
    private readonly AntdUI.Button _save;
    private bool _busy;
    private bool _loaded;

    public FrmWarehouseAssignments(UiServices services, UserListItemDto user)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(user);
        _services = services;
        _user = user;
        Text = "Phân kho — " + user.Username;
        MinimizeBox = MaximizeBox = ShowInTaskbar = false;
        MinimumSize = new Size(520, 440);
        ClientSize = new Size(660, 480);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        WorkflowUi.StyleForm(this);
        var layout = ProductUi.Layout(1, 7);
        layout.Padding = new Padding(24);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 28, 80, 48, 46 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var identity = ProductUi.Label($"{user.FullName} ({user.Username})", 12, true);
        identity.Name = "lblAssignmentUser";
        layout.Controls.Add(identity, 0, 0);
        layout.Controls.Add(ProductUi.Label("Các kho được phân công"), 0, 1);
        _current = new TextBox { Name = "lblCurrentAssignment", ReadOnly = true, Multiline = true,
            WordWrap = false, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill,
            BackColor = BackColor, ForeColor = ForeColor, Text = "Đang tải…", ScrollBars = ScrollBars.None,
            Margin = new Padding(0, 4, 0, 12), AccessibleName = "Các kho đã lưu phân công" };
        layout.Controls.Add(_current, 0, 2);
        layout.Controls.Add(ProductUi.Label("Chọn các kho làm việc"), 0, 3);
        _warehouse = new AntdUI.SelectMultiple { Name = "cboAssignmentWarehouse", Dock = DockStyle.Fill,
            CheckMode = true, DropDownArrow = true, PlaceholderText = "Chọn một hoặc nhiều kho",
            ListAutoWidth = true, Enabled = false, Margin = new Padding(0, 4, 0, 0) };
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
        Shown += async (_, _) => await LoadAsync();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private async Task RefreshAssignmentsAsync()
    {
        var data = await _services.RunAsync(async () => (
            Assignments: await _services.Assignments.GetAsync(_services.Session, _user.UserId),
            Warehouses: await _services.Warehouses.GetAllAsync(_services.Session)));
        var active = data.Assignments.Where(item => item.IsActive).ToList();
        _current.Text = active.Count == 0 ? "Chưa phân công" : string.Join(Environment.NewLine,
            active.Select(item => item.WarehouseName +
                (data.Warehouses.Any(w => w.WarehouseId == item.WarehouseId && w.IsActive) ? "" : " (ngừng hoạt động)")));
        _warehouse.SelectedValue = [];
        _warehouse.Items.Clear();
        foreach (var row in data.Warehouses.Where(row => row.IsActive || active.Any(a => a.WarehouseId == row.WarehouseId)))
            _warehouse.Items.Add(new AntdUI.SelectItem(row.WarehouseName + (row.IsActive ? "" : " (ngừng hoạt động)"), row.WarehouseId));
        _warehouse.SelectedValue = active.Select(item => (object)item.WarehouseId).ToArray();
        _loaded = true;
    }

    private async Task LoadAsync()
    {
        _busy = true;
        _loaded = false;
        try
        {
            if (_user.RoleName != RoleNames.Staff || !_user.IsActive)
                throw new InvalidOperationException("Chỉ phân kho cho tài khoản nhân viên đang hoạt động.");
            await RefreshAssignmentsAsync();
            _message.Text = "Chọn nhiều kho trong danh sách. Bỏ chọn kho để thu hồi phân công.";
        }
        catch (Exception ex) { _message.Text = LiveUi.LoadError(ex); }
        finally { _busy = false; _save.Enabled = _warehouse.Enabled = _loaded; }
    }

    private async Task SaveAsync()
    {
        if (_busy || !_loaded) return;
        var ids = _warehouse.SelectedValue.OfType<int>().Distinct().ToArray();
        _busy = true;
        _save.Enabled = _warehouse.Enabled = false;
        try
        {
            await _services.RunAsync(() => _services.Assignments.SetWarehousesAsync(_services.Session, _user.UserId, ids));
            _loaded = false;
            try
            {
                await RefreshAssignmentsAsync();
                _message.Text = "Đã lưu phân công.";
            }
            catch (Exception ex) { _message.Text = "Đã lưu phân công. " + LiveUi.LoadError(ex); }
        }
        catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
        finally { _busy = false; _save.Enabled = _warehouse.Enabled = _loaded; }
    }
}
