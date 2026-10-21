using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class PreviewFormUi
{
    public static TableLayoutPanel Root(Form form, string title, Size size)
    {
        WorkflowUi.StyleForm(form);
        form.MinimizeBox = false;
        form.Text = $"VioWare - {title}";
        form.ClientSize = size;
        form.MinimumSize = new Size(560, 540);
        form.StartPosition = FormStartPosition.CenterParent;
        form.ShowInTaskbar = false;
        var root = ProductUi.Layout(1, 3);
        form.AutoScroll = true;
        root.MinimumSize = new Size(0, size.Height);
        root.Dock = DockStyle.Top;
        root.Height = size.Height;
        form.ClientSizeChanged += (_, _) => root.Height = Math.Max(form.ClientSize.Height, root.MinimumSize.Height);
        root.Padding = new Padding(20, 14, 20, 12);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var heading = ProductUi.Layout(1, 2);
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(ProductUi.Label(title, 18, true), 0, 0);
        var note = ProductUi.Label("Các trường có dấu * là bắt buộc • Biểu mẫu xem trước chưa lưu dữ liệu", 9);
        note.Name = "formNote";
        // [COLOR CONFIG]: hướng dẫn phụ.
        note.ForeColor = AppColors.Muted;
        heading.Controls.Add(note, 0, 1);
        root.Controls.Add(heading, 0, 0);
        var footer = ProductUi.Layout(2, 1);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        footer.Padding = new Padding(0, 14, 0, 0);
        var status = ProductUi.Label("Chưa kết nối dữ liệu", 9);
        status.Name = "formStatus";
        // [COLOR CONFIG]: ghi chú phụ cuối biểu mẫu.
        status.ForeColor = AppColors.Muted;
        footer.Controls.Add(status, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Width = 100;
        close.DialogResult = DialogResult.Cancel;
        close.TabIndex = 90;
        var save = ProductUi.Button("btnSave", "Lưu", primary: true);
        save.Enabled = false;
        save.TabIndex = 91;
        save.AccessibleDescription = "Chưa thể lưu thông tin.";
        actions.Controls.Add(close);
        actions.Controls.Add(save);
        form.CancelButton = close;
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);
        form.Controls.Add(root);
        return root;
    }

    public static TableLayoutPanel Fields(int rows)
    {
        var fields = ProductUi.Layout(2, rows * 2);
        fields.Padding = new Padding(16);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        // [COLOR CONFIG]: nền trắng cho nhóm trường.
        fields.BackColor = AppColors.Surface;
        for (int i = 0; i < rows; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        }
        return fields;
    }

    public static AntdUI.Input Field(TableLayoutPanel fields, int col, int row, string name, string caption,
        int maxLength = 0, string? placeholder = null, bool unavailable = false)
    {
        fields.Controls.Add(ProductUi.Label(caption, 10, true), col, row * 2);
        var input = ProductUi.Input(name, placeholder ?? "");
        if (maxLength > 0) input.MaxLength = maxLength;
        input.ReadOnly = unavailable;
        input.TabIndex = row * 2 + col;
        input.AccessibleName = caption;
        if (unavailable)
        {
            input.SuffixSvg = MainIcons.Get("chevron-down");
            input.AccessibleDescription = "Danh sách lựa chọn chưa sẵn sàng";
        }
        fields.Controls.Add(input, col, row * 2 + 1);
        return input;
    }

    public static TextBox TextBoxField(TableLayoutPanel fields, int col, int row, string name, string caption,
        int maxLength = 0, bool password = false)
    {
        fields.Controls.Add(ProductUi.Label(caption, 10, true), col, row * 2);
        var input = new TextBox
        {
            Name = name,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            MaxLength = maxLength,
            TabIndex = row * 2 + col,
            AccessibleName = caption,
            UseSystemPasswordChar = password,
            BorderStyle = BorderStyle.FixedSingle
        };
        // [COLOR CONFIG]: nền và chữ của ô mật khẩu native WinForms.
        input.BackColor = AppColors.Surface;
        input.ForeColor = AppColors.Text;
        fields.Controls.Add(input, col, row * 2 + 1);
        return input;
    }
}
