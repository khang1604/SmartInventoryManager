using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.Forms.Products;

public partial class FrmProductList : Form
{
    public FrmProductList(UiServices services)
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(ConnectedPages.Products(services));
    }
    private readonly bool _canManageProducts;

    public FrmProductList() : this(false) { }

    public FrmProductList(bool canManageProducts)
    {
        _canManageProducts = canManageProducts;
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "Sản phẩm";
        Font = new Font("Segoe UI", 10F);
        // [COLOR CONFIG]: nền và chữ thống nhất với Main.
        BackColor = AppColors.Workspace;
        ForeColor = AppColors.Text;
        var root = ProductUi.Layout(1, 4);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var filters = ProductUi.Layout(3, 2);
        filters.Padding = new Padding(16, 10, 4, 12);
        // [COLOR CONFIG]: vùng lọc trắng như bản thiết kế.
        filters.BackColor = AppColors.Surface;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var search = ProductUi.Input("txtSearch", "Nhập mã hoặc tên sản phẩm");
        search.PrefixSvg = MainIcons.Get("search");
        search.ReadOnly = true;
        search.AccessibleName = "Tìm theo mã hoặc tên sản phẩm";
        search.AccessibleDescription = "Chức năng tìm kiếm chưa sẵn sàng";
        search.TabIndex = 0;
        var category = Filter("cboCategory", "Tất cả danh mục", 1);
        var status = Filter("cboStatus", "Tất cả trạng thái", 2);
        foreach (var (index, caption) in new[] { (0, "Tìm kiếm"), (1, "Danh mục"), (2, "Trạng thái") })
            filters.Controls.Add(ProductUi.Label(caption, 10, true), index, 0);
        filters.Controls.Add(search, 0, 1);
        filters.Controls.Add(category, 1, 1);
        filters.Controls.Add(status, 2, 1);
        root.Controls.Add(filters, 0, 0);

        var toolbar = ProductUi.Layout(2, 1);
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.Padding = new Padding(0, 14, 0, 10);
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty, AutoScroll = true };
        if (_canManageProducts)
        {
            var add = ProductUi.Button("btnAdd", "Thêm sản phẩm", "plus-lg", true);
            add.Width = 176;
            add.TabIndex = 3;
            add.AccessibleDescription = "Mở biểu mẫu xem trước, chưa lưu sản phẩm";
            add.Click += (_, _) => OpenPreview();
            actions.Controls.Add(add);
        }
        var preview = ProductUi.Button("btnPreview", "Xem chi tiết", "eye");
        preview.TabIndex = 4;
        preview.Enabled = false;
        preview.AccessibleDescription = "Chọn một sản phẩm để xem chi tiết. Danh sách chưa được tải dữ liệu.";
        actions.Controls.Add(preview);
        if (_canManageProducts)
        {
            var edit = Unavailable("btnEdit", "Sửa", "pencil");
            edit.Width = 100;
            actions.Controls.Add(edit);
            var deactivate = Unavailable("btnDeactivate", "Ngừng kinh doanh", "dash-circle");
            deactivate.Width = 195;
            actions.Controls.Add(deactivate);
        }
        toolbar.Controls.Add(actions, 0, 0);
        var refresh = Unavailable("btnRefresh", "", "arrow-clockwise");
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = Padding.Empty;
        refresh.AccessibleName = "Làm mới danh sách sản phẩm";
        toolbar.Controls.Add(refresh, 1, 0);
        root.Controls.Add(toolbar, 0, 1);

        var tableArea = ProductUi.Layout(1, 2);
        tableArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        tableArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng cho vùng danh sách và trạng thái trống.
        tableArea.BackColor = AppColors.Surface;
        var grid = new DataGridView
        {
            Name = "dgvProducts", Dock = DockStyle.Fill, ReadOnly = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToOrderColumns = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 34 },
            AutoGenerateColumns = false, RowHeadersVisible = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BorderStyle = BorderStyle.None, AccessibleName = "Danh sách sản phẩm", TabIndex = 8,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Margin = Padding.Empty,
            // [COLOR CONFIG]: nền trắng và đường kẻ nhẹ.
            BackgroundColor = AppColors.Surface, GridColor = AppColors.Border
        };
        // [COLOR CONFIG]: header lavender nhạt, chữ tối dễ đọc.
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.Columns.AddRange(
            Column("ProductCode", "Mã sản phẩm", 92), Column("ProductName", "Tên sản phẩm", 145),
            Column("CategoryName", "Danh mục", 92), Column("Unit", "Đơn vị", 65),
            Column("PurchasePrice", "Giá nhập (đ)", 90, "N2"), Column("SellingPrice", "Giá bán (đ)", 90, "N2"),
            Column("ReorderPoint", "Điểm đặt hàng lại", 130, "N0"), Column("MaximumStock", "Tồn tối đa", 85, "N0"),
            Column("Status", "Trạng thái", 95));
        tableArea.Controls.Add(grid, 0, 0);
        var empty = ProductUi.Layout(1, 5);
        empty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var symbol = ProductUi.Button("emptyIcon", "", "box-seam");
        symbol.IconSize = new Size(60, 60);
        symbol.BorderWidth = 0;
        symbol.Dock = DockStyle.Fill;
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        empty.Controls.Add(symbol, 0, 1);
        var emptyTitle = ProductUi.Label("Chưa có dữ liệu để hiển thị", 13, true);
        emptyTitle.TextAlign = ContentAlignment.MiddleCenter;
        var description = ProductUi.Label("Chức năng tải dữ liệu đang được hoàn thiện");
        description.TextAlign = ContentAlignment.MiddleCenter;
        // [COLOR CONFIG]: chữ phụ trong trạng thái chưa tải dữ liệu.
        description.ForeColor = AppColors.Muted;
        empty.Controls.Add(emptyTitle, 0, 2);
        empty.Controls.Add(description, 0, 3);
        tableArea.Controls.Add(empty, 0, 1);
        root.Controls.Add(tableArea, 0, 2);
        tableArea.SizeChanged += (_, _) =>
        {
            int minimumTableWidth = grid.Columns.Cast<DataGridViewColumn>().Sum(column => column.MinimumWidth);
            tableArea.RowStyles[0].Height = (tableArea.ClientSize.Width < minimumTableWidth ? 74 : 54) * DeviceDpi / 96F;
        };
        var note = ProductUi.Label("Chưa kết nối dữ liệu • Biểu mẫu xem trước chưa lưu sản phẩm", 9);
        // [COLOR CONFIG]: thông tin trạng thái phụ.
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, 3);
        Controls.Add(root);
    }

    private void OpenPreview()
    {
        using var preview = new FrmProductDetail();
        preview.ShowDialog(this);
    }

    private static AntdUI.Input Filter(string name, string text, int tabIndex)
    {
        var filter = ProductUi.Input(name);
        filter.Text = text;
        filter.ReadOnly = true;
        filter.SuffixSvg = MainIcons.Get("chevron-down");
        filter.TabIndex = tabIndex;
        filter.AccessibleName = text;
        filter.AccessibleDescription = "Bộ lọc chưa sẵn sàng khi chưa có dữ liệu";
        return filter;
    }

    private static AntdUI.Button Unavailable(string name, string caption, string icon)
    {
        var button = ProductUi.Button(name, caption, icon);
        button.Enabled = false;
        button.AccessibleDescription = "Chưa sẵn sàng: chức năng dữ liệu đang được hoàn thiện";
        return button;
    }

    private static DataGridViewTextBoxColumn Column(string name, string caption, int minWidth, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = name, HeaderText = caption, MinimumWidth = minWidth, FillWeight = minWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        // DataPropertyName sẽ được gán khi Khang hoàn thiện hợp đồng DTO.
        if (format is not null)
        {
            column.DefaultCellStyle.Format = format;
            column.DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        return column;
    }
}
