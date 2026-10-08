using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Controls;

namespace SmartInventoryManager.Forms.Dashboard;

public partial class FrmDashboard : Form
{
    private readonly UiServices? _services;
    private readonly StatisticCard[] _cards = new StatisticCard[5];
    private readonly ChartControl _chart;
    private readonly Label _note;

    public FrmDashboard() : this(null)
    {
    }

    public FrmDashboard(UiServices? services)
    {
        _services = services;
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        var root = ProductUi.Layout(1, 5);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 57));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var filters = ProductUi.Layout(3, 1);
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filters.Padding = new Padding(0, 0, 0, 8);
        var warehouse = ProductUi.Input("cboWarehouse", "Tất cả kho");
        warehouse.ReadOnly = true;
        warehouse.SuffixSvg = MainIcons.Get("chevron-down");
        warehouse.AccessibleName = "Phạm vi kho";
        var period = new DateRangeControl();
        var warehouseField = ProductUi.Layout(1, 3);
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        warehouseField.Controls.Add(ProductUi.Label("Kho", 10, true), 0, 0);
        warehouseField.Controls.Add(warehouse, 0, 1);
        filters.Controls.Add(warehouseField, 0, 0);
        filters.Controls.Add(period, 1, 0);
        var refresh = WorkflowUi.Pending("btnRefresh", "", "arrow-clockwise");
        refresh.Dock = DockStyle.Top;
        refresh.Height = 40;
        refresh.Margin = new Padding(0, 26, 0, 0);
        filters.Controls.Add(refresh, 2, 0);
        root.Controls.Add(filters, 0, 0);
        var cards = ProductUi.Layout(5, 1);
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var captions = new[] { "Sản phẩm", "Số lượng tồn", "Giá trị tồn (đ)", "Sắp hết hàng", "Hết hàng" };
        for (var i = 0; i < captions.Length; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            _cards[i] = new StatisticCard { MetricTitle = captions[i] };
            cards.Controls.Add(_cards[i]);
        }
        root.Controls.Add(cards, 0, 1);
        var panels = ProductUi.Layout(2, 1);
        panels.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        _chart = new ChartControl();
        panels.Controls.Add(_chart, 0, 0);
        panels.Controls.Add(new AlertCard(), 1, 0);
        root.Controls.Add(panels, 0, 2);
        var activity = ProductUi.Layout(1, 2);
        activity.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        activity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        activity.Controls.Add(ProductUi.Label("Hoạt động gần đây", 12, true), 0, 0);
        var grid = WorkflowUi.Grid("dgvRecentActivity", ["Mã phiếu", "Loại phiếu", "Kho", "Ngày", "Trạng thái"]);
        activity.Controls.Add(LiveUi.TableFrame(grid), 0, 1);
        root.Controls.Add(activity, 0, 3);
        _note = ProductUi.Label("Chưa tải dữ liệu • Số liệu thống kê đang được hoàn thiện", 9);
        _note.ForeColor = AppColors.Muted;
        root.Controls.Add(_note, 0, 4);
        AutoScroll = true;
        root.Dock = DockStyle.Top;
        root.Height = 620;
        SizeChanged += (_, _) => root.Height = Math.Max(620, ClientSize.Height);
        Controls.Add(root);
        if (_services is not null) Shown += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var data = await _services!.RunAsync(() => _services.Dashboard.GetAsync(_services.Session));
            _cards[0].MetricValue = data.InventoryLineCount.ToString("N0");
            _cards[1].MetricValue = data.TotalQuantity.ToString("N0");
            _cards[2].MetricValue = "Chưa có công thức";
            _cards[3].MetricValue = data.OpenAlertCount.ToString("N0");
            _cards[4].MetricValue = data.OutOfStockCount.ToString("N0");
            var series = data.RecentTransactions.GroupBy(item => item.TransactionType).OrderBy(item => item.Key)
                .Select(item => (item.Key, item.Sum(value => (double)value.Quantity))).ToArray();
            _chart.ShowSeries(series.Select(item => item.Key).ToArray(), series.Select(item => item.Item2).ToArray());
            _note.Text = "Đã cập nhật dữ liệu từ cơ sở dữ liệu";
        }
        catch (Exception exception)
        {
            _note.Text = exception.Message;
        }
    }
}
