namespace SmartInventoryManager.UI.Components;

internal static class PasswordVisibility
{
    public static Control Wrap(Control input)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var eye = new Button { Name = "btnShow" + input.Name, Text = "◉", Dock = DockStyle.Fill,
            Margin = Padding.Empty, AccessibleName = "Hiện " + input.AccessibleName, TabStop = true };
        var visible = false;
        void Mask()
        {
            visible = false;
            if (input is TextBox text) text.UseSystemPasswordChar = true;
            else if (input is AntdUI.Input ant) ant.UseSystemPasswordChar = true;
            eye.AccessibleName = "Hiện " + input.AccessibleName;
            eye.Text = "◉";
        }
        Mask();
        eye.Click += (_, _) =>
        {
            visible = !visible;
            if (input is TextBox text) text.UseSystemPasswordChar = !visible;
            else if (input is AntdUI.Input ant) ant.UseSystemPasswordChar = !visible;
            eye.AccessibleName = (visible ? "Ẩn " : "Hiện ") + input.AccessibleName;
            eye.Text = visible ? "◎" : "◉";
        };
        input.EnabledChanged += (_, _) => { eye.Enabled = input.Enabled; if (!input.Enabled) Mask(); };
        input.VisibleChanged += (_, _) => { if (!input.Visible) Mask(); };
        eye.Enabled = input.Enabled;
        panel.Controls.Add(input, 0, 0); panel.Controls.Add(eye, 1, 0);
        return panel;
    }
}
