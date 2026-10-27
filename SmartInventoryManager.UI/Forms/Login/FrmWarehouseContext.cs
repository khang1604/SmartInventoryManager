using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI.Forms.Login;

internal sealed class FrmWarehouseContext : Form
{
    private readonly UiServices _services;
    private readonly ComboBox _warehouse = LiveUi.Choice("cboCurrentWarehouse", "Kho làm việc");
    private readonly Label _message = ProductUi.Label("Đang tải các kho được phép…");
    private readonly AntdUI.Button _continue = ProductUi.Button("btnContinue", "Vào ứng dụng", primary: true);
    private bool _loading;

    // Khởi tạo màn hình chọn kho làm việc của nhân viên.
    public FrmWarehouseContext(UiServices services)
    {
        _services = services;
        WorkflowUi.StyleForm(this);
        Text = "VioWare - Chọn kho làm việc";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        var root = ProductUi.Layout(1, 4);
        root.Padding = new Padding(20);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(ProductUi.Label("Chọn kho làm việc được phân công", 12, true), 0, 0);
        _warehouse.Dock = DockStyle.Top;
        _warehouse.Enabled = false;
        root.Controls.Add(_warehouse, 0, 1);
        root.Controls.Add(_message, 0, 2);
        _continue.Enabled = false;
        _continue.Dock = DockStyle.Right;
        root.Controls.Add(_continue, 0, 3);
        Controls.Add(root);
        AcceptButton = _continue;
        _warehouse.SelectedIndexChanged += (_, _) => _continue.Enabled = !_loading && SelectedId > 0;
        _continue.Click += (_, _) => ChooseWarehouse();
        Shown += async (_, _) => await LoadWarehousesAsync();
        FormClosing += (_, e) => { if (_loading) e.Cancel = true; };
    }

    private int SelectedId => (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;

    // Xác nhận lựa chọn kho làm việc hợp lệ rồi đóng cửa sổ chọn kho.
    private void ChooseWarehouse()
    {
        if (_loading || SelectedId <= 0) return;
        _services.Session.SetCurrentWarehouse(SelectedId);
        DialogResult = DialogResult.OK;
        Close();
    }

    // Tải các kho được phân công để người dùng chọn kho làm việc.
    private async Task LoadWarehousesAsync()
    {
        _loading = true;
        try
        {
            var warehouses = await _services.RunAsync(() => _services.Warehouses.GetActiveAsync(_services.Session));
            _warehouse.DisplayMember = nameof(LookupItemDto.Name);
            _warehouse.ValueMember = nameof(LookupItemDto.Id);
            _warehouse.DataSource = new[] { new LookupItemDto(0, "", "Chọn kho làm việc") }.Concat(warehouses).ToList();
            _warehouse.Enabled = warehouses.Count > 0;
            _message.Text = warehouses.Count == 0
                ? "Chưa có kho được phép. Vui lòng liên hệ quản trị viên."
                : "Các thao tác phiếu sẽ sử dụng kho đã chọn.";
            if (warehouses.Count == 1) _warehouse.SelectedValue = warehouses[0].Id;
        }
        catch (Exception ex) { _message.Text = LiveUi.LoadError(ex); }
        finally { _loading = false; _continue.Enabled = SelectedId > 0; }
        if (SelectedId > 0) ChooseWarehouse();
        else _warehouse.Focus();
    }
}
