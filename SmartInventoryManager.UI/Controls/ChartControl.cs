namespace SmartInventoryManager.Controls
{
    /// <summary>Skeleton UserControl for the ChartControl component.</summary>
    public partial class ChartControl : UserControl
    {
        private readonly Panel _plotHost;
        public ChartControl()
        {
            InitializeComponent();
            var body = SmartInventoryManager.UI.Components.ProductUi.Layout(1, 2);
            body.Padding = new Padding(14);
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // [COLOR CONFIG]: nền trắng của vùng biểu đồ.
            body.BackColor = SmartInventoryManager.UI.Themes.AppColors.Surface;
            _title = SmartInventoryManager.UI.Components.ProductUi.Label("Nhập / xuất theo thời gian", 12, true);
            body.Controls.Add(_title, 0, 0);
            _plotHost = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(_plotHost, 0, 1);
            Controls.Add(body);
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 0, 10, 12);
        }
        private readonly Label _title;
        [System.ComponentModel.DefaultValue("Nhập / xuất theo thời gian")]
        public string ChartTitle { get => _title.Text; set => _title.Text = value; }

        public void ShowSeries(IReadOnlyList<string> labels, IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(labels);
            ArgumentNullException.ThrowIfNull(values);
            if (labels.Count != values.Count) throw new ArgumentException("Nhãn và dữ liệu biểu đồ phải cùng số lượng.");
            foreach(Control old in _plotHost.Controls.Cast<Control>().ToArray()) { _plotHost.Controls.Remove(old); old.Dispose(); }
            if (values.Count == 0)
            {
                _plotHost.Controls.Add(SmartInventoryManager.UI.Components.WorkflowUi.Empty(
                    "Không có biến động trong khoảng ngày", "Thử đổi kho hoặc khoảng ngày.", "bar-chart"));
                return;
            }
            var plot = new ScottPlot.WinForms.FormsPlot { Dock = DockStyle.Fill };
            plot.Plot.Add.Bars(values.ToArray());
            plot.Plot.Axes.Bottom.SetTicks(Enumerable.Range(0,labels.Count).Select(i=>(double)i).ToArray(),labels.ToArray());
            plot.Plot.Axes.Margins(bottom: 0);
            plot.Refresh();
            _plotHost.Controls.Add(plot);
        }
    }
}

