using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class StockOperationForm : Form
{
    private readonly UiServices _services;
    private readonly InventoryDto _inventory;
    private readonly bool _transfer;
    private readonly NumericUpDown _quantity = new() { Name = "numQuantity", AccessibleName = "Số lượng điều chỉnh hoặc chuyển kho", Width = 240, Minimum = int.MinValue, Maximum = int.MaxValue, ThousandsSeparator = true };
    private readonly ComboBox _destination = LiveUi.Choice("cboDestination", "Kho đích");
    private readonly TextBox _reason = new() { Name = "txtReason", AccessibleName = "Lý do cập nhật tồn kho", Width = 380, MaxLength = 500, Multiline = true, Height = 70 };
    private readonly Label _message = ProductUi.Label("Nhập thông tin và kiểm tra trước khi xác nhận.");
    private readonly Label _context = ProductUi.Label("Đang tải kho nguồn…", 12, true);
    private readonly FlowLayoutPanel _fields = LiveUi.Toolbar();
    private readonly AntdUI.Button _save = ProductUi.Button("btnSave", "Xác nhận", primary: true);
    private readonly ErrorProvider _errors;
    private bool _busy;
    private bool _committed;
    private bool _submitted;
    private string _sourceName = "";
    private readonly AntdUI.Button _refresh = ProductUi.Button("btnRefreshStock", "Tải lại tồn", "arrow-clockwise");

    public StockOperationForm(UiServices services, InventoryDto inventory, bool transfer)
    {
        _services = services; _inventory = inventory; _transfer = transfer;
        _errors = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        WorkflowUi.StyleForm(this);
        Text = "VioWare - " + (transfer ? "Chuyển kho" : "Điều chỉnh tồn");
        ClientSize = new Size(900, 500); MinimumSize = new Size(760, 500);
        StartPosition = FormStartPosition.CenterParent;
        var root = ProductUi.Layout(1, 4);
        AutoScroll = true;
        root.MinimumSize = new Size(0, 500 * DeviceDpi / 96);
        root.Dock = DockStyle.Top;
        root.Height = root.MinimumSize.Height;
        ClientSizeChanged += (_, _) => root.Height = Math.Max(ClientSize.Height, root.MinimumSize.Height);
        root.Padding = new Padding(20);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.Controls.Add(_context, 0, 0);
        if (transfer)
        {
            _quantity.Minimum = 1;
            _fields.Controls.Add(LiveUi.Field("Kho đích *", _destination));
        }
        _fields.Controls.Add(LiveUi.Field(transfer ? "Số lượng chuyển *" : "Chênh lệch (+ tăng, − giảm) *", _quantity));
        _fields.Controls.Add(LiveUi.Field("Lý do *", _reason));
        root.Controls.Add(_fields, 0, 1);
        _fields.WrapContents = true;
        _fields.AutoScroll = false;
        root.Controls.Add(_message, 0, 2);
        var actions = LiveUi.Toolbar();
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Click += (_, _) => Close();
        CancelButton = close;
        _save.Enabled = false;
        _save.Click += async (_, _) => await SaveAsync();
        actions.Controls.AddRange([_save, _refresh, close]);
        _refresh.Click += async (_, _) => { if (!_busy) await RefreshContextAsync(); };
        _destination.SelectedIndexChanged += async (_, _) => { if (!_busy && _transfer) await RefreshContextAsync(); };
        root.Controls.Add(actions, 0, 3);
        Controls.Add(root);
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
        Shown += async (_, _) =>
        {
            _busy = true;
            try
            {
                var warehouses = await services.RunAsync(() => services.Warehouses.GetActiveAsync(services.Session));
                var source = warehouses.FirstOrDefault(w => w.Id == inventory.WarehouseId);
                _sourceName = source?.Name ?? $"Kho #{inventory.WarehouseId}";
                _context.Text = $"{inventory.ProductCode} • {inventory.ProductName}\nKho nguồn: {source?.Name ?? "Không còn hoạt động"} • Tồn hiển thị: {inventory.Quantity:N0} {inventory.Unit}";
                _destination.DisplayMember = nameof(LookupItemDto.Name);
                _destination.ValueMember = nameof(LookupItemDto.Id);
                _destination.DataSource = warehouses.Where(w => w.Id != inventory.WarehouseId).ToList();
                _save.Enabled = source is not null && (!transfer || _destination.Items.Count > 0);
            }
            catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
            finally { _busy = false; }
            await RefreshContextAsync();
        };
    }

    private async Task RefreshContextAsync()
    {
        if (_busy) return;
        _busy = true;
        _save.Enabled = _fields.Enabled = _refresh.Enabled = false;
        _message.Text = _committed ? "Đã lưu thành công • Đang tải lại tồn…" : "Đang tải tồn và kiểm tra quyền…";
        try
        {
            var destinationId = (_destination.SelectedItem as LookupItemDto)?.Id ?? 0;
            var source = await _services.RunAsync(() => _services.Inventory.GetByWarehouseAsync(_services.Session, _inventory.WarehouseId));
            var sourceQuantity = source.FirstOrDefault(row => row.ProductId == _inventory.ProductId)?.Quantity ?? 0;
            var destinationText = "";
            if (_transfer && destinationId != 0)
            {
                var destination = await _services.RunAsync(() => _services.Inventory.GetByWarehouseAsync(_services.Session, destinationId));
                destinationText = $"\nKho đích: {(_destination.SelectedItem as LookupItemDto)?.Name} • Tồn: {destination.FirstOrDefault(row => row.ProductId == _inventory.ProductId)?.Quantity ?? 0:N0} {_inventory.Unit}";
            }
            var allowed = await _services.RunAsync(() => _transfer
                ? _services.Authorization.CanTransferInventoryAsync(_services.Session, _inventory.WarehouseId, destinationId)
                : _services.Authorization.CanAdjustInventoryAsync(_services.Session, _inventory.WarehouseId));
            if (IsDisposed) return;
            _context.Text = $"{_inventory.ProductCode} • {_inventory.ProductName}\nKho nguồn: {_sourceName} • Tồn: {sourceQuantity:N0} {_inventory.Unit}{destinationText}";
            _save.Enabled = !_submitted && !_committed && allowed;
            _message.Text = _committed ? "Đã lưu thành công • Tồn đã được làm mới. Đóng để xem lại danh sách."
                : _submitted ? "Đã tải lại tồn. Thao tác trước chưa được xác nhận thành công; đóng để kiểm tra lịch sử trước khi thử lại."
                : allowed ? "Nhập số lượng và lý do trước khi xác nhận."
                : "Không có quyền thao tác tại kho đã chọn hoặc chưa có kho đích phù hợp.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _message.Text = _committed
                ? "Đã lưu thành công nhưng chưa tải lại được tồn. Nhấn Tải lại tồn; không gửi lại thao tác."
                : LiveUi.LoadError(ex);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) { _fields.Enabled = !_submitted && !_committed; _refresh.Enabled = true; }
        }
    }

    private async Task SaveAsync()
    {
        if (_busy || _submitted || _committed) return;
        _errors.Clear();
        var quantity = decimal.ToInt32(_quantity.Value);
        var destinationId = (_destination.SelectedItem as LookupItemDto)?.Id ?? 0;
        if (quantity == 0) _errors.SetError(_quantity, "Chênh lệch phải khác 0.");
        if (string.IsNullOrWhiteSpace(_reason.Text)) _errors.SetError(_reason, "Nhập lý do.");
        if (_transfer && (destinationId == 0 || destinationId == _inventory.WarehouseId))
            _errors.SetError(_destination, "Chọn kho đích khác kho nguồn.");
        if (quantity == 0 || string.IsNullOrWhiteSpace(_reason.Text) || (_transfer && (destinationId == 0 || destinationId == _inventory.WarehouseId))) return;
        var description = _transfer ? $"Chuyển {quantity:N0} {_inventory.Unit} • {_inventory.ProductCode} sang {(_destination.SelectedItem as LookupItemDto)?.Name}"
            : $"Điều chỉnh {_inventory.ProductCode}: {(quantity > 0 ? "+" : "")}{quantity:N0}";
        if (MessageBox.Show(this, description + "?", "Xác nhận cập nhật tồn", MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
        _submitted = true;
        _busy = true;
        _save.Enabled = _fields.Enabled = _refresh.Enabled = false;
        _message.Text = "Đang xử lý tồn kho…";
        var committed = false;
        try
        {
            await _services.RunAsync(() => _transfer
                ? _services.Inventory.TransferAsync(_services.Session, _inventory.WarehouseId, destinationId, _inventory.ProductId, quantity, _reason.Text.Trim())
                : _services.Inventory.AdjustAsync(_services.Session, _inventory.WarehouseId, _inventory.ProductId, quantity, _reason.Text.Trim()));
            committed = _committed = true;
            // Tách refresh khỏi lệnh ghi. Không gửi lại nghiệp vụ nếu refresh lỗi.
            var source = await _services.RunAsync(() => _services.Inventory.GetByWarehouseAsync(_services.Session, _inventory.WarehouseId));
            var sourceQuantity = source.FirstOrDefault(r => r.ProductId == _inventory.ProductId)?.Quantity ?? 0;
            var destinationText = "";
            if (_transfer)
            {
                var destination = await _services.RunAsync(() => _services.Inventory.GetByWarehouseAsync(_services.Session, destinationId));
                destinationText = $" • Kho đích: {(destination.FirstOrDefault(r => r.ProductId == _inventory.ProductId)?.Quantity ?? 0):N0} {_inventory.Unit}";
            }
            _message.Text = $"Đã lưu thành công • Tồn kho nguồn: {sourceQuantity:N0} {_inventory.Unit}{destinationText}. Đóng để làm mới danh sách.";
        }
        catch (Exception ex)
        {
            _message.Text = committed ? "Đã lưu thành công nhưng chưa tải lại được tồn kho. Đóng và nhấn Làm mới; không gửi lại thao tác."
                : LiveUi.Error(ex);
        }
        finally
        {
            _busy = false;
            // Sau kết quả chưa rõ hoặc đã commit, đóng và kiểm tra lại thay vì cho gửi lặp.
            _save.Enabled = false;
            _fields.Enabled = false;
            _refresh.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _errors.Dispose();
        base.Dispose(disposing);
    }
}
