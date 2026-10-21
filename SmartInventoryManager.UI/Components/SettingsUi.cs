using System.Drawing.Drawing2D;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal sealed class RoundedSurface : Panel
{
    public RoundedSurface()
    {
        DoubleBuffered = true;
        Padding = new Padding(16);
        // [COLOR CONFIG]: nền trắng và đường viền nhạt cho các nhóm cài đặt.
        BackColor = AppColors.Surface; ForeColor = AppColors.Text;
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // [COLOR CONFIG]: góc ngoài theo nền cha, bề mặt trắng nằm trong đường bo góc.
        e.Graphics.Clear(Parent?.BackColor ?? AppColors.Workspace);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), new Size(12, 12));
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), new Size(12, 12));
        // [COLOR CONFIG]: đường viền nhóm cài đặt theo theme.
        using var pen = new Pen(AppColors.Border);
        e.Graphics.DrawPath(pen, path);
    }
}

internal static class SettingsUi
{
    public static (TableLayoutPanel Root, FlowLayoutPanel Body, FlowLayoutPanel Footer) Root(Form form, string title, string description, Size size)
    {
        WorkflowUi.StyleForm(form);
        form.MinimizeBox = false;
        form.Text = $"VioWare - {title}";
        form.AutoScaleMode = AutoScaleMode.Font;
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1366, 768);
        form.ClientSize = new Size(Math.Min(size.Width, workArea.Width - 80), Math.Min(size.Height, workArea.Height - 80));
        form.MinimumSize = new Size(620, 540);
        form.StartPosition = FormStartPosition.CenterParent; form.ShowInTaskbar = false;
        var root = ProductUi.Layout(1, 3); root.Padding = new Padding(20);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        var heading = ProductUi.Layout(1, 2);
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(ProductUi.Label(title, 18, true), 0, 0);
        heading.Controls.Add(ProductUi.Label(description, 10), 0, 1);
        var body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        body.SizeChanged += (_, _) =>
        {
            foreach (Control child in body.Controls) child.Width = Math.Max(300, body.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        };
        body.ControlAdded += (_, e) => { if (e.Control is not null) e.Control.Width = Math.Max(300, body.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4); };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 12, 0, 0), Margin = Padding.Empty };
        root.Controls.Add(heading, 0, 0); root.Controls.Add(body, 0, 1); root.Controls.Add(footer, 0, 2);
        form.Controls.Add(root); return (root, body, footer);
    }
    public static RoundedSurface Group(string title, Color tint, int height, params Control[] contents)
    {
        var group = new RoundedSurface { Height = height, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(14) };
        var rows = ProductUi.Layout(1, contents.Length + 1);
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var heading = ProductUi.Label(title, 12, true);
        // [COLOR CONFIG]: tiêu đề nhóm pastel, luôn có chữ mô tả rõ chức năng.
        heading.BackColor = tint; heading.Padding = new Padding(8, 0, 0, 0);
        rows.Controls.Add(heading, 0, 0);
        for (int i = 0; i < contents.Length; i++)
        {
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / contents.Length));
            rows.Controls.Add(contents[i], 0, i + 1);
        }
        group.Controls.Add(rows); return group;
    }
    public static Control Pair(string caption, Control value)
    {
        var row = ProductUi.Layout(2, 1);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        row.Controls.Add(ProductUi.Label(caption), 0, 0);
        value.Dock = DockStyle.Top; value.Margin = new Padding(8, 10, 0, 0);
        row.Controls.Add(value, 1, 0); return row;
    }
    public static Control Fields(params (string Caption, string Value, bool ReadOnly)[] fields)
    {
        var grid = ProductUi.Layout(2, (fields.Length + 1) / 2);
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int row = 0; row < grid.RowCount; row++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / grid.RowCount));
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var cell = ProductUi.Layout(1, 2); cell.Margin = new Padding(4, 4, 8, 4);
            cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cell.Controls.Add(ProductUi.Label(field.Caption), 0, 0);
            var input = ProductUi.Input("field" + i); input.Text = field.Value; input.ReadOnly = field.ReadOnly;
            // [COLOR CONFIG]: nền nhạt phân biệt thông tin chỉ xem với ô được nhập.
            input.BackColor = field.ReadOnly ? AppColors.Workspace : AppColors.Surface;
            input.AccessibleName = field.Caption; input.AccessibleDescription = field.ReadOnly ? "Thông tin chỉ xem" : "Thông tin cá nhân có thể chỉnh sửa khi chức năng lưu được hỗ trợ";
            input.Margin = Padding.Empty; cell.Controls.Add(input, 0, 1); grid.Controls.Add(cell, i % 2, i / 2);
        }
        return grid;
    }
    public static AntdUI.Button Avatar(int size = 52)
    {
        var avatar = ProductUi.Button("avatar", "", "person");
        avatar.Size = new Size(size, size); avatar.Radius = size / 2; avatar.BorderWidth = 0;
        avatar.IconSize = new Size(size / 2, size / 2); avatar.TabStop = false; avatar.Cursor = Cursors.Default;
        // [COLOR CONFIG]: avatar tròn xanh trời, icon người tối.
        avatar.DefaultBack = avatar.BackColor = AppColors.Sky;
        avatar.BackHover = avatar.BackActive = AppColors.Sky;
        return avatar;
    }
}
