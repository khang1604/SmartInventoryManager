using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class LiveUi
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("vi-VN");

    public static ComboBox Choice(string name, string caption) => new()
    {
        Name = name,
        AccessibleName = caption,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 190,
        Font = new Font("Segoe UI", 10),
        Margin = new Padding(0, 8, 12, 0)
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
        control.Dock = DockStyle.Top;
        control.Font = new Font("Segoe UI", 10);
        control.Margin = new Padding(0, 8, 0, 0);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    public static string ExportError(Exception exception)
    {
        Debug.WriteLine(exception);
        return exception switch
        {
            UnauthorizedAccessException => "Không có quyền ghi vào vị trí đã chọn. Hãy chọn thư mục khác.",
            IOException => "Không thể ghi tệp. Hãy đóng tệp đang mở hoặc chọn tên tệp khác.",
            ArgumentException => "Tên hoặc đường dẫn tệp không hợp lệ. Hãy chọn lại nơi lưu.",
            _ => "Không thể xuất báo cáo. Hãy kiểm tra đường dẫn lưu, quyền ghi và tệp đang mở rồi thử lại."
        };
    }

    public static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Fill,
        WrapContents = false,
        AutoScroll = true,
        Margin = Padding.Empty
    };

    public static void FitWrappedToolbar(FlowLayoutPanel toolbar, TableLayoutPanel root, int row)
    {
        toolbar.WrapContents = true;
        toolbar.AutoScroll = false;
        void Fit()
        {
            var width = Math.Max(160, toolbar.ClientSize.Width);
            var used = 0;
            var height = 0;
            var lineHeight = 0;
            foreach (Control child in toolbar.Controls)
            {
                var childWidth = child.Width + child.Margin.Horizontal;
                if (used > 0 && used + childWidth > width)
                {
                    height += lineHeight; used = 0; lineHeight = 0;
                }
                used += childWidth;
                lineHeight = Math.Max(lineHeight, child.Height + child.Margin.Vertical);
            }
            var required = height + lineHeight + 12;
            if (root.RowStyles[row].Height != required) root.RowStyles[row].Height = required;
        }
        toolbar.SizeChanged += (_, _) => Fit();
        toolbar.ControlAdded += (_, _) => Fit();
        Fit();
    }

    public static Control TableFrame(Control content)
    {
        var frame = new RoundedSurface
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = Padding.Empty,
            AccessibleName = "Khung bảng dữ liệu"
        };
        content.Dock = DockStyle.Fill;
        content.Margin = Padding.Empty;
        frame.Controls.Add(content);
        void StyleTables(Control control)
        {
            if (control is DataGridView table)
            {
                var bodyFont = new Font("Segoe UI", 10F, FontStyle.Regular);
                table.Font = bodyFont;
                Font? headerFont = null;
                void ApplyHeaderFont()
                {
                    var previous = headerFont;
                    headerFont = new Font("Segoe UI", table.Font.Size, FontStyle.Bold);
                    table.ColumnHeadersDefaultCellStyle.Font = headerFont;
                    previous?.Dispose();
                }
                table.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 8, 0);
                table.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                ApplyHeaderFont();
                table.DataSourceChanged += (_, _) =>
                {
                    if (table.Parent is TableLayoutPanel area && area.RowCount == 2 && area.RowStyles.Count == 2)
                    { area.RowStyles[0].SizeType = SizeType.Percent; area.RowStyles[0].Height = 100; area.RowStyles[1].SizeType = SizeType.Absolute; area.RowStyles[1].Height = 0; foreach (Control other in area.Controls) if (!ReferenceEquals(other, table)) other.Visible = false; }
                };
                table.FontChanged += (_, _) => ApplyHeaderFont();
                table.Disposed += (_, _) => { headerFont?.Dispose(); bodyFont.Dispose(); };
                table.BorderStyle = BorderStyle.None;
                table.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                table.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                // [COLOR CONFIG]: đường ngang nhạt, bỏ viền đen giữa các tiêu đề.
                table.GridColor = AppColors.Border;
                void RoundTable()
                {
                    if (table.Width < 2 || table.Height < 2) return;
                    using var outline = new System.Drawing.Drawing2D.GraphicsPath();
                    int radius = Math.Max(8, 8 * table.DeviceDpi / 96);
                    outline.AddRoundedRectangle(table.ClientRectangle, new Size(radius, radius));
                    var previous = table.Region;
                    table.Region = new Region(outline);
                    previous?.Dispose();
                }
                table.SizeChanged += (_, _) => RoundTable();
                table.DpiChangedAfterParent += (_, _) => RoundTable();
                RoundTable();
            }
            foreach (Control child in control.Controls) StyleTables(child);
        }
        StyleTables(content);
        return frame;
    }

    public static DataGridViewColumn Column(DataGridView grid, string name) =>
        grid.Columns[name] ?? throw new InvalidOperationException("Thiếu cột giao diện: " + name);

    public static DataGridView Grid(string name, params (string Property, string Caption)[] columns)
    {
        var grid = new ScrollableGrid
        {
            Name = name,
            AccessibleName = "Bảng dữ liệu",
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            // [COLOR CONFIG]: nền bảng trắng và chữ tối theo theme VioWare.
            BackgroundColor = AppColors.Surface,
            ForeColor = AppColors.Text,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            // [COLOR CONFIG]: đường phân cách bảng nhẹ, đồng bộ với khung bo góc.
            GridColor = AppColors.Border,
            BorderStyle = BorderStyle.None,
            RowTemplate = { Height = 34 }
        };
        foreach (var (property, caption) in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = caption,
                MinimumWidth = property switch
                {
                    "ProductName" => 140,
                    "Unit" => 70,
                    "ReorderPoint" => 130,
                    "PurchasePrice" or "SellingPrice" => 110,
                    _ => 80
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
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
        // [COLOR CONFIG]: header nhạt và lựa chọn lavender theo bảng màu đã duyệt.
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 52;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppColors.Text;
        grid.DefaultCellStyle.SelectionBackColor = AppColors.Lavender;
        grid.DefaultCellStyle.SelectionForeColor = AppColors.SelectedText;
        grid.GridColor = AppColors.Border;
        grid.ScrollBars = ScrollBars.None;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.FormatProvider = Culture;
        return grid;
    }

    public static string StockStatusText(SmartInventoryManager.Business.Inventory.StockStatus status) => status switch
    {
        SmartInventoryManager.Business.Inventory.StockStatus.OutOfStock => "Hết hàng",
        SmartInventoryManager.Business.Inventory.StockStatus.LowStock => "Sắp hết hàng",
        SmartInventoryManager.Business.Inventory.StockStatus.Normal => "Bình thường",
        SmartInventoryManager.Business.Inventory.StockStatus.Overstock => "Vượt mức",
        _ => "Chưa xác định"
    };

    public static string StatusText(string status) => status switch
    {
        "DRAFT" => "Nháp",
        "COMPLETED" => "Hoàn tất",
        "CANCELLED" => "Đã hủy",
        "PENDING_APPROVAL" => "Chờ duyệt",
        "APPROVED" => "Đã duyệt",
        "REJECTED" => "Đã từ chối",
        "OutOfStock" or "OUT_OF_STOCK" => "Hết hàng",
        "LowStock" or "LOW_STOCK" => "Sắp hết",
        "Normal" or "NORMAL" => "Bình thường",
        "Overstock" or "OVERSTOCK" => "Vượt mức",
        "IMPORT" => "Nhập kho",
        "EXPORT" => "Xuất kho",
        "ADJUSTMENT" => "Điều chỉnh",
        "TRANSFER_IN" => "Chuyển kho đến",
        "TRANSFER_OUT" => "Chuyển kho đi",
        _ => "Chưa xác định"
    };

    public static string LogActionText(string action) => action switch
    {
        "COMPLETE_IMPORT" => "Hoàn tất phiếu nhập",
        "COMPLETE_EXPORT" => "Hoàn tất phiếu xuất",
        "ADJUST" => "Điều chỉnh tồn",
        "TRANSFER" => "Chuyển kho",
        "UPDATE_PRODUCT" => "Sửa sản phẩm",
        "SET_PRODUCT_ACTIVE" => "Đổi trạng thái sản phẩm",
        "ACTIVATE_ASSIGNMENT" => "Kích hoạt phân kho",
        "DEACTIVATE_ASSIGNMENT" => "Ngừng phân kho",
        "USER_CREATED" => "Tạo tài khoản",
        "USER_DEACTIVATED" => "Ngừng tài khoản",
        "USER_UPDATED" => "Sửa tài khoản",
        "PASSWORD_RESET" => "Đặt lại mật khẩu",
        "ACCOUNT_UPDATED" => "Sửa hồ sơ cá nhân",
        "PASSWORD_CHANGED" => "Đổi mật khẩu",
        _ => "Thao tác hệ thống"
    };

    public static string Error(Exception exception)
    {
        Debug.WriteLine(exception);
        return exception switch
        {
            SmartInventoryManager.Business.Services.BusinessConflictException conflict => conflict.Message,
            UnauthorizedAccessException => "Bạn chưa đăng nhập hoặc không có quyền thao tác tại kho này.",
            ArgumentException argument when !string.IsNullOrWhiteSpace(argument.Message)
                => argument.Message,
            KeyNotFoundException => "Dữ liệu không còn tồn tại. Hãy làm mới danh sách.",
            InvalidOperationException when exception.Message is
                "Tên đăng nhập hoặc email trùng với thông tin đăng nhập của tài khoản khác."
                or "Hãy đổi hoặc gỡ người quản lý tại các kho trước khi đổi vai trò hoặc vô hiệu hóa tài khoản này."
                or "Hệ thống phải còn ít nhất một ADMIN đang hoạt động." => exception.Message,
            InvalidOperationException when exception.Message.Contains("không đủ", StringComparison.OrdinalIgnoreCase)
                => "Tồn kho không đủ. Hãy giảm số lượng hoặc làm mới tồn kho trước khi thử lại.",
            InvalidOperationException when exception.Message == "Số lượng điều chỉnh không được làm tồn kho âm."
                => "Mức giảm vượt quá tồn hiện tại. Hãy kiểm tra số lượng điều chỉnh.",
            InvalidOperationException when exception.Message is "Phiếu nhập đã hoàn tất trước đó." or "Phiếu xuất đã hoàn tất trước đó."
                => "Phiếu đã hoàn tất. Hãy làm mới danh sách; không cần gửi lại.",
            DbUpdateException => "Không lưu được dữ liệu. Hãy làm mới để kiểm tra kết quả; liên hệ quản trị viên nếu lỗi vẫn xảy ra.",
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
