using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Categories;

public partial class FrmCategory : Form
{
    private readonly bool _canManageCategories;
    private readonly UiServices? _services;
    private DataGridView? _grid;
    private AntdUI.Button? _refresh;
    private bool _loading;
    private readonly ComboBox _status = LiveUi.Choice("cboStatus", "Trạng thái danh mục");

    // Khởi tạo màn hình danh sách danh mục.
    public FrmCategory() : this(false) { }

    // Khởi tạo màn hình danh sách danh mục.
    public FrmCategory(bool canManageCategories)
    {
        _canManageCategories = canManageCategories;
        InitializeComponent();
        BuildLayout();
    }

    // Khởi tạo màn hình danh sách danh mục.
    public FrmCategory(UiServices services) : this(services.Authorization.CanManageCategory(services.Session))
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        if(_refresh is not null) { _refresh.Enabled=true; _refresh.Click+=async(_,_)=>await LoadCategoriesAsync(); }
        var search = Controls.Find("txtSearch", true).OfType<AntdUI.Input>().Single();
        search.ReadOnly = false;
        search.AccessibleDescription = "Nhập tên và nhấn Enter để tìm kiếm";
        search.KeyDown += async (_, e) => { if(e.KeyCode == Keys.Enter) { e.SuppressKeyPress=true; await LoadCategoriesAsync(); } };
        _status.SelectedIndexChanged += async (_,_) => await LoadCategoriesAsync();
        var preview = Controls.Find("btnPreview",true).First();
        preview.Click += (_,_) => { if(_grid?.CurrentRow?.DataBoundItem is CategoryDto row) UiDetails.Show(this,"Chi tiết danh mục",[("Tên danh mục",row.CategoryName),("Mô tả",row.Description ?? "—"),("Trạng thái",row.IsActive ? "Đang hoạt động" : "Ngừng hoạt động")]); };
        Shown += async (_, _) => await LoadCategoriesAsync();
    }

    // Tạo và sắp xếp các vùng giao diện của màn hình.
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
        var status = _status;
        status.Items.AddRange(["Tất cả trạng thái", "Đang hoạt động", "Ngừng hoạt động"]);
        status.SelectedIndex=0;
        status.Dock=DockStyle.Fill;
        status.TabIndex=1;
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
            add.AccessibleDescription = "Thêm danh mục";
            add.Click += async (_, _) =>
            {
                if (_services is null) return;
                using var detail = new FrmCategoryDetail(_services);
                if (detail.ShowDialog(this) == DialogResult.OK) await LoadCategoriesAsync();
            };
            actions.Controls.Add(add);
        }
        var preview = Unavailable("btnPreview", "Xem chi tiết", "eye");
        preview.AccessibleDescription = "Chọn một danh mục để xem chi tiết. Danh sách chưa được tải dữ liệu.";
        actions.Controls.Add(preview);
        if (_canManageCategories)
        {
            var edit = ProductUi.Button("btnEdit", "Sửa", "pencil");
            edit.Width = 100;
            edit.Enabled = false;
            edit.Click += async (_, _) => await EditSelectedAsync();
            actions.Controls.Add(edit);
            var deactivate = ProductUi.Button("btnDeactivate", "Ngừng hoạt động", "dash-circle");
            deactivate.Width = 190;
            deactivate.Enabled = false;
            deactivate.Click += async (_, _) => await ToggleSelectedAsync();
            actions.Controls.Add(deactivate);
        }
        toolbar.Controls.Add(actions, 0, 0);
        var refresh = Unavailable("btnRefresh", "", "arrow-clockwise");
        _refresh = refresh;
        if (_services is not null) { refresh.Enabled = true; refresh.Click += async (_, _) => await LoadCategoriesAsync(); }
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
        _grid = grid;
        grid.SelectionChanged += (_, _) => UpdateActions();
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
        grid.DefaultCellStyle.SelectionBackColor = AppColors.Lavender;
        grid.DefaultCellStyle.SelectionForeColor = AppColors.SelectedText;
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
        var note = ProductUi.Label("Đang tải danh mục…", 9);
        // [COLOR CONFIG]: thông tin trạng thái phụ.
        note.Name="lblCategoryState";
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, 3);
        Controls.Add(root);
    }

    // Tải danh sách danh mục theo bộ lọc và cập nhật bảng hiển thị.
    private async Task LoadCategoriesAsync()
    {
        if (_services is null || _grid is null || _loading) return;
        _loading=true;
        if (_refresh is not null) _refresh.Enabled=false;
        var note=Controls.Find("lblCategoryState",true).First();
        note.Text="Đang tải danh mục…";
        try
        {
            var rows = await _services.RunAsync(() => _services.Categories.GetAsync(_services.Session));
            var search=Controls.Find("txtSearch",true).OfType<AntdUI.Input>().Single().Text.Trim();
            var filtered=rows.Where(row=>(string.IsNullOrEmpty(search) || row.CategoryName.Contains(search,StringComparison.CurrentCultureIgnoreCase)) && (_status.SelectedIndex==0 || row.IsActive==(_status.SelectedIndex==1))).ToList();
            _grid.DataSource = filtered;
            note.Text=filtered.Count==0 ? "Không có danh mục phù hợp bộ lọc." : $"{filtered.Count:N0} danh mục";
            foreach (DataGridViewColumn column in _grid.Columns)
                column.DataPropertyName = column.Name == "Status" ? nameof(CategoryDto.IsActive) : column.Name;
            _grid.CellFormatting -= FormatStatus;
            _grid.CellFormatting += FormatStatus;
            UpdateActions();
        }
        catch (Exception ex) { note.Text=LiveUi.LoadError(ex); }
        finally { _loading=false; if(_refresh is not null && !IsDisposed) _refresh.Enabled=true; }
    }

    // Định dạng trạng thái hoạt động thành nội dung dễ đọc trong bảng.
    private void FormatStatus(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid is not null && _grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is bool active) e.Value = active ? "Đang hoạt động" : "Ngừng hoạt động";
    }

    // Cập nhật khả năng thao tác theo dòng đang chọn và quyền người dùng.
    private void UpdateActions()
    {
        var selected = _grid?.CurrentRow?.DataBoundItem is CategoryDto;
        foreach (Control control in Controls.Find("btnEdit", true).Concat(Controls.Find("btnDeactivate", true)).Concat(Controls.Find("btnPreview",true))) control.Enabled = selected;
    }

    // Mở form sửa đối tượng đang chọn và tải lại dữ liệu sau khi lưu.
    private async Task EditSelectedAsync()
    {
        if (_services is null || _grid?.CurrentRow?.DataBoundItem is not CategoryDto category) return;
        using var detail = new FrmCategoryDetail(_services, category);
        if (detail.ShowDialog(this) == DialogResult.OK) await LoadCategoriesAsync();
    }

    // Đổi trạng thái hoạt động của đối tượng đang chọn rồi tải lại danh sách.
    private async Task ToggleSelectedAsync()
    {
        if (_services is null || _grid?.CurrentRow?.DataBoundItem is not CategoryDto category) return;
        if(MessageBox.Show(this,"Thay đổi trạng thái danh mục đã chọn?","Xác nhận",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
        try { await _services.RunAsync(() => _services.Categories.SetActiveAsync(_services.Session, category.CategoryId, !category.IsActive)); await LoadCategoriesAsync(); }
        catch(Exception ex) { MessageBox.Show(this,LiveUi.Error(ex),"Không thể thay đổi trạng thái",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    }

    // Thông báo chức năng chưa được kết nối trong chế độ xem trước giao diện.
    private static AntdUI.Button Unavailable(string name, string text, string icon)
    {
        var button = ProductUi.Button(name, text, icon);
        button.Enabled = false;
        button.AccessibleDescription = "Chưa sẵn sàng: chức năng dữ liệu đang được hoàn thiện";
        return button;
    }

    // Tạo cột dữ liệu với tên, tiêu đề và cách hiển thị phù hợp.
    private static DataGridViewTextBoxColumn Column(string name, string caption, float weight) => new()
    {
        Name = name, HeaderText = caption, FillWeight = weight, MinimumWidth = 120,
        SortMode = DataGridViewColumnSortMode.NotSortable
        // Chưa gán DataPropertyName khi hợp đồng DTO chưa có.
    };
}
