using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal sealed class DateRangeControl : UserControl
{
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Dock = DockStyle.Top };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Dock = DockStyle.Top };
    private readonly Label _error = new() { Dock = DockStyle.Fill, AutoEllipsis = true };
    public bool IsValid => _from.Value.Date <= _to.Value.Date;
    public DateRangeControl()
    {
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(2, 3);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        root.Controls.Add(ProductUi.Label("Từ ngày", 10, true), 0, 0); root.Controls.Add(ProductUi.Label("Đến ngày", 10, true), 1, 0);
        _from.Margin = new Padding(0, 7, 8, 0); _to.Margin = new Padding(0, 7, 8, 0);
        _from.Font = _to.Font = new Font("Segoe UI", 10);
        _from.Value = DateTime.Today.AddDays(-30); _to.Value = DateTime.Today;
        _from.AccessibleName = "Từ ngày"; _to.AccessibleName = "Đến ngày";
        root.Controls.Add(_from, 0, 1); root.Controls.Add(_to, 1, 1);
        // [COLOR CONFIG]: thông báo ngày không hợp lệ bằng chữ đỏ đậm.
        _error.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(170, 35, 50);
        _error.Font = new Font("Segoe UI", 8);
        root.Controls.Add(_error, 0, 2); root.SetColumnSpan(_error, 2);
        _from.ValueChanged += (_, _) => ValidateRange(); _to.ValueChanged += (_, _) => ValidateRange();
        Controls.Add(root);
    }
    private void ValidateRange() => _error.Text = IsValid ? "" : "Từ ngày phải trước hoặc bằng đến ngày.";
}
