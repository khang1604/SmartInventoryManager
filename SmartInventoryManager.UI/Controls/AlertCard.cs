namespace SmartInventoryManager.Controls
{
    /// <summary>Skeleton UserControl for the AlertCard component.</summary>
    public partial class AlertCard : UserControl
    {
        public void ShowAlerts(IReadOnlyList<(SmartInventoryManager.Business.DTOs.StockAlertDto Alert, string Warehouse)> rows)
        {
            foreach(Control old in Controls.Cast<Control>().ToArray()) { Controls.Remove(old); old.Dispose(); }
            var body=SmartInventoryManager.UI.Components.ProductUi.Layout(1,2); body.Padding=new Padding(14); body.RowStyles.Add(new RowStyle(SizeType.Absolute,38)); body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            body.Controls.Add(SmartInventoryManager.UI.Components.ProductUi.Label("Cảnh báo đang mở",12,true),0,0);
            var list=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false };
            if(rows.Count==0) list.Controls.Add(new Label {Text="Không có cảnh báo đang mở.",AutoSize=true});
            foreach(var item in rows) { var row=item.Alert; var label=new Label { AutoSize=true,MaximumSize=new Size(280,0),Margin=new Padding(0,0,0,8),Text=$"{row.ProductCode} • {row.ProductName}\nKho: {item.Warehouse}\n{SmartInventoryManager.UI.Components.LiveUi.StatusText(row.AlertType)} • Tồn: {row.CurrentQuantity:N0} • Ngưỡng: {row.Threshold:N0}\n{row.CreatedAt:dd/MM/yyyy HH:mm}" }; list.Controls.Add(label); }
            list.SizeChanged+=(_,_)=> {foreach(Control child in list.Controls) child.MaximumSize=new Size(Math.Max(180,list.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-4),0);};
            body.Controls.Add(list,0,1); Controls.Add(body);
        }

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

