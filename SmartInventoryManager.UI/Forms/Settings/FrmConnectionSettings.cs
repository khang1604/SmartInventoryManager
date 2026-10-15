using System.Diagnostics;
using System.Security.Cryptography;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI.Forms.Settings;

internal sealed class FrmConnectionSettings : Form
{
    private readonly TextBox _server = new() { AccessibleName = "Địa chỉ máy chủ", PlaceholderText = "IP VPN hoặc tên máy chủ, không kèm cổng" };
    private readonly NumericUpDown _port = new() { Minimum = 1, Maximum = 65535, Value = 14330, AccessibleName = "Cổng kết nối" };
    private readonly TextBox _database = new() { AccessibleName = "Tên dữ liệu", PlaceholderText = "Tên database do quản trị viên cung cấp" };
    private readonly TextBox _username = new() { AccessibleName = "Tên đăng nhập kết nối", PlaceholderText = "SQL login, khác tài khoản đăng nhập VioWare" };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, AccessibleName = "Mật khẩu kết nối" };
    private readonly CheckBox _windows = new() { Text = "Dùng tài khoản Windows hiện tại", AutoSize = true };
    private readonly CheckBox _trust = new() { Text = "Chấp nhận chứng chỉ tự ký (chỉ dùng khi được quản trị viên hướng dẫn)", AutoSize = true };
    private readonly Label _status = ProductUi.Label("Chưa được cấu hình kết nối máy chủ. Bạn có thể đóng và quay lại sau khi quản trị viên duyệt quyền truy cập.");
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
            "Cài phần mềm trước • Quản trị viên cấp quyền kết nối và tài khoản VioWare sau", new Size(820, 760));
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
        var guidance = ProductUi.Label("Thông tin kết nối do quản trị viên cung cấp, khác tài khoản ADMIN đăng nhập VioWare.\nMật khẩu kết nối được bảo vệ theo tài khoản Windows trên máy này; không có bước tạo ADMIN ở đây.");
        guidance.Dock = DockStyle.None; guidance.Height = 76; guidance.AutoEllipsis = false;
        body.Controls.Add(guidance);
        var vpn = new LinkLabel { Text = "Cài / đăng nhập Tailscale và chờ quản trị viên cấp quyền", Height = 42, AutoSize = false };
        vpn.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://tailscale.com/download/windows") { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            { _status.Text = "Không mở được trình duyệt. Truy cập tailscale.com/download/windows để cài VPN."; }
        };
        body.Controls.Add(vpn);
        _status.Dock = DockStyle.None; _status.Height = 94; _status.AutoEllipsis = false;
        _status.AccessibleName = "Trạng thái kết nối";
        body.Controls.Add(_status);
        var close = ProductUi.Button("btnConfigureLater", "Để sau / Đóng");
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
                _status.Text = "Đã có cấu hình trên máy. Kiểm tra lại kết nối trước khi lưu thay đổi.";
            }
            catch (ArgumentException) { _status.Text = "Cấu hình cũ không hợp lệ. Nhập lại thông tin do quản trị viên cung cấp."; }
        }
        if (unreadable) _status.Text = "Không đọc được cấu hình của tài khoản Windows này. Nhập lại kết nối; không cần cài lại phần mềm.";
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMART_INVENTORY_CONNECTION_STRING")))
            _status.Text += "\nMáy đang có cấu hình từ biến môi trường; cấu hình đó có ưu tiên cao hơn file đã lưu.";
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
        catch (ArgumentException ex) { _status.Text = ex.Message; return; }
        _test.Enabled = _save.Enabled = false;
        _status.Text = "Đang kiểm tra kết nối… Bạn vẫn có thể đóng và thử lại sau.";
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
            _status.Text = "Kết nối thành công. Bấm Lưu và tiếp tục, rồi đăng nhập bằng tài khoản VioWare do quản trị viên cấp.";
        }
        catch (Exception)
        {
            _testedConnection = null;
            if (!IsDisposed) _status.Text = "Chưa kết nối được. Kiểm tra VPN, quyền truy cập đã được duyệt, địa chỉ/cổng, thông tin kết nối và chứng chỉ; có thể đóng rồi thử lại sau.";
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
        { _status.Text = "Không lưu được cấu hình trên máy. Kiểm tra quyền ghi của tài khoản Windows rồi thử lại."; }
    }
}
