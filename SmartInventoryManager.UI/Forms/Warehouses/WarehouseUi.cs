using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Warehouses;

internal static class WarehouseUi
{
    public static void BuildList(Form form, bool location, bool canManage, UiServices? services = null)
    {
        if (services is not null) { BuildConnected(form, location, canManage, services); return; }
        DataGridView? grid = null;
        UiBrand.ApplyIcon(form);
        form.Font = new Font("Segoe UI", 10F);
        var root = ProductUi.Layout(1, 4);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        int count = location ? 3 : 2;
        var filters = ProductUi.Layout(count, 2);
        filters.Padding = new Padding(16, 10, 4, 12);
        // [COLOR CONFIG]: nền trắng của vùng lọc.
        filters.BackColor = AppColors.Surface;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, location ? 44 : 60));
        for (int i = 1; i < count; i++) filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, location ? 28 : 40));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filters.Controls.Add(ProductUi.Label("Tìm kiếm", 10, true), 0, 0);
        var search = ProductUi.Input("txtSearch", location ? "Nhập mã hoặc tên vị trí" : "Nhập mã hoặc tên kho");
        search.PrefixSvg = MainIcons.Get("search");
        search.ReadOnly = true;
        search.AccessibleName = search.PlaceholderText;
        search.AccessibleDescription = "Tìm kiếm chưa sẵn sàng khi chưa có dữ liệu";
        filters.Controls.Add(search, 0, 1);
        for (int i = 1; i < count; i++)
        {
            bool warehouseFilter = location && i == 1;
            filters.Controls.Add(ProductUi.Label(warehouseFilter ? "Kho" : "Trạng thái", 10, true), i, 0);
            var filter = ProductUi.Input(warehouseFilter ? "cboWarehouse" : "cboStatus");
            filter.Text = warehouseFilter ? "Tất cả kho" : "Tất cả trạng thái";
            filter.SuffixSvg = MainIcons.Get("chevron-down");
            filter.ReadOnly = true;
            filter.AccessibleName = filter.Text;
            filter.AccessibleDescription = "Bộ lọc chưa sẵn sàng khi chưa có dữ liệu";
            filters.Controls.Add(filter, i, 1);
        }
        root.Controls.Add(filters, 0, 0);
        var toolbar = ProductUi.Layout(2, 1);
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        toolbar.Padding = new Padding(0, 14, 0, 10);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        if (canManage)
        {
            var add = ProductUi.Button("btnAdd", location ? "Thêm vị trí" : "Thêm kho", "plus-lg", true);
            add.AccessibleDescription = "Chỉ mở biểu mẫu xem trước, chưa lưu dữ liệu hoặc cấp quyền quản lý";
            add.Click += async (_, _) =>
            {
                if (services is null) return;
                using var detail = new FrmWarehouseDetail(services, location);
                if (detail.ShowDialog(form) == DialogResult.OK) await Task.CompletedTask;
            };
            actions.Controls.Add(add);
        }
        actions.Controls.Add(Unavailable("btnPreview", "Xem chi tiết", "eye", 150));
        if (canManage)
        {
            var edit = Unavailable("btnEdit", "Sửa", "pencil", 100);
            var deactivate = Unavailable("btnDeactivate", "Ngừng hoạt động", "dash-circle", 190);
            edit.Enabled = deactivate.Enabled = services is not null;
            edit.Click += async (_, _) => await EditAsync();
            deactivate.Click += async (_, _) => await ToggleAsync();
            actions.Controls.Add(edit); actions.Controls.Add(deactivate);
        }
        toolbar.Controls.Add(actions, 0, 0);
        var refresh = Unavailable("btnRefresh", "", "arrow-clockwise", 44);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = Padding.Empty;
        refresh.AccessibleName = "Làm mới danh sách";
        if (services is not null) { refresh.Enabled = true; refresh.Click += async (_, _) => await LoadAsync(); }
        toolbar.Controls.Add(refresh, 1, 0);
        root.Controls.Add(toolbar, 0, 1);
        var area = ProductUi.Layout(1, 2);
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng của bảng và trạng thái chưa tải dữ liệu.
        area.BackColor = AppColors.Surface;
        grid = new DataGridView
        {
            Name = location ? "dgvLocations" : "dgvWarehouses", Dock = DockStyle.Fill,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, AllowUserToResizeColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 34 },
            AutoGenerateColumns = false, RowHeadersVisible = false, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, Margin = Padding.Empty,
            // [COLOR CONFIG]: nền bảng trắng và đường phân chia nhẹ.
            BackgroundColor = AppColors.Surface, GridColor = AppColors.Border
        };
        // [COLOR CONFIG]: header nhạt, chữ tối.
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppColors.Workspace;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppColors.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
        var columns = location
            ? new[] { ("WarehouseName", "Kho"), ("LocationCode", "Mã vị trí"), ("LocationName", "Tên vị trí"), ("Status", "Trạng thái") }
            : new[] { ("WarehouseCode", "Mã kho"), ("WarehouseName", "Tên kho"), ("Location", "Địa chỉ"), ("ManagerName", "Người quản lý"), ("Status", "Trạng thái") };
        foreach (var (name, caption) in columns)
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = caption, MinimumWidth = 110, SortMode = DataGridViewColumnSortMode.NotSortable });
        UiDetails.AddColumn(grid);
        area.Controls.Add(grid, 0, 0);
        grid.SelectionChanged += (_, _) => UpdateActions();
        var empty = ProductUi.Layout(1, 5);
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var symbol = ProductUi.Button("emptyIcon", "", location ? "grid" : "building");
        symbol.Dock = DockStyle.Fill;
        symbol.IconSize = new Size(56, 56);
        symbol.BorderWidth = 0;
        symbol.TabStop = false;
        symbol.Cursor = Cursors.Default;
        var captionLabel = ProductUi.Label("Chưa có dữ liệu để hiển thị", 13, true);
        captionLabel.TextAlign = ContentAlignment.MiddleCenter;
        var description = ProductUi.Label("Chức năng tải dữ liệu đang được hoàn thiện");
        description.TextAlign = ContentAlignment.MiddleCenter;
        // [COLOR CONFIG]: chữ phụ của trạng thái chưa có dữ liệu.
        description.ForeColor = AppColors.Muted;
        empty.Controls.Add(symbol, 0, 1);
        empty.Controls.Add(captionLabel, 0, 2);
        empty.Controls.Add(description, 0, 3);
        area.Controls.Add(empty, 0, 1);
        root.Controls.Add(LiveUi.TableFrame(area), 0, 2);
        var note = ProductUi.Label(services is null
            ? "Chưa kết nối dữ liệu • Danh sách chỉ xem"
            : location ? "Quản lý vị trí theo kho" : "Quản lý kho", 9);
        // [COLOR CONFIG]: ghi chú phụ.
        note.ForeColor = AppColors.Muted;
        root.Controls.Add(note, 0, 3);
        form.Controls.Add(root);

        async Task LoadAsync()
        {
            if (services is null) return;
            try
            {
                if (location)
                {
                    var rows = await services.RunAsync(() => services.Locations.GetAsync(services.Session));
                    grid!.DataSource = rows.ToList();
                    foreach (DataGridViewColumn column in grid.Columns) column.DataPropertyName = column.Name == "Status" ? nameof(LocationDto.IsActive) : column.Name;
                }
                else
                {
                    var rows = await services.RunAsync(() => services.Warehouses.GetAllAsync(services.Session));
                    grid!.DataSource = rows.ToList();
                    foreach (DataGridViewColumn column in grid.Columns) column.DataPropertyName = column.Name == "Status" ? nameof(WarehouseDto.IsActive) : column.Name;
                }
                grid.CellFormatting += (_, e) => { if (grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is bool active) e.Value = active ? "Đang hoạt động" : "Ngừng hoạt động"; };
                UpdateActions();
            }
            catch (Exception ex) { MessageBox.Show(form, LiveUi.Error(ex), "Không thể tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void UpdateActions()
        {
            var selected = grid?.CurrentRow?.DataBoundItem is WarehouseDto or LocationDto;
            if (form.Controls.Find("btnEdit", true).FirstOrDefault() is Control edit) edit.Enabled = selected && canManage;
            if (form.Controls.Find("btnDeactivate", true).FirstOrDefault() is Control deactivate) deactivate.Enabled = selected && canManage;
        }

        async Task EditAsync()
        {
            if (services is null) return;
            if (location && grid?.CurrentRow?.DataBoundItem is LocationDto locationDto)
            {
                using var detail = new FrmWarehouseDetail(services, true, locationDto);
                if (detail.ShowDialog(form) == DialogResult.OK) await LoadAsync();
            }
            else if (!location && grid?.CurrentRow?.DataBoundItem is WarehouseDto warehouseDto)
            {
                using var detail = new FrmWarehouseDetail(services, false, warehouseDto);
                if (detail.ShowDialog(form) == DialogResult.OK) await LoadAsync();
            }
        }

        async Task ToggleAsync()
        {
            if (services is null) return;
            if (location && grid?.CurrentRow?.DataBoundItem is LocationDto locationDto) await services.RunAsync(() => services.Locations.SetActiveAsync(services.Session, locationDto.LocationId, !locationDto.IsActive));
            else if (!location && grid?.CurrentRow?.DataBoundItem is WarehouseDto warehouseDto) await services.RunAsync(() => services.Warehouses.SetActiveAsync(services.Session, warehouseDto.WarehouseId, !warehouseDto.IsActive));
            await LoadAsync();
        }
        if (services is not null) form.Shown += async (_, _) => await LoadAsync();
    }

    private static void BuildConnected(Form form, bool location, bool canManage, UiServices services)
    {
        UiBrand.ApplyIcon(form);
        var state=LiveUi.Choice("cboStatus","Trạng thái");
        state.Items.AddRange(["Tất cả trạng thái","Đang hoạt động","Ngừng hoạt động"]); state.SelectedIndex=0;
        bool Matches(string code,string name,string search,bool active) => (string.IsNullOrEmpty(search) || code.Contains(search,StringComparison.CurrentCultureIgnoreCase) || name.Contains(search,StringComparison.CurrentCultureIgnoreCase)) && (state.SelectedIndex==0 || active==(state.SelectedIndex==1));
        if(location)
        {
            var page=new DataListPage<LocationDto>(services,"Vị trí trong phạm vi được phép",[("WarehouseName","Kho"),("LocationCode","Mã vị trí"),("LocationName","Tên vị trí"),("IsActive","Trạng thái")],async (_,search,_,_) => (await services.Locations.GetAsync(services.Session)).Where(row=>Matches(row.LocationCode,row.LocationName ?? "",search,row.IsActive)).ToList());
            page.AddFilter("Trạng thái",state); page.AddDetailsColumn();
            if(canManage) {
                page.Action("Thêm vị trí",async()=> { using var detail=new FrmWarehouseDetail(services,true); if(detail.ShowDialog(form)==DialogResult.OK) await page.RefreshAsync(); },"plus-lg");
                page.SelectedAction("Sửa",async(row,_)=> { using var detail=new FrmWarehouseDetail(services,true,row); detail.ShowDialog(form); },"pencil");
                page.SelectedAction("Đổi trạng thái",async(row,_)=> { if(MessageBox.Show(form,"Thay đổi trạng thái vị trí đã chọn?","Xác nhận",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)==DialogResult.OK) await services.RunAsync(()=>services.Locations.SetActiveAsync(services.Session,row.LocationId,!row.IsActive)); },"dash-circle");
            }
            state.SelectedIndexChanged+=async(_,_)=>await page.RefreshAsync(); form.Controls.Add(page);
        }
        else
        {
            var page=new DataListPage<WarehouseDto>(services,"Kho trong phạm vi được phép",[("WarehouseCode","Mã kho"),("WarehouseName","Tên kho"),("Location","Địa chỉ"),("ManagerName","Người quản lý"),("IsActive","Trạng thái")],async (_,search,_,_) => (await services.Warehouses.GetAllAsync(services.Session)).Where(row=>Matches(row.WarehouseCode,row.WarehouseName,search,row.IsActive)).ToList());
            page.AddFilter("Trạng thái",state); page.AddDetailsColumn();
            if(canManage) {
                page.Action("Thêm kho",async()=> { using var detail=new FrmWarehouseDetail(services,false); if(detail.ShowDialog(form)==DialogResult.OK) await page.RefreshAsync(); },"plus-lg");
                page.SelectedAction("Sửa",async(row,_)=> { using var detail=new FrmWarehouseDetail(services,false,row); detail.ShowDialog(form); },"pencil");
                page.SelectedAction("Đổi trạng thái",async(row,_)=> { if(MessageBox.Show(form,"Thay đổi trạng thái kho đã chọn?","Xác nhận",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)==DialogResult.OK) await services.RunAsync(()=>services.Warehouses.SetActiveAsync(services.Session,row.WarehouseId,!row.IsActive)); },"dash-circle");
            }
            state.SelectedIndexChanged+=async(_,_)=>await page.RefreshAsync(); form.Controls.Add(page);
        }
    }

    private static AntdUI.Button Unavailable(string name, string text, string icon, int width)
    {
        var button = ProductUi.Button(name, text, icon);
        button.Width = width;
        button.Enabled = false;
        button.AccessibleDescription = "Chưa sẵn sàng: chưa có dữ liệu được chọn hoặc hợp đồng thao tác";
        return button;
    }
}
