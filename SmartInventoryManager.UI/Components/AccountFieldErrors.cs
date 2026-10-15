using System.Diagnostics;

namespace SmartInventoryManager.UI.Components;

internal sealed class AccountFieldErrors
{
    private readonly Dictionary<Control, Label> _labels = new();
    private Control? _first;

    public Control Wrap(Control input)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 60, ColumnCount = 1, RowCount = 2, Margin = input.Margin };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        var label = new Label { Dock = DockStyle.Fill, Margin = Padding.Empty, Font = new Font("Segoe UI", 9),
            AccessibleName = "Lỗi " + input.AccessibleName };
        // [COLOR CONFIG]: chữ đỏ tối để lỗi dễ đọc trên nền trắng, luôn kèm nội dung.
        label.ForeColor = Color.FromArgb(180, 32, 32);
        input.Margin = Padding.Empty; input.Dock = DockStyle.Fill;
        panel.Controls.Add(input, 0, 0); panel.Controls.Add(label, 0, 1);
        _labels.Add(input, label);
        input.TextChanged += (_, _) => { label.Text = ""; };
        return panel;
    }

    public void Clear()
    {
        _first = null;
        foreach (var label in _labels.Values) label.Text = "";
    }

    public void Set(Control input, string message)
    {
        _labels[input].Text = message;
        _first ??= input;
    }

    public bool FocusInvalid()
    {
        if (_first is null) return false;
        _first.Focus();
        return true;
    }

    public static string SafeMessage(Exception exception)
    {
        Debug.WriteLine(exception);
        return exception.Message switch
        {
            "Tên đăng nhập đã tồn tại." or "Email đã được sử dụng." or "Mật khẩu hiện tại không đúng."
                or "Tên đăng nhập hoặc email trùng với thông tin đăng nhập của tài khoản khác."
                or "Hãy đổi hoặc gỡ người quản lý tại các kho trước khi đổi vai trò hoặc vô hiệu hóa tài khoản này."
                or "Hệ thống phải còn ít nhất một ADMIN đang hoạt động."
                or "Vai trò không tồn tại trong hệ thống." => exception.Message,
            _ => "Không lưu được. Vui lòng kiểm tra kết quả trước khi thử lại."
        };
    }
}
