using System.Diagnostics;
using System.Globalization;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class LiveUi
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("vi-VN");

    public static ComboBox Choice(string name, string caption) => new()
    {
        Name = name, AccessibleName = caption, DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 190, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 8, 12, 0)
    };

    public static Control Field(string caption, Control control)
    {
        var panel = ProductUi.Layout(1, 2);
        panel.Size = new Size(Math.Max(160, control.Width), 70);
        panel.Dock = DockStyle.None;
        panel.Margin = new Padding(0, 0, 12, 0);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(ProductUi.Label(caption), 0, 0);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 6, 0, 6);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    public static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty
    };

    public static DataGridViewColumn Column(DataGridView grid, string name) =>
        grid.Columns[name] ?? throw new InvalidOperationException("Thiếu cột giao diện: " + name);

    public static DataGridView Grid(string name, params (string Property, string Caption)[] columns)
    {
        var grid = new DataGridView
        {
            Name = name, AccessibleName = "Bảng dữ liệu", Dock = DockStyle.Fill,
            AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            // [COLOR CONFIG]: nền bảng trắng và chữ tối theo theme VioWare.
            BackgroundColor = AppColors.Surface, ForeColor = AppColors.Text,
            BorderStyle = BorderStyle.None, RowTemplate = { Height = 34 }
        };
        foreach (var (property, caption) in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = property, DataPropertyName = property, HeaderText = caption,
                MinimumWidth = 110, SortMode = DataGridViewColumnSortMode.NotSortable
            });
            var column = Column(grid, property);
            if (property.Contains("Price") || property.Contains("Amount"))
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = "N2";
            }
            else if (property is "Quantity" or "CurrentQuantity" or "Threshold" or "ReorderPoint" or "MaximumStock")
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = "N0";
            }
            else if (property is "CreatedAt" or "UpdatedAt" or "ReceiptDate")
                column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
        grid.DefaultCellStyle.FormatProvider = Culture;
        return grid;
    }

    public static string StatusText(string status) => status switch
    {
        "DRAFT" => "Nháp", "COMPLETED" => "Hoàn tất", "CANCELLED" => "Đã hủy",
        "OUT_OF_STOCK" => "Hết hàng", "LOW_STOCK" => "Sắp hết",
        "NORMAL" => "Bình thường", "OVERSTOCK" => "Vượt mức",
        "IMPORT" => "Nhập kho", "EXPORT" => "Xuất kho", "ADJUSTMENT" => "Điều chỉnh",
        "TRANSFER_IN" => "Chuyển kho đến", "TRANSFER_OUT" => "Chuyển kho đi",
        _ => "Chưa xác định"
    };

    public static string Error(Exception exception)
    {
        Debug.WriteLine(exception);
        return exception switch
        {
            UnauthorizedAccessException => "Bạn chưa đăng nhập hoặc không có quyền thao tác tại kho này.",
            ArgumentException => "Thông tin chưa hợp lệ. Kiểm tra các trường bắt buộc, số lượng và đơn giá.",
            KeyNotFoundException => "Dữ liệu không còn tồn tại. Hãy làm mới danh sách.",
            InvalidOperationException when exception.Message.Contains("không đủ", StringComparison.OrdinalIgnoreCase)
                => "Tồn kho không đủ. Hãy giảm số lượng hoặc làm mới tồn kho trước khi thử lại.",
            InvalidOperationException when exception.Message == "Số lượng điều chỉnh không được làm tồn kho âm."
                => "Mức giảm vượt quá tồn hiện tại. Hãy kiểm tra số lượng điều chỉnh.",
            _ => "Chưa thực hiện được thao tác. Hãy làm mới dữ liệu hoặc thử lại; không gửi lại khi chưa kiểm tra kết quả."
        };
    }

    public static string LoadError(Exception exception)
    {
        Debug.WriteLine(exception);
        return exception is UnauthorizedAccessException
            ? "Bạn chưa đăng nhập hoặc không có quyền xem dữ liệu."
            : "Không tải được dữ liệu. Nhấn Làm mới để thử lại.";
    }
}
