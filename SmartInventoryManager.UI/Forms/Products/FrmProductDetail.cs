using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Products;

public partial class FrmProductDetail : Form
{
    private readonly UiServices? _services;
    private readonly int? _productId;

    public FrmProductDetail()
    {
        InitializeComponent();
        BuildLayout();
    }

    public FrmProductDetail(UiServices services, int? productId = null) : this()
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _productId = productId;
        FindControl<AntdUI.Button>("btnSave").Enabled = true;
        FindControl<CheckBox>("chkIsActive").Enabled = false;
        FindControl<CheckBox>("chkIsActive").AccessibleDescription = "Thay đổi trạng thái từ danh sách sản phẩm";
        Shown += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        Text = "VioWare - Thông tin sản phẩm";
        Font = new Font("Segoe UI", 10F);
        ClientSize = new Size(600, 690);
        MinimumSize = new Size(520, 570);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        // [COLOR CONFIG]: nền và chữ của biểu mẫu theo màu đã duyệt.
        BackColor = AppColors.Workspace;
        ForeColor = AppColors.Text;
        var root = ProductUi.Layout(1, 3);
        root.Padding = new Padding(20, 14, 20, 12);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var heading = ProductUi.Layout(1, 2);
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(ProductUi.Label("Thông tin sản phẩm", 18, true), 0, 0);
        var instruction = ProductUi.Label("Các trường có dấu * là bắt buộc", 10);
        // [COLOR CONFIG]: hướng dẫn nhẹ hơn tiêu đề.
        instruction.ForeColor = AppColors.Muted;
        heading.Controls.Add(instruction, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        var sections = ProductUi.Layout(1, 3);
        sections.Dock = DockStyle.Top;
        sections.Height = 480;
        sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 216));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 216));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var general = Section("Thông tin chung");
        var category = LiveUi.Choice("cboCategory", "Danh mục sản phẩm"); category.Dock=DockStyle.Fill;
        AddField(general, 0, 0, "Mã sản phẩm *", ProductUi.Input("txtProductCode"), 0);
        AddField(general, 1, 0, "Tên sản phẩm *", ProductUi.Input("txtProductName"), 1);
        AddField(general, 0, 1, "Danh mục *", category, 2);
        AddField(general, 1, 1, "Đơn vị tính *", ProductUi.Input("txtUnit"), 3);
        sections.Controls.Add(general.Parent!, 0, 0);
        var pricing = Section("Giá và ngưỡng tồn kho");
        AddField(pricing, 0, 0, "Giá nhập (đ) *", Number("numPurchasePrice", 2), 4);
        AddField(pricing, 1, 0, "Giá bán (đ) *", Number("numSellingPrice", 2), 5);
        var reorderPoint = Number("numReorderPoint", 0); reorderPoint.Minimum = 1; reorderPoint.Value = 1;
        var maximumStock = Number("numMaximumStock", 0); maximumStock.Minimum = 2; maximumStock.Value = 2;
        AddField(pricing, 0, 1, "Điểm đặt hàng lại *", reorderPoint, 6);
        AddField(pricing, 1, 1, "Tồn tối đa *", maximumStock, 7);
        sections.Controls.Add(pricing.Parent!, 0, 1);
        var active = new CheckBox
        {
            Name = "chkIsActive", Text = "Đang kinh doanh", Checked = true,
            Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0), TabIndex = 8,
            // [COLOR CONFIG]: vùng trạng thái xanh mint.
            BackColor = AppColors.Mint,
            // [COLOR CONFIG]: chữ tối trên nền mint.
            ForeColor = AppColors.Text
        };
        sections.Controls.Add(active, 0, 2);
        scroll.Controls.Add(sections);
        root.Controls.Add(scroll, 0, 1);
        var footer = ProductUi.Layout(2, 1);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.Padding = new Padding(0, 10, 0, 0);
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
        var note = ProductUi.Label("Các trường có dấu * là bắt buộc", 8.5F);
        // [COLOR CONFIG]: ghi chú phụ rõ ràng về trạng thái biểu mẫu.
        note.ForeColor = AppColors.Muted;
        footer.Controls.Add(note, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Width = 90;
        close.TabIndex = 9;
        close.DialogResult = DialogResult.Cancel;
        var save = ProductUi.Button("btnSave", "Lưu sản phẩm", primary: true);
        save.Width = 145;
        save.Margin = Padding.Empty;
        save.Enabled = _services is not null;
        save.TabIndex = 10;
        save.AccessibleDescription = "Lưu thông tin sản phẩm qua Service";
        save.Click += async (_, _) => await SaveAsync();
        actions.Controls.Add(close);
        actions.Controls.Add(save);
        CancelButton = close;
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private async Task LoadDataAsync()
    {
        if (_services is null) return;
        try
        {
            var categories = await _services.RunAsync(() => _services.Categories.GetActiveAsync(_services.Session));
            var category = FindControl<ComboBox>("cboCategory");
            category.DisplayMember=nameof(LookupItemDto.Name); category.ValueMember=nameof(LookupItemDto.Id); category.DataSource=new[]{new LookupItemDto(0,"","Chọn danh mục")}.Concat(categories).ToList(); category.SelectedIndex=0;
            if (_productId is null) return;
            var product = await _services.RunAsync(() => _services.ProductManagement.GetByIdAsync(_services.Session, _productId.Value));
            if (product is null) throw new KeyNotFoundException("Không tìm thấy sản phẩm.");
            FindControl<AntdUI.Input>("txtProductCode").Text = product.ProductCode;
            FindControl<AntdUI.Input>("txtProductName").Text = product.ProductName;
            category.SelectedValue = product.CategoryId;
            FindControl<AntdUI.Input>("txtUnit").Text = product.Unit;
            FindControl<AntdUI.InputNumber>("numPurchasePrice").Value = product.PurchasePrice;
            FindControl<AntdUI.InputNumber>("numSellingPrice").Value = product.SellingPrice;
            FindControl<AntdUI.InputNumber>("numReorderPoint").Value = product.ReorderPoint;
            FindControl<AntdUI.InputNumber>("numMaximumStock").Value = product.MaximumStock;
            FindControl<CheckBox>("chkIsActive").Checked = product.IsActive;
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private bool _saving;

    private async Task SaveAsync()
    {
        if (_services is null || _saving) return;
        _saving = true;
        var save = Controls.Find("btnSave", true).First();
        save.Enabled = false;
        try
        {
            var selectedCategory = FindControl<ComboBox>("cboCategory").SelectedItem as LookupItemDto;
            var categories = await _services.RunAsync(() => _services.Categories.GetActiveAsync(_services.Session));
            var category = categories.FirstOrDefault(item => item.Id == selectedCategory?.Id);
            if (category is null) { MessageBox.Show(this,"Vui lòng chọn danh mục sản phẩm.","Kiểm tra thông tin",MessageBoxButtons.OK,MessageBoxIcon.Warning); FindControl<ComboBox>("cboCategory").Focus(); return; }
            var request = new ProductRequestDto
            {
                ProductCode = FindControl<AntdUI.Input>("txtProductCode").Text,
                ProductName = FindControl<AntdUI.Input>("txtProductName").Text,
                CategoryId = category.Id,
                Unit = FindControl<AntdUI.Input>("txtUnit").Text,
                PurchasePrice = Convert.ToDecimal(FindControl<AntdUI.InputNumber>("numPurchasePrice").Value),
                SellingPrice = Convert.ToDecimal(FindControl<AntdUI.InputNumber>("numSellingPrice").Value),
                ReorderPoint = Convert.ToInt32(FindControl<AntdUI.InputNumber>("numReorderPoint").Value),
                MaximumStock = Convert.ToInt32(FindControl<AntdUI.InputNumber>("numMaximumStock").Value)
            };
            await _services.RunAsync(() => _productId.HasValue
                ? _services.ProductManagement.UpdateAsync(_services.Session, _productId.Value, request)
                : _services.ProductManagement.CreateAsync(_services.Session, request));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _saving = false; if (!IsDisposed) save.Enabled = true; }
    }

    private T FindControl<T>(string name) where T : Control
    {
        return Controls.Find(name, true).OfType<T>().FirstOrDefault() ?? throw new InvalidOperationException($"Không tìm thấy control {name}.");
    }

    private static TableLayoutPanel Section(string caption)
    {
        var section = ProductUi.Layout(1, 2);
        section.Margin = new Padding(0, 0, 0, 16);
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng của nhóm trường.
        section.BackColor = AppColors.Surface;
        var title = ProductUi.Label(caption, 12, true);
        title.Padding = new Padding(16, 0, 0, 0);
        // [COLOR CONFIG]: header nhóm lavender nhạt như mẫu.
        title.BackColor = AppColors.Workspace;
        section.Controls.Add(title, 0, 0);
        var fields = ProductUi.Layout(2, 4);
        fields.Padding = new Padding(16, 8, 4, 10);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        section.Controls.Add(fields, 0, 1);
        return fields;
    }

    private static AntdUI.InputNumber Number(string name, int decimals) => new()
    {
        Name = name, DecimalPlaces = decimals, ThousandsSeparator = true,
        Minimum = 0, Maximum = decimals == 0 ? int.MaxValue : 9999999999999999.99m,
        Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Radius = 6,
        // [COLOR CONFIG]: viền ô số giống các ô nhập văn bản.
        BorderColor = AppColors.Border, BorderActive = AppColors.Primary,
        // [COLOR CONFIG]: trường số trắng, chữ chính tối.
        BackColor = AppColors.Surface, ForeColor = AppColors.Text
    };

    private static void AddField(TableLayoutPanel table, int column, int row, string caption, Control input, int tabIndex)
    {
        table.Controls.Add(ProductUi.Label(caption, 10, true), column, row * 2);
        input.TabIndex = tabIndex;
        input.AccessibleName = caption.Replace(" *", "");
        table.Controls.Add(input, column, row * 2 + 1);
    }
}
