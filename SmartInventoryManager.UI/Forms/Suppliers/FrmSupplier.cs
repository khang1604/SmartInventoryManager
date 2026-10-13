using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Suppliers;

public partial class FrmSupplier : Form
{
    public FrmSupplier(UiServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(ConnectedPages.Suppliers(services));
    }
    private readonly bool _canManageSuppliers;

    public FrmSupplier() : this(false) { }

    public FrmSupplier(bool canManageSuppliers)
    {
        _canManageSuppliers = canManageSuppliers;
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "Nhà cung cấp";
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
        var search = ProductUi.Input("txtSearch", "Nhập mã, tên hoặc số điện thoại");
        search.PrefixSvg = MainIcons.Get("search");
        search.ReadOnly = false;
        search.TabIndex = 0;
        search.AccessibleName = "Tìm mã, tên hoặc số điện thoại nhà cung cấp";
        search.AccessibleDescription = "Tìm kiếm chưa sẵn sàng khi chưa có dữ liệu";
        var status = ProductUi.Input("cboStatus");
        status.Text = "Tất cả trạng thái";
        status.SuffixSvg = MainIcons.Get("chevron-down");
        status.ReadOnly = false;
        status.TabIndex = 1;
        status.AccessibleName = "Lọc trạng thái nhà cung cấp";
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
        if (_canManageSuppliers)
        {
            var add = ProductUi.Button("btnAdd", "Thêm nhà cung cấp", "plus-lg", true);
            add.Width = 205;
            add.TabIndex = 2;
            add.AccessibleDescription = "Mở biểu mẫu xem trước, chưa lưu nhà cung cấp";
            add.Click += async (_, _) =>
            {
                if (_services is null) return;
                using var detail = new FrmSupplierDetail(_services, null);
                if (detail.ShowDialog(this) == DialogResult.OK) await LoadSuppliersAsync();
            };
            actions.Controls.Add(add);
        }
        var preview = Unavailable("btnPreview", "Xem chi tiết", "eye");
        preview.AccessibleDescription = "Chọn một nhà cung cấp để xem chi tiết. Danh sách chưa được tải dữ liệu.";
        actions.Controls.Add(preview);
        if (_canManageSuppliers)
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
        if (_services is not null) { refresh.Enabled = true; refresh.Click += async (_, _) => await LoadSuppliersAsync(); }
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = Padding.Empty;
        refresh.AccessibleName = "Làm mới nhà cung cấp";
        toolbar.Controls.Add(refresh, 1, 0);
        root.Controls.Add(toolbar, 0, 1);

        var tableArea = ProductUi.Layout(1, 2);
        tableArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        tableArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng cho danh sách.
        tableArea.BackColor = AppColors.Surface;
        var grid = new DataGridView
        {
            Name = "dgvSuppliers", Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 34 },
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, Margin = Padding.Empty,
            AccessibleName = "Danh sách nhà cung cấp", TabIndex = 6,
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
        grid.Columns.AddRange(Column("SupplierCode", "Mã nhà cung cấp", 16),
            Column("SupplierName", "Tên nhà cung cấp", 24), Column("Phone", "Số điện thoại", 15),
            Column("Email", "Email", 20), Column("TaxCode", "Mã số thuế", 13),
            Column("Status", "Trạng thái", 12));
        UiDetails.AddColumn(grid, insetButton: true);
        tableArea.Controls.Add(grid, 0, 0);
        var empty = ProductUi.Layout(1, 5);
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var symbol = ProductUi.Button("emptyIcon", "", "people");
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
        var note = ProductUi.Label("Chưa kết nối dữ liệu • Biểu mẫu xem trước chưa lưu nhà cung cấp", 9);
        // [COLOR CONFIG]: thông tin trạng thái phụ.
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, 3);
        Controls.Add(root);
    }

    private async Task LoadSuppliersAsync()
    {
        if (_services is null || _grid is null) return;
        try
        {
            var rows = await _services.RunAsync(() => _services.Suppliers.GetAsync(_services.Session));
            _grid.DataSource = rows.ToList();
            foreach (DataGridViewColumn column in _grid.Columns)
                column.DataPropertyName = column.Name == "Status" ? nameof(SupplierDto.IsActive) : column.Name;
            _grid.CellFormatting -= FormatStatus;
            _grid.CellFormatting += FormatStatus;
            UpdateActions();
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể tải nhà cung cấp", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void FormatStatus(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid is not null && _grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is bool active) e.Value = active ? "Đang hoạt động" : "Ngừng hoạt động";
    }

    private void UpdateActions()
    {
        var selected = _grid?.CurrentRow?.DataBoundItem is SupplierDto;
        foreach (Control control in Controls.Find("btnEdit", true).Concat(Controls.Find("btnDeactivate", true))) control.Enabled = selected;
    }

    private async Task EditSelectedAsync()
    {
        if (_services is null || _grid?.CurrentRow?.DataBoundItem is not SupplierDto supplier) return;
        using var detail = new FrmSupplierDetail(_services, supplier);
        if (detail.ShowDialog(this) == DialogResult.OK) await LoadSuppliersAsync();
    }

    private async Task ToggleSelectedAsync()
    {
        if (_services is null || _grid?.CurrentRow?.DataBoundItem is not SupplierDto supplier) return;
        await _services.RunAsync(() => _services.Suppliers.SetActiveAsync(_services.Session, supplier.SupplierId, !supplier.IsActive));
        await LoadSuppliersAsync();
    }

    private readonly UiServices? _services;
    private DataGridView? _grid;

    private static AntdUI.Button Unavailable(string name, string text, string icon)
    {
        var button = ProductUi.Button(name, text, icon);
        button.Click += (_, _) => ActionFeedback.Show(button);
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
