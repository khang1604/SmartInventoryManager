using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Forms.Main;

public partial class FrmMain : Form
{
    private readonly Dictionary<string, MainNavigationButton> _navigation = new();
    private TableLayoutPanel _root = null!;
    private TableLayoutPanel _brand = null!;
    private Label _brandText = null!;
    private FlowLayoutPanel _menu = null!;
    private MainNavigationButton _logout = null!;
    private MainNavigationButton _sidebarToggle = null!;
    private bool _collapsed;
    private readonly ToolTip _toolTips = new();
    private readonly Panel _content = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label _heading = new();
    private readonly Label _breadcrumb = new();

    public FrmMain()
    {
        InitializeComponent();
        BuildLayout();
    }

    public FrmMain(AuthenticatedUserDto user) : this()
    {
        ArgumentNullException.ThrowIfNull(user);
        _account.Text = user.FullName;
        _account.AccessibleDescription = $"Tài khoản {user.FullName}";
    }

    private readonly Label _account = new();

    private void BuildLayout()
    {
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
        var status = TextLabel($"{UiBrand.ApplicationName} • {UiBrand.ProjectName} • Chức năng dữ liệu đang được hoàn thiện", 8.5F);
        status.Padding = new Padding(12, 0, 0, 0);
        root.Controls.Add(status, 0, 1);
        root.SetColumnSpan(status, 2);
        Controls.Add(root);
        Disposed += (_, _) => _toolTips.Dispose();
        ShowPage("Tổng quan");
        ResumeLayout(true);
    }

