using System.Security.Cryptography;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI.Forms.Settings;

internal sealed class FrmConnectionSettings : Form
{
    private readonly TextBox _server = new() { AccessibleName = "Địa chỉ máy chủ", PlaceholderText = "Ví dụ: 100.x.x.x (không nhập cổng ở đây)" };
    private readonly NumericUpDown _port = new() { Minimum = 1, Maximum = 65535, Value = 14330, AccessibleName = "Cổng kết nối" };
    private readonly TextBox _database = new() { AccessibleName = "Tên dữ liệu", PlaceholderText = "Nhập tên dữ liệu được cung cấp" };
    private readonly TextBox _username = new() { AccessibleName = "Tên đăng nhập kết nối", PlaceholderText = "Nhập tên kết nối được cung cấp" };
    private readonly TextBox _password = new() { PlaceholderText = "Nhập mật khẩu kết nối được cung cấp", UseSystemPasswordChar = true, AccessibleName = "Mật khẩu kết nối" };
    private readonly CheckBox _windows = new() { Text = "Dùng tài khoản Windows hiện tại", AutoSize = true };
    private readonly CheckBox _trust = new() { Text = "Bỏ kiểm tra chứng chỉ (chỉ bật khi được hướng dẫn)", AutoSize = true };
    private readonly Label _status = ProductUi.Label("");
    private readonly AntdUI.Button _test = ProductUi.Button("btnTestConnection", "Kiểm tra kết nối");
    private readonly AntdUI.Button _save = ProductUi.Button("btnSaveConnection", "Lưu và tiếp tục", primary: true);
    private readonly ConnectionSettingsStore _store;
    private readonly Func<string, CancellationToken, Task> _probe;
    private CancellationTokenSource? _request;
    private string? _testedConnection;
    public string? ConnectionString { get; private set; }

    public FrmConnectionSettings(ConnectionSettingsStore store, Func<string, CancellationToken, Task> probe)
    {
        _store = store; _probe = probe;
        var (_, body, footer) = SettingsUi.Root(this, "Kết nối máy chủ",
            "Nhập thông tin do quản trị viên cung cấp.", new Size(820, 590));
        ShowInTaskbar = true;
        var fields = ProductUi.Layout(2, 7);
        fields.Dock = DockStyle.None; fields.Height = 315;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var inputs = new (string Caption, Control Input)[]
        {
            ("Địa chỉ máy chủ", _server), ("Cổng", _port), ("Tên dữ liệu", _database),
            ("Xác thực", _windows), ("Tên đăng nhập kết nối", _username),
            ("Mật khẩu kết nối", _password), ("Chứng chỉ", _trust)
        };
        for (var i = 0; i < inputs.Length; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            fields.Controls.Add(ProductUi.Label(inputs[i].Caption), 0, i);
            inputs[i].Input.Dock = DockStyle.Top;
            inputs[i].Input.Margin = new Padding(0, 9, 0, 0);
            inputs[i].Input.TabIndex = i;
            fields.Controls.Add(inputs[i].Input, 1, i);
        }
        _trust.AutoSize = false; _trust.Height = 44; _trust.Margin = Padding.Empty;
        body.Controls.Add(fields);
        _status.Dock = DockStyle.None; _status.Height = 64; _status.AutoEllipsis = false;
        _status.AccessibleName = "Trạng thái kết nối";
        body.Controls.Add(_status);
        var close = ProductUi.Button("btnConfigureLater", "Đóng");
        close.DialogResult = DialogResult.Cancel;
        footer.Controls.Add(close); footer.Controls.Add(_save); footer.Controls.Add(_test);
        CancelButton = close;
        _save.Enabled = false;
        _test.Click += async (_, _) => await TestAsync();
        _save.Click += (_, _) => Save();
        foreach (var input in new[] { _server, _database, _username, _password }) input.TextChanged += (_, _) => InvalidateTest();
        _port.ValueChanged += (_, _) => InvalidateTest();
        _trust.CheckedChanged += (_, _) => InvalidateTest();
        _windows.CheckedChanged += (_, _) => { _username.Enabled = _password.Enabled = !_windows.Checked; InvalidateTest(); };
        FormClosing += (_, _) => _request?.Cancel();
        Shown += (_, _) => WorkflowUi.FitToWorkingArea(this);
        var saved = store.Load(out var unreadable);
        if (saved is not null)
        {
            try
            {
                var profile = ConnectionProfile.FromConnectionString(saved);
                _server.Text = profile.Server; _port.Value = profile.Port; _database.Text = profile.Database;
                _username.Text = profile.Username; _password.Text = profile.Password;
                _windows.Checked = profile.WindowsAuthentication; _trust.Checked = profile.TrustServerCertificate;
                _status.Text = "Kiểm tra kết nối trước khi lưu thay đổi.";
            }
            catch (ArgumentException) { _status.Text = "Nhập lại thông tin kết nối được cung cấp."; }
        }
        if (unreadable) _status.Text = "Không đọc được cấu hình đã lưu. Vui lòng nhập lại.";
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMART_INVENTORY_CONNECTION_STRING")))
            _status.Text += "\nMáy đang ưu tiên cấu hình do quản trị viên thiết lập. Liên hệ quản trị viên để đổi cấu hình đó.";
        ScrollbarAppearance.Apply(this);
    }

    private ConnectionProfile Profile() => new(_server.Text, (int)_port.Value, _database.Text,
        _username.Text, _password.Text, _windows.Checked, _trust.Checked);

    private void InvalidateTest()
    {
        if (_testedConnection is not null) _status.Text = "Thông tin đã thay đổi. Kiểm tra lại kết nối trước khi lưu.";
        _testedConnection = null; _save.Enabled = false;
    }

    private async Task TestAsync()
    {
        if (_request is not null) return;
        string connection;
        try { connection = Profile().BuildConnectionString(); }
        catch (ArgumentException ex) { _status.Text = LiveUi.ValidationMessage(ex); return; }
        _test.Enabled = _save.Enabled = false;
        _status.Text = "Đang kiểm tra kết nối…";
        using var request = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _request = request;
        try
        {
            await _probe(connection, request.Token);
            if (IsDisposed || request.IsCancellationRequested) return;
            if (connection != Profile().BuildConnectionString())
            { _status.Text = "Thông tin đã thay đổi. Hãy kiểm tra lại cấu hình mới."; return; }
            _testedConnection = connection;
            _save.Enabled = true;
            _status.Text = "Kết nối thành công. Bấm Lưu và tiếp tục.";
        }
        catch (Exception)
        {
            _testedConnection = null;
            if (!IsDisposed) _status.Text = "Chưa kết nối được. Kiểm tra địa chỉ, cổng, mật khẩu và quyền kết nối.";
        }
        finally { _request = null; if (!IsDisposed) _test.Enabled = true; }
    }

    private void Save()
    {
        try
        {
            var connection = Profile().BuildConnectionString();
            if (_testedConnection != connection) { InvalidateTest(); return; }
            _store.Save(connection);
            ConnectionString = connection; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException)
        { _status.Text = "Không lưu được cấu hình. Vui lòng thử lại."; }
    }
}
