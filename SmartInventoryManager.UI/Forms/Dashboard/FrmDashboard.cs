using SmartInventoryManager.UI.Components;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Controls;

namespace SmartInventoryManager.Forms.Dashboard;

public partial class FrmDashboard : Form
{
    private readonly UiServices? _services;
    private readonly ComboBox _warehouse = LiveUi.Choice("cboDashboardWarehouse","Kho");
    private readonly DateRangeControl _period = new();
    private readonly AntdUI.Button _refresh = ProductUi.Button("btnRefreshDashboard","","arrow-clockwise");
    private readonly AlertCard _alerts = new();
    private DataGridView _activity = null!;
    private bool _loading;
    private readonly System.Windows.Forms.Timer _refreshDelay = new() { Interval=300 };
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
        var warehouse = _warehouse; warehouse.Dock=DockStyle.Fill;
        warehouse.AccessibleName = "Phạm vi kho";
        var period = _period;
        var warehouseField = ProductUi.Layout(1, 3);
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        warehouseField.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        warehouseField.Controls.Add(ProductUi.Label("Kho", 10, true), 0, 0);
        warehouseField.Controls.Add(warehouse, 0, 1);
        filters.Controls.Add(warehouseField, 0, 0);
        filters.Controls.Add(period, 1, 0);
        var refresh = _refresh; refresh.AccessibleName="Làm mới Dashboard"; refresh.Click+=async(_,_)=>await LoadDataAsync();
        refresh.Dock = DockStyle.Top;
        refresh.Height = 40;
        refresh.Margin = new Padding(0, 26, 0, 0);
        filters.Controls.Add(refresh, 2, 0);
        root.Controls.Add(filters, 0, 0);
        var cards = ProductUi.Layout(5, 1);
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var captions = new[] { "Dòng tồn kho", "Số lượng tồn", "Giá trị tồn (đ)", "Cảnh báo mở", "Hết hàng" };
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
        panels.Controls.Add(_alerts, 1, 0);
        root.Controls.Add(panels, 0, 2);
        var activity = ProductUi.Layout(1, 2);
        activity.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        activity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        activity.Controls.Add(ProductUi.Label("Hoạt động gần đây", 12, true), 0, 0);
        var grid = LiveUi.Grid("dgvRecentActivity", [("ReceiptCode","Mã phiếu"),("ReceiptType","Loại phiếu"),("WarehouseName","Kho"),("ReceiptDate","Ngày"),("Status","Trạng thái")]); _activity=grid;
        grid.CellFormatting+=(_,e)=> { if(e.Value is string value && grid.Columns[e.ColumnIndex].Name is "ReceiptType" or "Status") e.Value=LiveUi.StatusText(value); };
        activity.Controls.Add(LiveUi.TableFrame(grid), 0, 1);
        root.Controls.Add(activity, 0, 3);
        _note = ProductUi.Label("Chọn kho và khoảng ngày để xem dữ liệu", 9);
        _note.ForeColor = AppColors.Muted;
        root.Controls.Add(_note, 0, 4);
        AutoScroll = true;
        root.Dock = DockStyle.Top;
        root.Height = 620;
        SizeChanged += (_, _) => root.Height = Math.Max(620, ClientSize.Height);
        Controls.Add(root);
        if (_services is not null)
        {
            _refreshDelay.Tick += async (_,_) => { _refreshDelay.Stop(); if(_services.HasPendingOperations) { _refreshDelay.Start(); return; } if(Visible) await LoadDataAsync(); };
            _warehouse.SelectedIndexChanged += (_,_) => QueueRefresh();
            _period.RangeChanged += (_,_) => QueueRefresh();
            Shown += async (_,_) => await LoadDataAsync();
            VisibleChanged += (_,_) => { if(Visible) QueueRefresh(); };
            EventHandler<SmartInventoryManager.Business.Events.StockAlertEvent> changed = (_,_) => { if(IsHandleCreated && !IsDisposed) BeginInvoke(new Action(QueueRefresh)); };
            _services.Alerts.AlertChanged += changed;
            Disposed += (_,_) => { _services.Alerts.AlertChanged -= changed; _refreshDelay.Dispose(); };
        }
        else Disposed += (_,_) => _refreshDelay.Dispose();
    }

    private void QueueRefresh() { if(_loading || IsDisposed || _services is null) return; _refreshDelay.Stop(); _refreshDelay.Start(); }

    private async Task LoadDataAsync()
    {
        if(_services is null || _loading) return;
        if(!_period.IsValid) { _note.Text="Ngày bắt đầu không được sau ngày kết thúc."; return; }
        _loading=true; _refresh.Enabled=_warehouse.Enabled=_period.Enabled=false;
        _note.Text="Đang tải dữ liệu…";
        try
        {
            var choices=await _services.RunAsync(()=>_services.Warehouses.GetActiveAsync(_services.Session));
            if(_warehouse.DataSource is null) { _warehouse.DisplayMember="Name"; _warehouse.ValueMember="Id"; _warehouse.DataSource=new[]{new LookupItemDto(0,"","Tất cả kho được phép")}.Concat(choices).ToList(); }
            int? warehouseId=(_warehouse.SelectedItem as LookupItemDto)?.Id; if(warehouseId==0) warehouseId=null;
            var data=await _services.RunAsync(()=>_services.Dashboard.GetAsync(_services.Session,warehouseId,_period.From,_period.To));
            if(IsDisposed)return;
            _cards[0].MetricValue=data.InventoryLineCount.ToString("N0"); _cards[1].MetricValue=data.TotalQuantity.ToString("N0");
            _cards[2].MetricValue="—"; _cards[2].MetricNote="Chưa thống nhất định giá";
            foreach(var index in new[]{0,1,3,4}) _cards[index].MetricNote="Tồn hiện tại trong kho đã chọn"; _cards[3].MetricValue=data.OpenAlertCount.ToString("N0"); _cards[4].MetricValue=data.OutOfStockCount.ToString("N0");
            _chart.ChartTitle="20 biến động gần nhất • số lượng";
            var series=data.RecentTransactions.GroupBy(row=>row.TransactionType).Select(group=>(LiveUi.StatusText(group.Key),group.Sum(row=>(double)row.Quantity))).ToArray();
            _chart.ShowSeries(series.Select(row=>row.Item1).ToArray(),series.Select(row=>row.Item2).ToArray());
            var alerts=new List<(StockAlertDto Alert,string Warehouse)>();
            foreach(var choice in choices.Where(row=>!warehouseId.HasValue || row.Id==warehouseId)) {
                var rows=await _services.RunAsync(()=>_services.Alerts.GetOpenAsync(_services.Session,choice.Id)); alerts.AddRange(rows.Select(row=>(row,choice.Name)));
            }
            _alerts.ShowAlerts(alerts.OrderByDescending(row=>row.Alert.CreatedAt).Take(10).ToList());
            var report=await _services.RunAsync(()=>_services.Reports.GetImportExportAsync(_services.Session,warehouseId,_period.From,_period.To));
            _activity.DataSource=report.Imports.Select(row=>new ImportExportReportRowDto(row.ReceiptDate,row.ReceiptCode,"IMPORT",row.WarehouseName,row.TotalAmount,row.Status))
                .Concat(report.Exports.Select(row=>new ImportExportReportRowDto(row.ReceiptDate,row.ReceiptCode,"EXPORT",row.WarehouseName,row.TotalAmount,row.Status))).OrderByDescending(row=>row.ReceiptDate).Take(10).ToList();
            _note.Text=$"Đã cập nhật {DateTime.Now:HH:mm} • Phiếu hoàn tất trong khoảng ngày đã chọn • Tồn là số liệu hiện tại; giá trị tồn chưa có quy định định giá";
        }
        catch(Exception ex) { if(!IsDisposed) { foreach(var card in _cards) { card.MetricValue="—"; card.MetricNote="Không tải được số liệu"; } _activity.DataSource=null; _alerts.ShowAlerts([]); _chart.ShowSeries([],[]); _note.Text=LiveUi.LoadError(ex); } }
        finally { _loading=false; if(!IsDisposed) _refresh.Enabled=_warehouse.Enabled=_period.Enabled=true; }
    }
}
