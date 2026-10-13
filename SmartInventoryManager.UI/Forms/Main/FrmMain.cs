using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.Authorization;

namespace SmartInventoryManager.UI.Forms.Main;

public partial class FrmMain : Form
{
    private readonly AuthorizationService? _authorization;
    private readonly UserSession? _session;
    private readonly UiServices? _services;
    public bool LogoutRequested { get; private set; }
    private readonly Dictionary<string, MainNavigationButton> _navigation = new();
    private readonly Dictionary<string, Control> _pages = new();
    private TableLayoutPanel _root = null!;
    private TableLayoutPanel _brand = null!;
    private Label _brandText = null!;
    private FlowLayoutPanel _menu = null!;
    private MainNavigationButton _logout = null!;
    private MainNavigationButton _sidebarToggle = null!;
    private bool _collapsed;
    private bool _resizingMenu;
    private readonly ToolTip _toolTips = new();
    private Action<bool> _setMenuCollapsed = null!;
    private AccountPopover? _accountPopover;
    private readonly Dictionary<Control, float> _originalFontSizes = new();
    private readonly List<Font> _preferenceFonts = new();
    private readonly Panel _content = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label _heading = new();
    private readonly Label _breadcrumb = new();

    public FrmMain()
    {
        InitializeComponent();
        BuildLayout();
    }

    public FrmMain(AuthenticatedUserDto user, AuthorizationService authorization, UserSession session, UiServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _services = services;
        InitializeComponent();
        BuildLayout();
        _account.Text = user.FullName;
        var role = user.RoleName switch
        {
            RoleNames.Admin => "Quản trị viên",
            RoleNames.WarehouseManager => "Quản lý kho",
            RoleNames.Staff => "Nhân viên kho",
            _ => "Tài khoản"
        };
        _account.AccessibleDescription = $"{user.FullName} • {role}";
        _account.Text = role;
        _logout.Text = role;
        _logout.AccessibleName = $"{role} - Mở menu tài khoản";
        _toolTips.SetToolTip(_account, _account.AccessibleDescription);
    }

    private readonly Label _account = new();

