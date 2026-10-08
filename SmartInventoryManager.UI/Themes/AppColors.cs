namespace SmartInventoryManager.UI.Themes;

public static class AppColors
{
    // [COLOR CONFIG]: #9FA1FF, màu chủ đạo được KH duyệt cho nút chính.
    public static Color Primary => SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(159, 161, 255);
    // [COLOR CONFIG]: #B5BAFF, mục điều hướng được chọn.
    public static Color Lavender => SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(181, 186, 255);
    // [COLOR CONFIG]: #AEE2FF, điểm nhấn thông tin.
    public static Color Sky => SystemInformation.HighContrast ? SystemColors.Control : Color.FromArgb(174, 226, 255);
    // [COLOR CONFIG]: #D9F9DF, điểm nhấn xanh mint.
    public static Color Mint => SystemInformation.HighContrast ? SystemColors.Control : Color.FromArgb(217, 249, 223);
    // [COLOR CONFIG]: #F7F7FF, nền làm việc sáng.
    public static Color Workspace => SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(247, 247, 255);
    // [COLOR CONFIG]: trắng, nền các vùng nội dung.
    public static Color Surface => SystemColors.Window;
    // [COLOR CONFIG]: #2D3142, chữ tối dễ đọc trên nền pastel.
    public static Color Text => SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(45, 49, 66);
    // [COLOR CONFIG]: #626780, chữ mô tả và trạng thái.
    public static Color Muted => SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(98, 103, 128);
    // [COLOR CONFIG]: #DDDFF4, đường phân chia nhẹ.
    public static Color Border => SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(221, 223, 244);
    // [COLOR CONFIG]: chữ trên vùng được chọn, giữ tương phản khi bật High Contrast.
    public static Color SelectedText => SystemInformation.HighContrast ? SystemColors.HighlightText : Text;
}
