using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmartInventoryManager.UI.Components;

// Hide scrollbar chrome without disabling the existing scroll ranges and wheel handling.
internal static class ScrollbarAppearance
{
    private static readonly ConditionalWeakTable<Control, Subscription> Subscriptions = new();

    // Áp dụng cấu hình hiển thị cho thành phần giao diện.
    public static void Apply(Control root)
    {
        Subscriptions.GetValue(root, control => new Subscription(control));
        foreach (Control child in root.Controls) Apply(child);
    }

    private sealed class Subscription
    {
        private readonly ScrollWindow? _window;
        // Khởi tạo bộ theo dõi thông điệp Windows để điều chỉnh thanh cuộn.
        public Subscription(Control control)
        {
            control.ControlAdded += (_, e) => { if (e.Control is not null) Apply(e.Control); };
            if (control is not ScrollableControl) return;
            _window = new ScrollWindow();
            control.HandleCreated += (_, _) => { _window.AssignHandle(control.Handle); _window.HideBars(); };
            control.HandleDestroyed += (_, _) => _window.ReleaseHandle();
            control.Layout += (_, _) => _window.HideBars();
            if (control.IsHandleCreated) { _window.AssignHandle(control.Handle); _window.HideBars(); }
        }
    }

    private sealed class ScrollWindow : NativeWindow
    {
        private bool _hiding;
        // Ẩn thanh cuộn gốc theo cấu hình hiển thị của điều khiển.
        public void HideBars()
        {
            if (Handle == IntPtr.Zero || _hiding || (GetWindowLong(Handle, -16) & 0x00300000) == 0) return;
            _hiding = true;
            try { ShowScrollBar(Handle, 3, false); }
            finally { _hiding = false; }
        }
        // Xử lý thông điệp Windows để đồng bộ hành vi thanh cuộn.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg is 0x0005 or 0x0085 or 0x0114 or 0x0115 or 0x020A or 0x020E) HideBars();
        }
        // Đọc giá trị kiểu cửa sổ qua hàm Windows được nhập từ thư viện hệ thống.
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr handle, int index);
        // Gọi hàm Windows để bật hoặc ẩn thanh cuộn của cửa sổ.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(IntPtr handle, int bar, [MarshalAs(UnmanagedType.Bool)] bool show);
    }
}

internal sealed class ScrollableGrid : DataGridView
{
    // Khởi tạo bảng dữ liệu hỗ trợ cuộn theo thiết lập giao diện.
    public ScrollableGrid() => ScrollBars = ScrollBars.None;

    // Xử lý thông điệp Windows để đồng bộ hành vi thanh cuộn.
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x020E)
        {
            var delta = (short)((m.WParam.ToInt64() >> 16) & 0xffff);
            HorizontalScrollingOffset = Math.Max(0, HorizontalScrollingOffset + Math.Sign(delta) * 60);
            return;
        }
        base.WndProc(ref m);
    }

    // Xử lý lăn chuột để cuộn nội dung bảng dữ liệu.
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e.Delta == 0 || RowCount == 0) return;
        if ((ModifierKeys & Keys.Shift) != 0)
        {
            HorizontalScrollingOffset = Math.Max(0, HorizontalScrollingOffset - Math.Sign(e.Delta) * 60);
            return;
        }
        var current = FirstDisplayedScrollingRowIndex;
        if (current < 0) return;
        var steps = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        if (SystemInformation.MouseWheelScrollLines < 0) steps = Math.Max(1, DisplayedRowCount(false));
        var direction = e.Delta > 0 ? -1 : 1;
        var target = current;
        for (var count = 0; count < steps; count++)
        {
            var next = target + direction;
            while (next >= 0 && next < RowCount && (!Rows[next].Visible || Rows[next].Frozen)) next += direction;
            if (next < 0 || next >= RowCount) break;
            target = next;
        }
        if (target != current) FirstDisplayedScrollingRowIndex = target;
    }
}
