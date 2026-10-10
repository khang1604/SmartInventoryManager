namespace SmartInventoryManager.Controls
{
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
            _plotHost.Controls.Add(new BarChartSurface(labels, values));
        }

        private sealed class BarChartSurface : Control
        {
            private readonly string[] _labels;
            private readonly double[] _values;

            public BarChartSurface(IReadOnlyList<string> labels, IReadOnlyList<double> values)
            {
                _labels = labels.ToArray();
                _values = values.ToArray();
                Dock = DockStyle.Fill;
                DoubleBuffered = true;
                ResizeRedraw = true;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (_values.Length == 0) return;

                var bounds = ClientRectangle;
                var chart = new Rectangle(48, 12, Math.Max(1, bounds.Width - 60), Math.Max(1, bounds.Height - 44));
                var maximum = Math.Max(1d, _values.Max());
                var slotWidth = chart.Width / (double)_values.Length;
                var barWidth = Math.Max(2, (int)(slotWidth * 0.65));

                using var axisPen = new Pen(SystemColors.ControlDark);
                using var barBrush = new SolidBrush(SystemColors.Highlight);
                using var labelBrush = new SolidBrush(SystemColors.ControlText);
                using var labelFont = new Font(Font.FontFamily, Math.Max(7f, Font.Size - 1f));

                e.Graphics.DrawLine(axisPen, chart.Left, chart.Top, chart.Left, chart.Bottom);
                e.Graphics.DrawLine(axisPen, chart.Left, chart.Bottom, chart.Right, chart.Bottom);

                for (var i = 0; i < _values.Length; i++)
                {
                    var height = (int)(Math.Max(0d, _values[i]) / maximum * chart.Height);
                    var x = chart.Left + (int)(i * slotWidth + (slotWidth - barWidth) / 2);
                    var y = chart.Bottom - height;
                    e.Graphics.FillRectangle(barBrush, x, y, barWidth, height);

                    var label = _labels[i];
                    var labelSize = e.Graphics.MeasureString(label, labelFont);
                    e.Graphics.DrawString(label, labelFont, labelBrush,
                        x + (barWidth - labelSize.Width) / 2,
                        chart.Bottom + 4);
                }
            }
        }
    }
}