    private Control BuildSidebar()
    {
        var sidebar = CreateLayout(1, 3);
        sidebar.Padding = new Padding(8, 12, 8, 12);
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        var brand = _brand = CreateLayout(2, 1);
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
        toggle.Click += (_, _) =>
        {
            _collapsed = !_collapsed;
            _root.ColumnStyles[0].Width = (_collapsed ? 76 : 205) * DeviceDpi / 96F;
            _brandText.Visible = !_collapsed;
            _account.Visible = !_collapsed;
            _brand.ColumnStyles[0].Width = 60 * DeviceDpi / 96F;
            toggle.SetIcon(_collapsed ? "chevron-right" : "chevron-left");
            _logout.Dock = _collapsed ? DockStyle.None : DockStyle.Fill;
            _logout.Size = new Size((int)(52 * DeviceDpi / 96F), (int)(40 * DeviceDpi / 96F));
            _toolTips.SetToolTip(toggle, _collapsed ? "Mở rộng menu" : "Thu gọn menu");
            foreach (var button in _navigation.Values.Append(_logout))
            {
                button.Collapsed = _collapsed;
                button.Invalidate();
            }
            _root.PerformLayout();
            _brand.PerformLayout();
            ResizeMenu();
            _menu.PerformLayout();
            _menu.Refresh();
            Refresh();
        };
        _toolTips.SetToolTip(toggle, "Thu gọn menu");
        var menu = _menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Margin = Padding.Empty
        };
        (string title, string icon)[] pages =
        [
            ("Tổng quan", "house"), ("Sản phẩm", "box-seam"), ("Danh mục", "grid"),
            ("Nhà cung cấp", "people"), ("Kho & vị trí", "building"),
            ("Nhập kho", "box-arrow-in-down"), ("Xuất kho", "box-arrow-up"),
            ("Tồn kho", "boxes"), ("Cảnh báo", "bell"), ("Báo cáo", "file-earmark-bar-graph")
        ];
        foreach (var (title, icon) in pages)
        {
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
        menu.SizeChanged += (_, _) =>
        {
            // Chừa chỗ cho thanh cuộn dọc để không phát sinh thanh cuộn ngang khi cửa sổ nhỏ.
            ResizeMenu();
        };
        var footer = CreateLayout(1, 2);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _account.Text = "Tài khoản";
        _account.Dock = DockStyle.Fill;
        _account.TextAlign = ContentAlignment.MiddleLeft;
        _account.AutoEllipsis = true;
        footer.Controls.Add(_account, 0, 0);
        var logout = _logout = new MainNavigationButton("Đăng xuất", "box-arrow-right");
        _toolTips.SetToolTip(logout, "Đăng xuất");
        logout.Dock = DockStyle.Fill;
        logout.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "Đăng xuất và trở về màn hình đăng nhập?", "Đăng xuất",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                DialogResult = DialogResult.Retry;
                Close();
            }
        };
        footer.Controls.Add(logout, 0, 1);
        sidebar.Controls.Add(footer, 0, 2);
        return sidebar;
    }

    private void ResizeMenu()
    {
        foreach (var button in _navigation.Values)
            button.Width = _collapsed ? Math.Max(40, (int)(52 * DeviceDpi / 96F))
                : Math.Max(40, _menu.ClientSize.Width - (_menu.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 4);
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
        notifications.Click += (_, _) => MessageBox.Show(this,
            "Chức năng thông báo đang được hoàn thiện.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        header.Controls.Add(notifications, 2, 0);
        return header;
    }

    private void ShowPage(string title)
    {
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
            child.Dispose();
        }
        _content.Controls.Add(title == "Tổng quan" ? BuildOverview() : EmptyPanel(title,
            "Màn hình đang được hoàn thiện. Chưa có chức năng thao tác dữ liệu.", "file-earmark-text"));
    }

    private Control BuildOverview()
    {
        var page = CreateLayout(1, 4);
        page.MinimumSize = new Size(720, 560);
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 57));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 43));
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        filters.Controls.Add(TextLabel("Kho:", 10F, width: 40));
        filters.Controls.Add(Filter("Tất cả kho", "Lọc theo kho"));
        filters.Controls.Add(TextLabel("Thời gian:", 10F, width: 80));
        filters.Controls.Add(Filter("Tháng này", "Lọc theo thời gian"));
        var refresh = ActionButton("Làm mới", "arrow-clockwise");
        refresh.Size = new Size(125, 36);
        refresh.Enabled = false;
        refresh.AccessibleDescription = "Chưa sẵn sàng: chức năng dữ liệu đang được hoàn thiện";
        // [COLOR CONFIG]: #9FA1FF, nền hành động chính theo thiết kế.
        refresh.DefaultBack = AppColors.Primary;
        filters.Controls.Add(refresh);
        page.Controls.Add(filters, 0, 0);
        var cards = CreateLayout(4, 1);
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(Metric("Sản phẩm", "box-seam", AppColors.Lavender), 0, 0);
        cards.Controls.Add(Metric("Giá trị tồn kho", "coin", AppColors.Mint), 1, 0);
        cards.Controls.Add(Metric("Sắp hết hàng", "exclamation-triangle", AppColors.Sky), 2, 0);
        cards.Controls.Add(Metric("Hết hàng", "dash-circle", AppColors.Lavender), 3, 0);
        page.Controls.Add(cards, 0, 1);
        var panels = CreateLayout(2, 1);
        panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        panels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        panels.Controls.Add(EmptyPanel("Nhập / xuất theo thời gian", "Chưa có dữ liệu • Chức năng đang được hoàn thiện", "bar-chart"), 0, 0);
        panels.Controls.Add(EmptyPanel("Cảnh báo tồn kho", "Chưa có dữ liệu cảnh báo", "bell"), 1, 0);
        page.Controls.Add(panels, 0, 2);
        var activity = CreateLayout(1, 3);
        activity.Margin = new Padding(5);
        activity.Padding = new Padding(14);
        // [COLOR CONFIG]: trắng cho vùng hoạt động gần đây.
        activity.BackColor = AppColors.Surface;
        activity.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        activity.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        activity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        activity.Controls.Add(TextLabel("Hoạt động gần đây", 12F, true), 0, 0);
        var columns = CreateLayout(5, 1);
        foreach (var name in new[] { "Mã phiếu", "Loại phiếu", "Kho", "Ngày", "Trạng thái" })
        {
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            columns.Controls.Add(TextLabel(name, 9.5F, true));
        }
        // [COLOR CONFIG]: lavender cho hàng tiêu đề bảng minh họa chưa có dữ liệu.
        columns.BackColor = AppColors.Workspace;
        activity.Controls.Add(columns, 0, 1);
        activity.Controls.Add(TextLabel("Chưa có dữ liệu giao dịch", 10F, center: true), 0, 2);
        page.Controls.Add(activity, 0, 3);
        return page;
    }

    private static Control Metric(string title, string icon, Color accent)
    {
        var card = CreateLayout(2, 1);
        card.Margin = new Padding(5);
        card.Padding = new Padding(12);
        // [COLOR CONFIG]: trắng cho thẻ thống kê, không dùng số liệu giả.
        card.BackColor = AppColors.Surface;
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var symbol = ActionButton("", icon);
        symbol.Dock = DockStyle.Fill;
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        symbol.AccessibleName = title;
        // [COLOR CONFIG]: điểm nhấn lavender/sky/mint tương ứng thẻ thống kê.
        symbol.DefaultBack = accent;
        card.Controls.Add(symbol, 0, 0);
        card.Controls.Add(TextLabel($"{title}\n—", 11F, true), 1, 0);
        card.AccessibleDescription = $"{title}: chưa có dữ liệu";
        return card;
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
            Enabled = false, AccessibleName = accessibleName,
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
