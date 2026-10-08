using System.Drawing.Drawing2D;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal sealed class MainNavigationButton : Button
{
    private Bitmap _icon;
    private bool _hover;
    [System.ComponentModel.DefaultValue(false)]
    public bool UseAvatar { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool ShowChevron { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool ChevronExpanded { get; set; }
    [System.ComponentModel.DefaultValue(6)]
    public int CornerRadius { get; set; } = 6;
    [System.ComponentModel.DefaultValue(false)]
    public bool Collapsed { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool Selected { get; set; }

    public MainNavigationButton(string title, string icon)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Text = title;
        AccessibleName = title;
        UseMnemonic = false;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0, 2, 0, 2);
        _icon = AntdUI.SvgExtend.SvgToBmp(MainIcons.Get(icon), 28, 28, AppColors.Text)
            ?? throw new InvalidOperationException($"Cannot render icon: {icon}");
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    public void SetIcon(string name)
    {
        var replacement = AntdUI.SvgExtend.SvgToBmp(MainIcons.Get(name), 28, 28, AppColors.Text)
            ?? throw new InvalidOperationException($"Cannot render icon: {name}");
        _icon.Dispose();
        _icon = replacement;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // [COLOR CONFIG]: xóa toàn bộ nền #F7F7FF để viền/focus cũ không lưu lại sau repaint.
        e.Graphics.Clear(AppColors.Workspace);
        // [COLOR CONFIG]: nền lavender khi chọn/hover, nền workspace khi không chọn.
        var background = Selected || _hover ? AppColors.Lavender : AppColors.Workspace;
        // [COLOR CONFIG]: chữ tương phản theo trạng thái chọn và High Contrast.
        var foreground = Selected || _hover ? AppColors.SelectedText : AppColors.Text;
        using var brush = new SolidBrush(background);
        using var outline = new GraphicsPath();
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        outline.AddRoundedRectangle(bounds, new Size(CornerRadius, CornerRadius));
        e.Graphics.FillPath(brush, outline);
        var iconSize = (int)((Text.Length == 0 ? 18 : 28) * DeviceDpi / 96F);
        var left = Collapsed ? (Width - iconSize) / 2 : (int)(12 * DeviceDpi / 96F);
        var iconRect = new Rectangle(left, (Height - iconSize) / 2, iconSize, iconSize);
        if (UseAvatar)
        {
            // [COLOR CONFIG]: avatar tròn xanh trời, person.svg là ảnh đại diện mặc định.
            using var avatarBrush = new SolidBrush(AppColors.Sky);
            e.Graphics.FillEllipse(avatarBrush, Rectangle.Inflate(iconRect, 4, 4));
        }
        e.Graphics.DrawImage(_icon, iconRect);
        if (!Collapsed)
            TextRenderer.DrawText(e.Graphics, Text, Font,
                new Rectangle(iconRect.Right + 10, 0, Math.Max(0, Width - iconRect.Right - (ShowChevron ? 34 : 18)), Height),
                foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (ShowChevron && !Collapsed)
        {
            // [COLOR CONFIG]: mũi tên dùng cùng màu chữ, báo trạng thái thanh tài khoản.
            using var pen = new Pen(foreground, 1.5F);
            int x = Width - 18, y = Height / 2;
            int offset = ChevronExpanded ? 3 : -3;
            e.Graphics.DrawLines(pen, [new Point(x - 4, y - offset), new Point(x, y + offset), new Point(x + 4, y - offset)]);
        }
        if (Focused && ShowFocusCues)
        {
            // [COLOR CONFIG]: viền nét liền màu chữ, chỉ hiện khi dùng bàn phím để giữ focus rõ ràng.
            using var focusPen = new Pen(foreground, 1);
            using var focusOutline = new GraphicsPath();
            focusOutline.AddRoundedRectangle(Rectangle.Inflate(bounds, -3, -3), new Size(5, 5));
            e.Graphics.DrawPath(focusPen, focusOutline);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _icon.Dispose();
        base.Dispose(disposing);
    }
}
