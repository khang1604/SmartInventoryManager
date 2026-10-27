using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class UiDetails
{
    // Mở cửa sổ hiển thị các thông tin chi tiết được cung cấp.
    public static void Show(Control owner, string title, IEnumerable<(string Caption, string Value)> values, string? note = null)
    {
        using var form = new Form { Text = $"VioWare - {title}", ClientSize = new Size(650, 580),
            MinimumSize = new Size(540, 420), StartPosition = FormStartPosition.CenterParent,
            AutoScaleMode = AutoScaleMode.Font, ShowInTaskbar = false };
        WorkflowUi.StyleForm(form);
        var root = ProductUi.Layout(1, 3);
        root.Padding = new Padding(24);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.Controls.Add(ProductUi.Label(title, 18, true), 0, 0);
        var rows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        foreach (var (caption, value) in values)
        {
            int row = rows.RowCount++;
            rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rows.Controls.Add(new Label { Text = caption, AutoSize = true, MaximumSize = new Size(180, 0), Padding = new Padding(0, 8, 0, 8) }, 0, row);
            rows.Controls.Add(new TextBox { Text = value, ReadOnly = true, BorderStyle = BorderStyle.None,
                Multiline = true, WordWrap = true, Dock = DockStyle.Fill, Height = 48, Margin = new Padding(8),
                // [COLOR CONFIG]: nền trắng và chữ tối cho thông tin chỉ đọc, có thể sao chép.
                BackColor = AppColors.Surface, ForeColor = AppColors.Text }, 1, row);
        }
        if (note is not null)
        {
            int row = rows.RowCount++;
            var label = new Label { Text = note, AutoSize = true, MaximumSize = new Size(550, 0), Padding = new Padding(0, 12, 0, 12) };
            rows.Controls.Add(label, 0, row); rows.SetColumnSpan(label, 2);
        }
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(rows); root.Controls.Add(scroll, 0, 1);
        var close = ProductUi.Button("btnCloseDetails", "Đóng");
        close.Dock = DockStyle.Right; close.Width = 110; close.DialogResult = DialogResult.Cancel;
        form.CancelButton = close; root.Controls.Add(close, 0, 2); form.Controls.Add(root);
        form.ShowDialog(owner.FindForm());
    }

    // Thêm cột thao tác xem chi tiết vào bảng dữ liệu.
    public static void AddColumn(DataGridView grid, bool insetButton = false)
    {
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "ViewDetails", HeaderText = "Chi tiết",
            Text = "Xem chi tiết", UseColumnTextForButtonValue = true, Width = 125, MinimumWidth = 125,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable });
        DecorateDetailsColumn(grid, insetButton);
        grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "ViewDetails") return;
            var row = grid.Rows[e.RowIndex];
            var values = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c.Name != "ViewDetails")
                .Select(c => (c.HeaderText, Convert.ToString(row.Cells[c.Index].FormattedValue, LiveUi.Culture) ?? "—"));
            Show(grid, "Thông tin chi tiết", values);
        };
        grid.CellFormatting += (_, e) =>
        {
            if (e.FormattingApplied) return;
            if (e.Value is bool active) { e.Value = active ? "Đang hoạt động" : "Ngừng hoạt động"; e.FormattingApplied = true; }
            else if (grid.Columns[e.ColumnIndex].HeaderText is "Vai trò" && e.Value is string role)
            {
                e.Value = LiveUi.RoleText(role);
                e.FormattingApplied = true;
            }
        };
    }

    // Định dạng cột xem chi tiết và gắn hành vi mở thông tin.
    public static void DecorateDetailsColumn(DataGridView grid, bool insetButton = false)
    {
        var eye = AntdUI.SvgExtend.SvgToBmp(MainIcons.Get("eye"), 18, 18, AppColors.Text);
        // [COLOR CONFIG]: icon của hàng chọn dùng màu chữ chọn để giữ tương phản.
        var selectedEye = AntdUI.SvgExtend.SvgToBmp(MainIcons.Get("eye"), 18, 18, SystemInformation.HighContrast ? SystemColors.HighlightText : AppColors.SelectedText);
        grid.Disposed += (_, _) => { eye?.Dispose(); selectedEye?.Dispose(); };
        grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "ViewDetails" || eye is null) return;
            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            int size = (int)(18 * grid.DeviceDpi / 96F);
            var bounds = e.CellBounds;
            if (insetButton)
            {
                int gap = Math.Max(4, (int)(5 * grid.DeviceDpi / 96F));
                bounds.Inflate(-gap, -gap);
                using var outline = new System.Drawing.Drawing2D.GraphicsPath();
                outline.AddRoundedRectangle(bounds, new Size(gap * 2, gap * 2));
                // [COLOR CONFIG]: nút giữ cùng nền hàng được chọn; viền và chữ theo style cell để hỗ trợ High Contrast.
                using var background = new SolidBrush(selected ? e.CellStyle!.SelectionBackColor : e.CellStyle!.BackColor);
                using var border = new Pen(SystemInformation.HighContrast ? (selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor) : AppColors.Border);
                e.Graphics!.FillPath(background, outline);
                e.Graphics.DrawPath(border, outline);
            }
            e.Graphics!.DrawImage(selected && selectedEye is not null ? selectedEye : eye, new Rectangle(bounds.Left + 6, bounds.Top + (bounds.Height - size) / 2, size, size));
            TextRenderer.DrawText(e.Graphics, "Chi tiết", grid.Font,
                new Rectangle(bounds.Left + size + 12, bounds.Top, Math.Max(0, bounds.Width - size - 16), bounds.Height),
                selected ? e.CellStyle!.SelectionForeColor : e.CellStyle!.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.Handled = true;
        };
    }
}
