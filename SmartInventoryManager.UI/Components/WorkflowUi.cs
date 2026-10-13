using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

// Bố cục chung cho các màn hình nghiệp vụ đang chờ hợp đồng Service/DTO.
internal static class WorkflowUi
{
    public static Control List(string name, (string Caption, string Value)[] filters,
        string[] columns, string icon, string? previewCaption = null, Func<Form>? preview = null,
        string[]? pendingActions = null, bool report = false)
    {
        var root = ProductUi.Layout(1, report ? 5 : 4);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        if (report) root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        var filterPanel = ProductUi.Layout(filters.Length, 3);
        filterPanel.Padding = new Padding(16, 10, 4, 12);
        // [COLOR CONFIG]: vùng lọc trắng.
        filterPanel.BackColor = AppColors.Surface;
        filterPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        filterPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        filterPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        for (int i = 0; i < filters.Length; i++)
        {
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / filters.Length));

            if (filters[i].Caption == "Thời gian")
            {
                var dates = new DateRangeControl();
                filterPanel.Controls.Add(dates, i, 0);
                filterPanel.SetRowSpan(dates, 3);
                continue;
            }
            filterPanel.Controls.Add(ProductUi.Label(filters[i].Caption, 10, true), i, 0);
            var input = ProductUi.Input($"filter{i}", filters[i].Value);
            input.ReadOnly = false;
            input.TabIndex = i;
            input.AccessibleName = filters[i].Caption;
            input.AccessibleDescription = "Chưa sẵn sàng: chức năng tải dữ liệu đang được hoàn thiện";
            if (filters[i].Caption == "Tìm kiếm") input.PrefixSvg = MainIcons.Get("search");
            else input.SuffixSvg = MainIcons.Get("chevron-down");
            filterPanel.Controls.Add(input, i, 1);
        }
        root.Controls.Add(filterPanel, 0, 0);
        var toolbar = ProductUi.Layout(2, 1);
        toolbar.Padding = new Padding(0, 14, 0, 10);
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        if (preview is not null)
        {
            var button = ProductUi.Button("btnAddPreview", previewCaption ?? "Xem biểu mẫu", "plus-lg", true);
            button.Width = 195;
            button.AccessibleDescription = "Mở biểu mẫu xem trước, chưa ghi dữ liệu";
            button.Click += (_, _) => { using var detail = preview(); detail.ShowDialog(root.FindForm()); };
            actions.Controls.Add(button);
        }
        actions.Controls.Add(Pending("btnView", "Xem chi tiết", "eye"));
        int actionIndex = 0;
        foreach (var text in pendingActions ?? [])
            actions.Controls.Add(Pending($"btnPending{actionIndex++}", text, report ? "file-earmark-text" : "pencil"));
        toolbar.Controls.Add(actions, 0, 0);
        var refresh = Pending("btnRefresh", "", "arrow-clockwise");
        refresh.AccessibleName = "Làm mới";
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = Padding.Empty;
        toolbar.Controls.Add(refresh, 1, 0);
        root.Controls.Add(toolbar, 0, 1);
        int tableRow = report ? 3 : 2;
        if (report)
        {
            var summaries = ProductUi.Layout(3, 1);
            summaries.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (var text in new[] { "Phạm vi báo cáo", "Số bản ghi", "Tổng hợp" })
            {
                summaries.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                var label = ProductUi.Label($"{text}\n—", 11, true);
                label.Padding = new Padding(14, 4, 0, 4);
                label.Margin = new Padding(0, 0, 10, 12);
                // [COLOR CONFIG]: nền trắng thẻ tổng hợp, không có số liệu giả.
                label.BackColor = AppColors.Surface;
                summaries.Controls.Add(label);
            }
            root.Controls.Add(summaries, 0, 2);
        }
        root.Controls.Add(TableWithEmpty(name, columns, icon), 0, tableRow);
        var note = ProductUi.Label("Chưa kết nối dữ liệu • Bộ lọc và thao tác dữ liệu chưa sẵn sàng", 9);
        // [COLOR CONFIG]: ghi chú phụ dưới danh sách.
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, tableRow + 1);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        root.Dock = DockStyle.Top;
        scroll.Controls.Add(root);
        scroll.SizeChanged += (_, _) => root.Height = Math.Max(report ? 600 : 540, scroll.ClientSize.Height);
        toolbar.SizeChanged += (_, _) => root.RowStyles[1].Height =
            actions.Controls.Cast<Control>().Sum(control => control.Width + control.Margin.Horizontal) > actions.ClientSize.Width ? 90 : 72;
        return scroll;
    }

    public static DataGridView Grid(string name, string[] columns)
    {
        var grid = new DataGridView
        {
            Name = name, Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToOrderColumns = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 34 },
            RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, Margin = Padding.Empty,
            AccessibleName = name,
            // [COLOR CONFIG]: nền bảng trắng và đường phân chia nhẹ.
            BackgroundColor = AppColors.Surface, GridColor = AppColors.Border
        };
        // [COLOR CONFIG]: header nhạt, chữ tối.
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        for (int i = 0; i < columns.Length; i++)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = $"column{i}", HeaderText = columns[i], MinimumWidth = columns[i].Length > 17 ? 145 : 115,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        // [COLOR CONFIG]: dòng đang chọn dùng lavender và chữ tối.
        grid.DefaultCellStyle.SelectionBackColor = AppColors.Lavender;
        grid.DefaultCellStyle.SelectionForeColor = AppColors.SelectedText;
        UiDetails.AddColumn(grid);
        return grid;
    }

    public static Control TableWithEmpty(string name, string[] columns, string icon)
    {
        var area = ProductUi.Layout(1, 2);
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var grid = Grid(name, columns);
        area.Controls.Add(grid, 0, 0);
        area.Controls.Add(Empty("Chưa có dữ liệu để hiển thị", "Chức năng tải dữ liệu đang được hoàn thiện", icon), 0, 1);
        area.SizeChanged += (_, _) => area.RowStyles[0].Height =
            (area.ClientSize.Width < grid.Columns.Cast<DataGridViewColumn>().Sum(column => column.MinimumWidth) ? 74 : 54) * area.DeviceDpi / 96F;
        return LiveUi.TableFrame(area);
    }

    public static Control Empty(string title, string description, string icon)
    {
        var empty = ProductUi.Layout(1, 5);
        // [COLOR CONFIG]: vùng nội dung trắng.
        empty.BackColor = AppColors.Surface;
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var symbol = ProductUi.Button("emptyIcon", "", icon);
        symbol.Dock = DockStyle.Fill;
        symbol.IconSize = new Size(56, 56);
        symbol.BorderWidth = 0;
        // [COLOR CONFIG]: icon trạng thái trống giữ nền trắng khi rê chuột hoặc nhấn.
        symbol.BackHover = AppColors.Surface;
        symbol.BackActive = AppColors.Surface;
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        var caption = ProductUi.Label(title, 13, true);
        caption.TextAlign = ContentAlignment.MiddleCenter;
        var note = ProductUi.Label(description);
        note.TextAlign = ContentAlignment.MiddleCenter;
        // [COLOR CONFIG]: chữ phụ của trạng thái chưa có dữ liệu.
        note.ForeColor = AppColors.Muted;
        empty.Controls.Add(symbol, 0, 1);
        empty.Controls.Add(caption, 0, 2);
        empty.Controls.Add(note, 0, 3);
        return empty;
    }

    public static AntdUI.Button Pending(string name, string caption, string icon)
    {
        var button = ProductUi.Button(name, caption, icon);
        button.Width = Math.Max(145, TextRenderer.MeasureText(caption, button.Font).Width + 48);
        button.Click += (_, _) => ActionFeedback.Show(button);
        button.AccessibleDescription = "Chưa sẵn sàng: chưa có dữ liệu được chọn hoặc hợp đồng thao tác";
        return button;
    }

    public static void StyleForm(Form form)
    {
        UiBrand.ApplyIcon(form);
        form.Font = new Font("Segoe UI", 10F);
        // [COLOR CONFIG]: nền và chữ của các màn hình nghiệp vụ.
        form.BackColor = AppColors.Workspace;
        form.ForeColor = AppColors.Text;
    }
}
