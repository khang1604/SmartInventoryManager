using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.Forms.Categories;

public partial class FrmCategory : Form
{
    private readonly bool _canManageCategories;

    public FrmCategory() : this(false) { }

    public FrmCategory(bool canManageCategories)
    {
        _canManageCategories = canManageCategories;
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        UiBrand.ApplyIcon(this);
        Text = "Danh mục";
        Font = new Font("Segoe UI", 10F);
        // [COLOR CONFIG]: nền và chữ đồng bộ với màn hình Sản phẩm.
        BackColor = AppColors.Workspace;
        ForeColor = AppColors.Text;
        var root = ProductUi.Layout(1, 4);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        var filters = ProductUi.Layout(2, 2);
        filters.Padding = new Padding(16, 10, 4, 12);
        // [COLOR CONFIG]: nền trắng của vùng lọc.
        filters.BackColor = AppColors.Surface;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filters.Controls.Add(ProductUi.Label("Tìm kiếm", 10, true), 0, 0);
        filters.Controls.Add(ProductUi.Label("Trạng thái", 10, true), 1, 0);
        var search = ProductUi.Input("txtSearch", "Nhập tên danh mục");
        search.PrefixSvg = MainIcons.Get("search");
        search.ReadOnly = true;
        search.TabIndex = 0;
        search.AccessibleName = "Tìm tên danh mục";
        search.AccessibleDescription = "Tìm kiếm chưa sẵn sàng khi chưa có dữ liệu";
        var status = ProductUi.Input("cboStatus");
        status.Text = "Tất cả trạng thái";
        status.SuffixSvg = MainIcons.Get("chevron-down");
        status.ReadOnly = true;
        status.TabIndex = 1;
        status.AccessibleName = "Lọc trạng thái danh mục";
        status.AccessibleDescription = "Bộ lọc chưa sẵn sàng khi chưa có dữ liệu";
        filters.Controls.Add(search, 0, 1);
        filters.Controls.Add(status, 1, 1);
        root.Controls.Add(filters, 0, 0);

        var toolbar = ProductUi.Layout(2, 1);
        toolbar.Padding = new Padding(0, 14, 0, 10);
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        if (_canManageCategories)
        {
            var add = ProductUi.Button("btnAdd", "Thêm danh mục", "plus-lg", true);
            add.Width = 180;
            add.TabIndex = 2;
            add.AccessibleDescription = "Mở biểu mẫu xem trước, chưa lưu danh mục";
            add.Click += (_, _) =>
            {
                using var detail = new FrmCategoryDetail();
                detail.ShowDialog(this);
            };
            actions.Controls.Add(add);
        }
        var preview = Unavailable("btnPreview", "Xem chi tiết", "eye");
        preview.AccessibleDescription = "Chọn một danh mục để xem chi tiết. Danh sách chưa được tải dữ liệu.";
        actions.Controls.Add(preview);
        if (_canManageCategories)
        {
            var edit = Unavailable("btnEdit", "Sửa", "pencil");
            edit.Width = 100;
            actions.Controls.Add(edit);
            var deactivate = Unavailable("btnDeactivate", "Ngừng hoạt động", "dash-circle");
            deactivate.Width = 190;
            actions.Controls.Add(deactivate);
        }
        toolbar.Controls.Add(actions, 0, 0);
        var refresh = Unavailable("btnRefresh", "", "arrow-clockwise");
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = Padding.Empty;
        refresh.AccessibleName = "Làm mới danh mục";
        toolbar.Controls.Add(refresh, 1, 0);
        root.Controls.Add(toolbar, 0, 1);

        var tableArea = ProductUi.Layout(1, 2);
        tableArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        tableArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng cho danh sách.
        tableArea.BackColor = AppColors.Surface;
        var grid = new DataGridView
        {
            Name = "dgvCategories", Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 34 },
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, Margin = Padding.Empty,
            AccessibleName = "Danh sách danh mục", TabIndex = 6,
            // [COLOR CONFIG]: nền bảng trắng, đường kẻ nhẹ.
            BackgroundColor = AppColors.Surface, GridColor = AppColors.Border
        };
        // [COLOR CONFIG]: header nhạt với chữ tối.
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 0, 0, 0);
        grid.Columns.AddRange(Column("CategoryName", "Tên danh mục", 35),
            Column("Description", "Mô tả", 45), Column("Status", "Trạng thái", 20));
        UiDetails.AddColumn(grid);
        tableArea.Controls.Add(grid, 0, 0);
        var empty = ProductUi.Layout(1, 5);
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var symbol = ProductUi.Button("emptyIcon", "", "grid");
        symbol.Dock = DockStyle.Fill;
        symbol.IconSize = new Size(56, 56);
        symbol.BorderWidth = 0;
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        var caption = ProductUi.Label("Chưa có dữ liệu để hiển thị", 13, true);
        caption.TextAlign = ContentAlignment.MiddleCenter;
        var description = ProductUi.Label("Chức năng tải dữ liệu đang được hoàn thiện");
        description.TextAlign = ContentAlignment.MiddleCenter;
        // [COLOR CONFIG]: chữ phụ trong trạng thái chưa tải dữ liệu.
        description.ForeColor = AppColors.Muted;
        empty.Controls.Add(symbol, 0, 1);
        empty.Controls.Add(caption, 0, 2);
        empty.Controls.Add(description, 0, 3);
        tableArea.Controls.Add(empty, 0, 1);
        root.Controls.Add(LiveUi.TableFrame(tableArea), 0, 2);
        var note = ProductUi.Label("Chưa kết nối dữ liệu • Biểu mẫu xem trước chưa lưu danh mục", 9);
        // [COLOR CONFIG]: thông tin trạng thái phụ.
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, 3);
        Controls.Add(root);
    }

    private static AntdUI.Button Unavailable(string name, string text, string icon)
    {
        var button = ProductUi.Button(name, text, icon);
        button.Enabled = false;
        button.AccessibleDescription = "Chưa sẵn sàng: chức năng dữ liệu đang được hoàn thiện";
        return button;
    }

    private static DataGridViewTextBoxColumn Column(string name, string caption, float weight) => new()
    {
        Name = name, HeaderText = caption, FillWeight = weight, MinimumWidth = 120,
        SortMode = DataGridViewColumnSortMode.NotSortable
        // Chưa gán DataPropertyName khi hợp đồng DTO chưa có.
    };
}
