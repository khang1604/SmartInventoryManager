namespace SmartInventoryManager.Controls
{
    /// <summary>Skeleton UserControl for the AlertCard component.</summary>
    public partial class AlertCard : UserControl
    {
        public AlertCard()
        {
            InitializeComponent();
            var body = SmartInventoryManager.UI.Components.ProductUi.Layout(1, 2);
            body.Padding = new Padding(14);
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // [COLOR CONFIG]: nền trắng của vùng cảnh báo.
            body.BackColor = SmartInventoryManager.UI.Themes.AppColors.Surface;
            body.Controls.Add(SmartInventoryManager.UI.Components.ProductUi.Label("Cảnh báo tồn kho", 12, true), 0, 0);
            body.Controls.Add(SmartInventoryManager.UI.Components.WorkflowUi.Empty("Chưa tải dữ liệu cảnh báo",
                "Hết hàng • Sắp hết hàng • Vượt mức tồn", "bell"), 0, 1);
            Controls.Add(body);
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 0, 0, 12);
        }
    }
}

