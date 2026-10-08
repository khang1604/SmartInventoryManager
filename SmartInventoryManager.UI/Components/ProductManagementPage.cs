using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class ProductManagementPage : UserControl
{
    private readonly UiServices _services;
    private readonly TextBox _search = new() { Width = 260, AccessibleName = "Tìm mã hoặc tên sản phẩm" };
    private readonly ComboBox _status = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid;
    private readonly Label _message = ProductUi.Label("Đang tải dữ liệu…");
    private readonly Button _previous = new() { Text = "Trang trước", AutoSize = true };
    private readonly Button _next = new() { Text = "Trang sau", AutoSize = true };
    private int _pageNumber = 1;
    private const int PageSize = 50;
    private bool _busy;

    public ProductManagementPage(UiServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Dock = DockStyle.Fill;

        var root = ProductUi.Layout(1, 4);
        root.Padding = new Padding(20, 12, 20, 16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filters = LiveUi.Toolbar();
        filters.Controls.Add(LiveUi.Field("Tìm kiếm", _search));
        _status.Items.AddRange(["Tất cả trạng thái", "Đang kinh doanh", "Ngừng kinh doanh"]);
        _status.SelectedIndex = 0;
        filters.Controls.Add(LiveUi.Field("Trạng thái", _status));
        var refresh = ProductUi.Button("btnRefreshProducts", "Làm mới", "arrow-clockwise");
        refresh.Click += async (_, _) => await RefreshAsync();
        filters.Controls.Add(refresh);
        root.Controls.Add(filters, 0, 0);

        var actions = LiveUi.Toolbar();
        var add = ProductUi.Button("btnAddProduct", "Thêm sản phẩm", "plus-lg", true);
        add.Enabled = false;
        add.AccessibleDescription = "Chưa sẵn sàng: cần CategoryService để chọn danh mục.";
        actions.Controls.Add(add);
        var edit = ProductUi.Button("btnEditProduct", "Sửa", "pencil");
        edit.Enabled = false;
        edit.AccessibleDescription = "Chưa sẵn sàng: cần CategoryService để chọn danh mục.";
        actions.Controls.Add(edit);
        _previous.Enabled = false;
        _next.Enabled = false;
        _previous.Click += async (_, _) => { if (_pageNumber > 1) { _pageNumber--; await RefreshAsync(); } };
        _next.Click += async (_, _) => { _pageNumber++; await RefreshAsync(); };
        actions.Controls.Add(_previous);
        actions.Controls.Add(_next);
        root.Controls.Add(actions, 0, 1);

        _grid = LiveUi.Grid("dgvProducts", [
            ("ProductCode", "Mã sản phẩm"), ("ProductName", "Tên sản phẩm"),
            ("CategoryName", "Danh mục"), ("Unit", "Đơn vị"),
            ("PurchasePrice", "Giá nhập (đ)"), ("SellingPrice", "Giá bán (đ)"),
            ("ReorderPoint", "Điểm đặt hàng lại"), ("MaximumStock", "Tồn tối đa"),
            ("IsActive", "Trạng thái")]);
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "IsActive" && e.Value is bool active)
                e.Value = active ? "Đang kinh doanh" : "Ngừng kinh doanh";
        };
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { _pageNumber = 1; await RefreshAsync(); e.SuppressKeyPress = true; }
        };
        _status.SelectedIndexChanged += async (_, _) => { _pageNumber = 1; await RefreshAsync(); };
        root.Controls.Add(_message, 0, 2);
        root.Controls.Add(_grid, 0, 3);
        Controls.Add(root);
        Load += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        _busy = true;
        try
        {
            Enabled = false;
            var status = _status.SelectedIndex switch { 1 => true, 2 => false, _ => (bool?)null };
            var result = await _services.RunAsync(() => _services.ProductManagement.SearchAsync(
                _services.Session,
                new ProductSearchDto { Search = _search.Text, IsActive = status, PageNumber = _pageNumber, PageSize = PageSize }));
            _grid.DataSource = result.Items.ToList();
            _message.Text = result.TotalCount == 0
                ? "Không có sản phẩm phù hợp."
                : $"Trang {result.PageNumber} • {result.TotalCount:N0} sản phẩm";
            _previous.Enabled = result.PageNumber > 1;
            _next.Enabled = result.PageNumber * result.PageSize < result.TotalCount;
        }
        catch (Exception ex)
        {
            _message.Text = LiveUi.Error(ex);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) Enabled = true;
        }
    }
}