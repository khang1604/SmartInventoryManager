using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class ActionFeedback
{
    public static void Show(Control source, string message = "Chưa lưu dữ liệu: chức năng này chưa được kết nối với xử lý nghiệp vụ.")
    {
        var form = source.FindForm();
        if (form is null) return;
        var label = form.Controls.Find("formStatus", true).OfType<Label>().FirstOrDefault()
            ?? form.Controls.Find("actionFeedback", true).OfType<Label>().FirstOrDefault();
        if (label is null)
        {
            label = new Label { Name = "actionFeedback", Dock = DockStyle.Bottom, Height = 48,
                Padding = new Padding(12, 4, 12, 4), AccessibleName = "Kết quả thao tác" };
            // [COLOR CONFIG]: nền thông báo dùng màu workspace hiện có.
            label.BackColor = AppColors.Workspace;
            form.Controls.Add(label);
            label.BringToFront();
        }
        // [COLOR CONFIG]: chữ đỏ tối cho thao tác chưa lưu, luôn có nội dung rõ ràng.
        label.ForeColor = Color.FromArgb(180, 32, 32);
        label.Text = message;
    }
}
