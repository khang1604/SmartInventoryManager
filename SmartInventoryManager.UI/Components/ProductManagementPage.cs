using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class ProductManagementPage : UserControl
{
    private readonly UiServices _services;
    private readonly TextBox _search = new() { Width = 260, AccessibleName = "Tìm mã hoặc tên sản phẩm" };
    private readonly ComboBox _status = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid;
    private readonly Label _message = ProductUi.Label("Đang tải dữ liệu…");
    private readonly AntdUI.Button _previous = ProductUi.Button("btnPreviousPage", "Trang trước", "chevron-left");
    private readonly AntdUI.Button _next = ProductUi.Button("btnNextPage", "Trang sau", "chevron-right");
    private int _pageNumber = 1;
    private int PageSize => UiPreferences.Current.PageSize;
    private bool _busy;

    public ProductManagementPage(UiServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Dock = DockStyle.Fill;

        var root = ProductUi.Layout(1, 5);
        root.Padding = new Padding(20, 12, 20, 16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var filters = LiveUi.Toolbar();
        filters.Controls.Add(LiveUi.Field("Tìm kiếm", _search));
        _status.Items.AddRange(["Tất cả trạng thái", "Đang kinh doanh", "Ngừng kinh doanh"]);
        _status.SelectedIndex = 0;
        filters.Controls.Add(LiveUi.Field("Trạng thái", _status));
        var refresh = ProductUi.Button("btnRefreshProducts", "Làm mới", "arrow-clockwise");
        refresh.Click += async (_, _) => await RefreshAsync();
        refresh.Height = 34; refresh.Margin = new Padding(0, 28, 10, 0);
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
        var paging = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 10, 0, 0), Margin = Padding.Empty };
        _previous.Size = _next.Size = new Size(130, 36);
        paging.Controls.Add(_next); paging.Controls.Add(_previous);
        root.Controls.Add(paging, 0, 4);
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
        var detail = new DataGridViewButtonColumn { Name = "ViewDetails", HeaderText = "Chi tiết", Text = "Xem chi tiết",
            UseColumnTextForButtonValue = true, MinimumWidth = 125, Width = 125, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
        _grid.Columns.Add(detail);
        UiDetails.DecorateDetailsColumn(_grid);
        _grid.CellContentClick += async (_, e) =>
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "ViewDetails"
                || _grid.Rows[e.RowIndex].DataBoundItem is not ProductDto selected) return;
            _busy = true; Enabled = false;
            try
            {
                var product = await _services.RunAsync(() => _services.ProductManagement.GetByIdAsync(_services.Session, selected.ProductId));
                if (product is null) { _message.Text = "Không tìm thấy sản phẩm. Vui lòng làm mới."; return; }
                UiDetails.Show(this, "Chi tiết sản phẩm", [
                    ("Mã sản phẩm", product.ProductCode), ("Tên sản phẩm", product.ProductName),
                    ("Danh mục", product.CategoryName), ("Đơn vị", product.Unit),
                    ("Giá nhập (đ)", product.PurchasePrice.ToString("N2", LiveUi.Culture)),
                    ("Giá bán (đ)", product.SellingPrice.ToString("N2", LiveUi.Culture)),
                    ("Điểm đặt hàng lại", product.ReorderPoint.ToString("N0", LiveUi.Culture)),
                    ("Tồn tối đa", product.MaximumStock.ToString("N0", LiveUi.Culture)),
                    ("Trạng thái", product.IsActive ? "Đang kinh doanh" : "Ngừng kinh doanh"),
                    ("Ngày tạo", product.CreatedAt.ToString("dd/MM/yyyy HH:mm", LiveUi.Culture))]);
            }
            catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
            finally { _busy = false; if (!IsDisposed) Enabled = true; }
        };
        if (_services.Authorization.CanManageProduct(_services.Session))
        {
            var active = ProductUi.Button("btnSetProductActive", "Đổi trạng thái", "pencil");
            active.Enabled = false;
            _grid.SelectionChanged += (_, _) => active.Enabled = _grid.CurrentRow?.DataBoundItem is ProductDto;
            active.Click += async (_, _) =>
            {
                if (_busy || _grid.CurrentRow?.DataBoundItem is not ProductDto product) return;
                var verb = product.IsActive ? "Ngừng kinh doanh" : "Kích hoạt";
                if (MessageBox.Show(FindForm(), $"{verb} sản phẩm {product.ProductCode}?", "Xác nhận",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
                _busy = true; Enabled = false;
                bool saved = false;
                try { await _services.RunAsync(() => _services.ProductManagement.SetActiveAsync(_services.Session, product.ProductId, !product.IsActive)); saved = true; }
                catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
                finally { _busy = false; if (!IsDisposed) Enabled = true; }
                if (saved && !IsDisposed)
                {
                    await RefreshAsync();
                    _message.Text = "Đã lưu trạng thái sản phẩm. " + _message.Text;
                }
            };
            actions.Controls.Add(active);
        }
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { _pageNumber = 1; await RefreshAsync(); e.SuppressKeyPress = true; }
        };
        _status.SelectedIndexChanged += async (_, _) => { _pageNumber = 1; await RefreshAsync(); };
        root.Controls.Add(_message, 0, 2);
        root.Controls.Add(LiveUi.TableFrame(_grid), 0, 3);
        Controls.Add(root);
        Load += async (_, _) => await RefreshAsync();
    }

    public async Task ApplyPreferencesAsync()
    {
        _pageNumber = 1;
        await RefreshAsync();
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
            _message.Text = LiveUi.LoadError(ex);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) Enabled = true;
        }
    }
}
