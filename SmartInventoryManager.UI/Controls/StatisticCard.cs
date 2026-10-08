namespace SmartInventoryManager.Controls
{
    /// <summary>Skeleton UserControl for the StatisticCard component.</summary>
    public partial class StatisticCard : UserControl
    {
        public StatisticCard()
        {
            InitializeComponent();
            var body = SmartInventoryManager.UI.Components.ProductUi.Layout(1, 3);
            body.Padding = new Padding(12);
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            // [COLOR CONFIG]: nền trắng của thẻ chỉ số chưa có dữ liệu.
            body.BackColor = SmartInventoryManager.UI.Themes.AppColors.Surface;
            _title = SmartInventoryManager.UI.Components.ProductUi.Label("Chỉ số", 10, true);
            body.Controls.Add(_title, 0, 0);
            body.Controls.Add(SmartInventoryManager.UI.Components.ProductUi.Label("—", 22, true), 0, 1);
            var note = SmartInventoryManager.UI.Components.ProductUi.Label("Chưa có dữ liệu", 8.5F);
            // [COLOR CONFIG]: ghi chú phụ của chỉ số chưa sẵn sàng.
            note.ForeColor = SmartInventoryManager.UI.Themes.AppColors.Muted;
            body.Controls.Add(note, 0, 2);
            Controls.Add(body);
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 0, 10, 12);
        }
        private readonly Label _title;
        [System.ComponentModel.DefaultValue("Chỉ số")]
        public string MetricTitle { get => _title.Text; set { _title.Text = value; AccessibleName = value; } }
    }
}