    private void BuildLayout()
    {
        FormClosed += (_, _) =>
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            _accountPopover?.Dispose();
            foreach (var font in _preferenceFonts) font.Dispose();
            _toolTips.Dispose();
        };
        FormClosing += (_, e) =>
        {
            if (_services?.HasPendingOperations != true) return;
            e.Cancel = true;
            LogoutRequested = false;
            MessageBox.Show(this, "Đang xử lý dữ liệu. Vui lòng chờ hoàn tất rồi đóng hoặc đăng xuất.",
                "Đang xử lý", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        UiBrand.ApplyIcon(this);
        SuspendLayout();
        Text = $"{UiBrand.ApplicationName} - Quản lý kho";
        ClientSize = new Size(1320, 780);
        MinimumSize = new Size(1024, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        // [COLOR CONFIG]: nền pastel sáng, chữ tối theo bảng màu KH duyệt.
        BackColor = AppColors.Workspace;
        // [COLOR CONFIG]: chữ chính #2D3142 hoặc màu hệ thống High Contrast.
        ForeColor = AppColors.Text;

        var root = _root = CreateLayout(2, 2);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.Controls.Add(BuildSidebar(), 0, 0);
        var workspace = CreateLayout(1, 2);
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildHeader(), 0, 0);
        _content.Padding = new Padding(20, 0, 20, 16);
        workspace.Controls.Add(_content, 0, 1);
        root.Controls.Add(workspace, 1, 0);
        var status = TextLabel($"{UiBrand.ApplicationName} • {UiBrand.ProjectName} • Dữ liệu theo quyền tài khoản", 8.5F);
        status.Padding = new Padding(12, 0, 0, 0);
        if(_services is not null && _session is not null && _authorization?.IsStaff(_session)==true)
            Shown += async (_,_) => {
                try { var warehouses=await _services.RunAsync(()=>_services.Warehouses.GetActiveAsync(_session)); var current=warehouses.FirstOrDefault(row=>row.Id==_session.CurrentWarehouseId); if(!IsDisposed) status.Text=$"{UiBrand.ApplicationName} • Kho làm việc: {current?.Name ?? "Chưa chọn kho"}"; }
                catch(Exception ex) { if(!IsDisposed) status.Text=LiveUi.LoadError(ex); }
            };
        root.Controls.Add(status, 0, 1);
        root.SetColumnSpan(status, 2);
        Controls.Add(root);
        Disposed += (_, _) => _toolTips.Dispose();
        ShowPage("Tổng quan");
        _setMenuCollapsed(UiPreferences.Current.CollapsedMenu);
        ApplyFontPreferences();
        ResumeLayout(true);
    }

    private Control BuildSidebar()
    {
        var sidebar = CreateLayout(1, 3);
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.Padding = new Padding(8, 12, 8, 12);
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        var brand = _brand = CreateLayout(2, 1);
        brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var logo = UiBrand.CreateLogo();
        logo.Dock = DockStyle.Fill;
        logo.TabStop = false;
        logo.AccessibleName = "Logo VioWare";
        brand.Controls.Add(logo, 0, 0);
        _brandText = TextLabel($"{UiBrand.ApplicationName}\nQuản lý kho", 14.5F, true);
        _brandText.AccessibleDescription = UiBrand.ProjectName;
        brand.Controls.Add(_brandText, 1, 0);
        sidebar.Controls.Add(brand, 0, 0);
        var toggle = _sidebarToggle = new MainNavigationButton("", "chevron-left") { Collapsed = true };
        toggle.Dock = DockStyle.Fill;
        toggle.AccessibleName = "Thu gọn hoặc mở rộng menu";
        _setMenuCollapsed = collapsed =>
        {
            _collapsed = collapsed;
            _root.ColumnStyles[0].Width = (_collapsed ? 76 : 205) * DeviceDpi / 96F;
            _brandText.Visible = !_collapsed;
            _account.Visible = !_collapsed;
            _brand.ColumnStyles[0].SizeType = _collapsed ? SizeType.Percent : SizeType.Absolute;
            _brand.ColumnStyles[0].Width = _collapsed ? 100 : 60 * DeviceDpi / 96F;
            _brand.ColumnStyles[1].SizeType = _collapsed ? SizeType.Absolute : SizeType.Percent;
            _brand.ColumnStyles[1].Width = _collapsed ? 0 : 100;
            toggle.SetIcon(_collapsed ? "chevron-right" : "chevron-left");
            _logout.Dock = DockStyle.Fill;
            _logout.Visible = true;
            _toolTips.SetToolTip(toggle, _collapsed ? "Mở rộng menu" : "Thu gọn menu");
            foreach (var button in _navigation.Values.Append(_logout))
            {
                button.Collapsed = _collapsed;
                button.Invalidate();
            }
            _root.PerformLayout();
            _brand.PerformLayout();
            _logout.Parent?.PerformLayout();
            _logout.Invalidate();
            ResizeMenu();
            _menu.PerformLayout();
            _menu.Refresh();
            Refresh();
        };
        toggle.Click += (_, _) => _setMenuCollapsed(!_collapsed);
        _toolTips.SetToolTip(toggle, "Thu gọn menu");
        var menu = _menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Margin = Padding.Empty
        };
        (string title, string icon)[] pages =
        [
            ("Tổng quan", "house"), ("Sản phẩm", "box-seam"), ("Danh mục", "grid"),
            ("Nhà cung cấp", "supplier"), ("Kho & vị trí", "building"),
            ("Nhập kho", "box-arrow-in-down"), ("Xuất kho", "box-arrow-up"),
            ("Tồn kho", "boxes"), ("Lịch sử tồn kho", "file-earmark-text"),
            ("Cảnh báo", "bell"), ("Báo cáo", "file-earmark-bar-graph"), ("Người dùng", "people")
        ];
        foreach (var (title, icon) in pages)
        {
            if (title == "Người dùng" && !CanOpenPage(title)) continue;
            var button = new MainNavigationButton(title, icon);
            button.Size = new Size(175, 43);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.TabIndex = _navigation.Count;
            button.Click += (_, _) => ShowPage(title);
            _navigation.Add(title, button);
            _toolTips.SetToolTip(button, title);
            menu.Controls.Add(button);
        }
        sidebar.Controls.Add(menu, 0, 1);
        menu.Layout += (_, _) => ResizeMenu();
        menu.SizeChanged += (_, _) =>
        {
            // Chừa chỗ cho thanh cuộn dọc để không phát sinh thanh cuộn ngang khi cửa sổ nhỏ.
            ResizeMenu();
        };
        var footer = CreateLayout(1, 1);
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.Padding = new Padding(0, 12, 0, 12);
        _account.Text = "Tài khoản";
        var accountButton = _logout = new MainNavigationButton("Tài khoản", "person")
        {
            Selected = true, CornerRadius = 12, UseAvatar = true, ShowChevron = true, Dock = DockStyle.Fill
        };
        accountButton.AccessibleName = "Mở menu tài khoản";
        footer.Controls.Add(accountButton, 0, 0);
        _toolTips.SetToolTip(accountButton, "Tài khoản và cài đặt");
        accountButton.Click += (_, _) =>
        {
            if (_accountPopover?.Visible == true) { _accountPopover.Close(); return; }
            _accountPopover?.Dispose();
            _accountPopover = new AccountPopover(_account.Text, Math.Max(189, accountButton.Width), OpenApplicationSettings,
                () => { using var dialog = new SmartInventoryManager.UI.Forms.Settings.FrmAccountSettings(_session?.CurrentUser, _account.Text, _services); dialog.ShowDialog(this); },
                () =>
                {
                    if (MessageBox.Show(this, "Đăng xuất và trở về màn hình đăng nhập?", "Đăng xuất", MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
                    LogoutRequested = true; Close();
                });
            _accountPopover.Closing += (_, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.AppClicked
                    && accountButton.RectangleToScreen(accountButton.ClientRectangle).Contains(Cursor.Position))
                    e.Cancel = true;
            };
            _accountPopover.Closed += (_, _) => { accountButton.ChevronExpanded = false; accountButton.Invalidate(); };
            accountButton.ChevronExpanded = true; accountButton.Invalidate();
            _accountPopover.Show(accountButton, new Point(0, -_accountPopover.Height - 6));
        };
        sidebar.Controls.Add(footer, 0, 2);
        return sidebar;
    }

    private async void OpenApplicationSettings()
    {
        using var dialog = new SmartInventoryManager.UI.Forms.Settings.FrmApplicationSettings();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _setMenuCollapsed(UiPreferences.Current.CollapsedMenu);
        ApplyFontPreferences();
        foreach (var page in _pages.Values)
            foreach (var product in Descendants(page).OfType<ProductManagementPage>())
                await product.ApplyPreferencesAsync();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
            foreach (var nested in Descendants(child)) yield return nested;
    }

    private void ApplyFontPreferences()
    {
        var controls = Descendants(this).Concat(_pages.Values.SelectMany(Descendants)).Distinct().Where(c => !c.IsDisposed).ToArray();
        foreach (var control in controls)
        {
            if (_originalFontSizes.ContainsKey(control)) continue;
            float size = control.Font.Size;
            if (System.ComponentModel.TypeDescriptor.GetProperties(control)["Font"]?.ShouldSerializeValue(control) == false
                && control.Parent is not null && _originalFontSizes.TryGetValue(control.Parent, out var parentSize)) size = parentSize;
            _originalFontSizes.Add(control, size);
        }
        foreach (var control in controls)
        {
            float size = _originalFontSizes[control];
            float target = size * UiPreferences.Current.FontSize / 10F;
            if (Math.Abs(control.Font.Size - target) < .01F) continue;
            var font = new Font(control.Font.FontFamily, target, control.Font.Style);
            _preferenceFonts.Add(font); control.Font = font;
        }
    }

    private void ResizeMenu()
    {
        if (_resizingMenu) return;
        _resizingMenu = true;
        try
        {
            int itemsHeight = _navigation.Values.Sum(button => button.Height + button.Margin.Vertical);
            int available = _menu.ClientSize.Width - (itemsHeight > _menu.ClientSize.Height ? SystemInformation.VerticalScrollBarWidth : 0) - 4;
            foreach (var button in _navigation.Values)
                button.Width = _collapsed ? Math.Max(32, Math.Min(available, (int)(52 * DeviceDpi / 96F)))
                    : Math.Max(40, available);
        }
        finally { _resizingMenu = false; }
    }

    private Control BuildHeader()
    {
        var header = CreateLayout(3, 1);
        header.Padding = new Padding(20, 12, 20, 8);
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        header.Controls.Add(_sidebarToggle, 0, 0);
        var titles = CreateLayout(1, 2);
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _heading.Dock = DockStyle.Fill;
        _heading.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _breadcrumb.Dock = DockStyle.Fill;
        // [COLOR CONFIG]: chữ phụ #626780 cho đường dẫn trang.
        _breadcrumb.ForeColor = AppColors.Muted;
        titles.Controls.Add(_heading, 0, 0);
        titles.Controls.Add(_breadcrumb, 0, 1);
        header.Controls.Add(titles, 1, 0);
        var notifications = ActionButton("Thông báo", "bell");
        notifications.Dock = DockStyle.Fill;
        notifications.Name = "btnNotifications";
        notifications.Click += (_, _) => ShowPage("Thông báo");
        header.Controls.Add(notifications, 2, 0);
        return header;
    }

    private void ShowPage(string title)
    {
        _heading.UseMnemonic = false;
        _breadcrumb.UseMnemonic = false;
        if (!CanOpenPage(title))
        {
            MessageBox.Show(this, "Bạn không có quyền mở chức năng này.", "Quyền truy cập",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _heading.Text = title;
        _breadcrumb.Text = $"Trang chủ / {title}";
        foreach (var (name, button) in _navigation)
        {
            // [COLOR CONFIG]: lavender #B5BAFF cho trang được chọn, nền sáng cho trang khác.
            button.Selected = name == title;
            // [COLOR CONFIG]: chữ tương phản trên mục chọn, không chỉ dùng màu để chỉ trạng thái.
            button.ForeColor = name == title ? AppColors.SelectedText : AppColors.Text;
            button.AccessibleDescription = name == title ? "Trang đang chọn" : $"Mở trang {name}";
            button.Invalidate();
        }
        foreach (Control child in _content.Controls.Cast<Control>().ToArray())
        {
            _content.Controls.Remove(child);
            child.Hide();
        }
        if (!_pages.TryGetValue(title, out var page))
        {
            page = CreatePage(title);
            _pages.Add(title, page);
        }
        _content.Controls.Add(page);
        page.Show();
        ApplyFontPreferences();
    }

    private bool CanOpenPage(string title)
    {
        // Constructor không tham số phục vụ Designer, không mở phiên nghiệp vụ.
        if (_authorization is null || _session is null) return title != "Người dùng";
        return title == "Người dùng"
            ? _authorization.CanManageUser(_session)
            : _authorization.CanView(_session);
    }

    private Control CreatePage(string title) => title switch
    {
        "Tổng quan" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Dashboard.FrmDashboard(_services), title),
        "Tổng quan" => PrepareForm(new SmartInventoryManager.Forms.Dashboard.FrmDashboard(), title),
        "Sản phẩm" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Products.FrmProductList(_services), title),
        "Nhà cung cấp" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Suppliers.FrmSupplier(_services), title),
        "Kho & vị trí" when _services is not null => CreateTabs(
            ("Kho", new SmartInventoryManager.Forms.Warehouses.FrmWarehouse(_services)),
            ("Vị trí", new SmartInventoryManager.Forms.Warehouses.FrmLocation(_services))),
        "Tồn kho" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Inventory.FrmInventory(_services), title),
        "Lịch sử tồn kho" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Inventory.FrmInventoryHistory(_services), title),
        "Cảnh báo" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Alerts.FrmStockAlert(_services), title),
        "Sản phẩm" => PrepareForm(new SmartInventoryManager.Forms.Products.FrmProductList(
            _authorization is not null && _session is not null && _authorization.CanManageProduct(_session)), title),
        "Danh mục" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Categories.FrmCategory(_services), title),
        "Danh mục" => PrepareForm(new SmartInventoryManager.Forms.Categories.FrmCategory(
            _authorization is not null && _session is not null && _authorization.CanManageCategory(_session)), title),
        "Nhà cung cấp" => PrepareForm(new SmartInventoryManager.Forms.Suppliers.FrmSupplier(
            _authorization is not null && _session is not null && _authorization.CanManageSupplier(_session)), title),
        "Kho & vị trí" => CreateTabs(
            ("Kho", new SmartInventoryManager.Forms.Warehouses.FrmWarehouse(
                _authorization is not null && _session is not null && _authorization.CanManageWarehouse(_session))),
            ("Vị trí", new SmartInventoryManager.Forms.Warehouses.FrmLocation())),
        "Nhập kho" => PrepareForm(_services is null ? new SmartInventoryManager.Forms.Imports.FrmImportList() : new SmartInventoryManager.Forms.Imports.FrmImportList(_services), title),
        "Xuất kho" => PrepareForm(_services is null ? new SmartInventoryManager.Forms.Exports.FrmExportList() : new SmartInventoryManager.Forms.Exports.FrmExportList(_services), title),
        "Tồn kho" => PrepareForm(new SmartInventoryManager.Forms.Inventory.FrmInventory(), title),
        "Lịch sử tồn kho" => PrepareForm(new SmartInventoryManager.Forms.Inventory.FrmInventoryHistory(), title),
        "Cảnh báo" => PrepareForm(new SmartInventoryManager.Forms.Alerts.FrmStockAlert(), title),
        "Báo cáo" when _services is not null => CreateTabs(
            ("Tồn kho", new SmartInventoryManager.Forms.Reports.FrmInventoryReport(_services)),
            ("Nhập / xuất", new SmartInventoryManager.Forms.Reports.FrmImportExportReport(_services))),
        "Báo cáo" => CreateTabs(
            ("Tồn kho", new SmartInventoryManager.Forms.Reports.FrmInventoryReport()),
            ("Nhập / xuất", new SmartInventoryManager.Forms.Reports.FrmImportExportReport())),
        "Người dùng" when _services is not null => PrepareForm(new SmartInventoryManager.Forms.Users.FrmUserManagement(_services), title),
        "Thông báo" when _services is not null => new NotificationPanel(_services),
        "Thông báo" => new NotificationPanel(),
        _ => throw new ArgumentOutOfRangeException(nameof(title))
    };

    private Form PrepareForm(Form form, string title, Control? content = null)
    {
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.Text = title;
        form.AccessibleName = title;
        form.Font = Font;
        // [COLOR CONFIG]: nền trang đồng nhất vùng làm việc.
        form.BackColor = AppColors.Workspace;
        if (content is not null) form.Controls.Add(content);
        else if (form.Controls.Count == 0)
            form.Controls.Add(EmptyPanel(title,
                "Màn hình đang được hoàn thiện. Chưa có chức năng thao tác dữ liệu.", "file-earmark-text"));
        form.Show();
        return form;
    }

    private Control CreateTabs(params (string Title, Form Form)[] pages)
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (var (title, form) in pages)
        {
            var page = new TabPage(title);
            page.Controls.Add(PrepareForm(form, title));
            tabs.TabPages.Add(page);
        }
        return tabs;
    }

    private static Control EmptyPanel(string title, string message, string icon)
    {
        var panel = CreateLayout(1, 3);
        panel.Margin = new Padding(5);
        panel.Padding = new Padding(14);
        // [COLOR CONFIG]: trắng cho panel nội dung chưa sẵn sàng.
        panel.BackColor = AppColors.Surface;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        panel.Controls.Add(TextLabel(title, 12F, true), 0, 0);
        var symbol = ActionButton("", icon);
        symbol.Dock = DockStyle.Fill;
        symbol.IconSize = new Size(44, 44);
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        // [COLOR CONFIG]: nền trắng cho icon trạng thái rỗng, đồng nhất panel nội dung.
        symbol.DefaultBack = AppColors.Surface;
        // [COLOR CONFIG]: giữ nền trắng khi rê chuột trên icon chỉ dùng để minh họa.
        symbol.BackHover = AppColors.Surface;
        // [COLOR CONFIG]: giữ nền trắng khi nhấn icon minh họa không có hành động.
        symbol.BackActive = AppColors.Surface;
        symbol.AccessibleName = title;
        panel.Controls.Add(symbol, 0, 1);
        panel.Controls.Add(TextLabel(message, 10F, center: true), 0, 2);
        return panel;
    }

    private static ComboBox Filter(string value, string accessibleName)
    {
        var filter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = 165,
            AccessibleName = accessibleName,
            AccessibleDescription = "Chức năng dữ liệu đang được hoàn thiện"
        };
        filter.Items.Add(value);
        filter.SelectedIndex = 0;
        return filter;
    }

    private static AntdUI.Button ActionButton(string text, string icon)
    {
        return new AntdUI.Button
        {
            Text = text, IconSvg = MainIcons.Get(icon), IconSize = new Size(26, 26), UseMnemonic = false,
            Font = new Font("Segoe UI", 10F), TextCenterHasIcon = false, IconGap = 0.6F,
            Radius = 6, WaveSize = 0, Margin = new Padding(0, 2, 0, 2),
            Cursor = Cursors.Hand, AccessibleName = text,
            // [COLOR CONFIG]: nền sáng #F7F7FF cho nút mặc định.
            DefaultBack = AppColors.Workspace,
            // [COLOR CONFIG]: chữ tối #2D3142 cho nút và icon SVG currentColor.
            ForeColor = AppColors.Text,
            // [COLOR CONFIG]: #B5BAFF cho hover, màu hệ thống trong High Contrast.
            BackHover = AppColors.Lavender,
            // [COLOR CONFIG]: #9FA1FF cho trạng thái nhấn.
            BackActive = AppColors.Primary
        };
    }

    private static TableLayoutPanel CreateLayout(int columns, int rows) => new()
    {
        Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows,
        Margin = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.FixedSize
    };

    private static Label TextLabel(string text, float size, bool bold = false, int width = 0, bool center = false)
    {
        var label = new Label
        {
            Text = text, Dock = width == 0 ? DockStyle.Fill : DockStyle.None,
            Width = width == 0 ? 100 : width, Height = 36,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            TextAlign = center ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
            AutoEllipsis = true, Margin = new Padding(4, 0, 4, 0),
            // [COLOR CONFIG]: chữ tối trên nền pastel, hỗ trợ High Contrast.
            ForeColor = AppColors.Text
        };
        return label;
    }
}
