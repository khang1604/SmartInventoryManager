using SmartInventoryManager.Business.DTOs;
namespace SmartInventoryManager.UI.Components;
public class NotificationPanel : Panel
{
    public NotificationPanel() { Dock=DockStyle.Fill; Controls.Add(WorkflowUi.Empty("Thông báo", "Đăng nhập để xem thông báo.","bell")); }
    public NotificationPanel(UiServices services)
    {
        Dock=DockStyle.Fill;
        DataListPage<NotificationDto>? page=null;
        page=new DataListPage<NotificationDto>(services,"Thông báo của tôi",
            [("Title","Tiêu đề"),("NotificationType","Loại thông báo"),("CreatedAt","Thời gian"),("IsRead","Trạng thái đọc")],
            (_,search,from,to)=>services.Notifications.GetAsync(services.Session,search,page?.SelectedType switch {1=>false,2=>true,_=>(bool?)null},from,to),history:true);
        page.SelectedAction("Xem nội dung",(row,_)=> { UiDetails.Show(page,row.Title,[("Nội dung",row.Message),("Thời gian",row.CreatedAt.ToString("dd/MM/yyyy HH:mm")),("Trạng thái",row.IsRead ? "Đã đọc":"Chưa đọc")]); return Task.CompletedTask; },"eye");
        page.SelectedAction("Đánh dấu đã đọc",(row,_)=>services.RunAsync(()=>services.Notifications.MarkAsReadAsync(services.Session,row.NotificationId)));
        page.Action("Đọc tất cả",async()=> { await services.RunAsync(()=>services.Notifications.MarkAllAsReadAsync(services.Session)); await page.RefreshAsync(); });
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var notifications = new TabPage("Thông báo của tôi");
        notifications.Controls.Add(page);
        var alerts = new TabPage("Cảnh báo kho");
        var stockAlerts = (DataListPage<StockAlertDto>)ConnectedPages.Alerts(services);
        alerts.Controls.Add(stockAlerts);
        tabs.TabPages.AddRange([notifications, alerts]);
        Controls.Add(tabs);
        var refresh = new System.Windows.Forms.Timer { Interval = 30000 };
        refresh.Tick += async (_, _) =>
        {
            if (!Visible || services.HasPendingOperations) return;
            if (tabs.SelectedTab == notifications) await page.RefreshAsync();
            else await stockAlerts.RefreshAsync();
        };
        VisibleChanged += (_, _) => refresh.Enabled = Visible;
        EventHandler<SmartInventoryManager.Business.Events.StockAlertEvent> alertChanged = (_, _) =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(async () =>
                {
                    if (!Visible || IsDisposed) return;
                    if (tabs.SelectedTab == notifications) await page.RefreshAsync();
                    else await stockAlerts.RefreshAsync();
                }));
        };
        services.Alerts.AlertChanged += alertChanged;
        Disposed += (_, _) => { refresh.Dispose(); services.Alerts.AlertChanged -= alertChanged; };
    }
}
