namespace SmartInventoryManager.UI.Components;

public class NotificationPanel : Panel
{
    public NotificationPanel()
    {
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(1, 2);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.Controls.Add(WorkflowUi.List("dgvNotifications",
            [("Tìm kiếm", "Nhập tiêu đề thông báo"), ("Đã đọc", "Tất cả thông báo"), ("Thời gian", "Chọn khoảng thời gian")],
            ["Tiêu đề", "Loại thông báo", "Thời gian", "Trạng thái đọc"], "bell",
            pendingActions: ["Đánh dấu đã đọc", "Đọc tất cả"]), 0, 0);
        var content = ProductUi.Label("Nội dung thông báo\nChọn một thông báo để xem nội dung khi dữ liệu đã được tải.", 11);
        content.Padding = new Padding(16);
        // [COLOR CONFIG]: vùng nội dung thông báo trắng.
        content.BackColor = Themes.AppColors.Surface;
        root.Controls.Add(content, 0, 1);
        Controls.Add(root);
    }
}
