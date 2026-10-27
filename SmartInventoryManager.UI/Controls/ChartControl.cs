namespace SmartInventoryManager.Controls
{
    public partial class ChartControl : UserControl
    {
        private readonly Panel _plotHost;
        // Khởi tạo khung biểu đồ với tiêu đề và vùng vẽ dữ liệu.
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

        // Hiển thị biểu đồ một dãy giá trị theo các nhãn tương ứng.
        public void ShowSeries(IReadOnlyList<string> labels, IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(labels);
            ArgumentNullException.ThrowIfNull(values);
            if (labels.Count != values.Count) throw new ArgumentException("Nhãn và dữ liệu biểu đồ phải cùng số lượng.");
            foreach (Control old in _plotHost.Controls.Cast<Control>().ToArray()) { _plotHost.Controls.Remove(old); old.Dispose(); }
            if (values.Count == 0)
            {
                _plotHost.Controls.Add(SmartInventoryManager.UI.Components.WorkflowUi.Empty(
                    "Không có biến động trong khoảng ngày", "Thử đổi kho hoặc khoảng ngày.", "bar-chart"));
                return;
            }
            _plotHost.Controls.Add(new BarChartSurface(labels, values));
        }

        // Hiển thị biểu đồ so sánh số lượng nhập và xuất theo từng nhãn.
        public void ShowComparison(IReadOnlyList<string> labels, IReadOnlyList<double> imports, IReadOnlyList<double> exports)
        {
            if (labels.Count != imports.Count || labels.Count != exports.Count) throw new ArgumentException("Dữ liệu biểu đồ không khớp.");
            ReplacePlot(new ComparisonSurface(labels.ToArray(), imports.ToArray(), exports.ToArray()));
        }

        // Hiển thị biểu đồ thanh ngang cho các nhãn và giá trị.
        public void ShowHorizontal(IReadOnlyList<string> labels, IReadOnlyList<double> values)
        {
            if (labels.Count != values.Count) throw new ArgumentException("Dữ liệu biểu đồ không khớp.");
            ReplacePlot(new HorizontalSurface(labels.ToArray(), values.ToArray()));
        }

        // Thay nội dung biểu đồ và giải phóng điều khiển biểu đồ cũ.
        private void ReplacePlot(Control plot)
        {
            foreach (Control old in _plotHost.Controls.Cast<Control>().ToArray()) { _plotHost.Controls.Remove(old); old.Dispose(); }
            plot.Dock = DockStyle.Fill;
            _plotHost.Controls.Add(plot);
        }

        private sealed class ComparisonSurface(string[] labels, double[] imports, double[] exports) : Control
        {
            // Vẽ biểu đồ, nhãn và các giá trị theo kích thước vùng hiển thị.
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (labels.Length == 0) return;
                var area = new Rectangle(35, 34, Math.Max(1, Width - 45), Math.Max(1, Height - 62));
                var maximum = Math.Max(1, Math.Max(imports.Max(), exports.Max()));
                var slot = area.Width / (double)labels.Length;
                // [COLOR CONFIG]: system highlight cho nhập, control-dark cho xuất; có chú giải chữ để phân biệt.
                using var importBrush = new SolidBrush(SystemColors.Highlight);
                using var exportBrush = new SolidBrush(SystemColors.ControlDark);
                using var textBrush = new SolidBrush(SystemColors.ControlText);
                using var axis = new Pen(SystemColors.ControlText);
                e.Graphics.DrawString("Nhập ■   Xuất ▧  • số lượng", Font, textBrush, 0, 0);
                e.Graphics.DrawLine(axis, area.Left, area.Bottom, area.Right, area.Bottom);
                e.Graphics.DrawString(maximum.ToString("N0"), Font, textBrush, 0, area.Top);
                for (var i = 0; i < labels.Length; i++)
                {
                    var width = Math.Max(1, (float)(slot * .38));
                    var x = area.Left + (float)(i * slot);
                    var ih = (float)(imports[i] / maximum * area.Height);
                    var eh = (float)(exports[i] / maximum * area.Height);
                    e.Graphics.FillRectangle(importBrush, x, area.Bottom - ih, width, ih);
                    e.Graphics.FillRectangle(exportBrush, x + width, area.Bottom - eh, width, eh);
                    if (i % Math.Max(1, labels.Length / 5) == 0)
                        e.Graphics.DrawString(labels[i], Font, textBrush, x, area.Bottom + 4);
                }
            }
        }

        private sealed class HorizontalSurface(string[] labels, double[] values) : Control
        {
            // Vẽ biểu đồ, nhãn và các giá trị theo kích thước vùng hiển thị.
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                // [COLOR CONFIG]: dùng màu system để giữ tương phản ở High Contrast, luôn hiển thị giá trị bằng chữ.
                using var bar = new SolidBrush(SystemColors.Highlight);
                using var text = new SolidBrush(SystemColors.ControlText);
                if (values.Length == 0) { e.Graphics.DrawString("Chưa có dòng tồn kho", Font, text, 0, 0); return; }
                var max = Math.Max(1, values.Max());
                var row = Math.Max(1, Height / (float)values.Length);
                var labelWidth = Math.Min(180, Width * .5f);
                for (var i = 0; i < values.Length; i++)
                {
                    var y = i * row;
                    using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                    e.Graphics.DrawString(labels[i], Font, text, new RectangleF(0, y, labelWidth - 6, row), format);
                    var width = (float)(values[i] / max * Math.Max(1, Width - labelWidth - 45));
                    e.Graphics.FillRectangle(bar, labelWidth, y + 3, width, Math.Max(1, row - 8));
                    e.Graphics.DrawString(values[i].ToString("N0"), Font, text, labelWidth + width + 4, y);
                }
            }
        }

        private sealed class BarChartSurface : Control
        {
            private readonly string[] _labels;
            private readonly double[] _values;

            // Khởi tạo vùng vẽ biểu đồ thanh từ danh sách nhãn và giá trị.
            public BarChartSurface(IReadOnlyList<string> labels, IReadOnlyList<double> values)
            {
                _labels = labels.ToArray();
                _values = values.ToArray();
                Dock = DockStyle.Fill;
                DoubleBuffered = true;
                ResizeRedraw = true;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            }

            // Vẽ biểu đồ, nhãn và các giá trị theo kích thước vùng hiển thị.
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

