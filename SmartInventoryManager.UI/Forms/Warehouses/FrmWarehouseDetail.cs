using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Warehouses;

public sealed class FrmWarehouseDetail : Form
{
    private readonly UiServices? _services;
    private readonly bool _location;
    private readonly int? _id;

    public FrmWarehouseDetail(bool location = false) : this(null, location, null, null) { }

    public FrmWarehouseDetail(UiServices services, bool location, WarehouseDto? warehouse = null) : this(services, location, warehouse, null) { }
    public FrmWarehouseDetail(UiServices services, bool location, LocationDto? locationDto) : this(services, location, null, locationDto) { }

    private FrmWarehouseDetail(UiServices? services, bool location, WarehouseDto? warehouse, LocationDto? locationDto)
    {
        _services = services; _location = location; _id = warehouse?.WarehouseId ?? locationDto?.LocationId;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 10F);
        string subject = location ? "vị trí" : "kho";
        Text = $"VioWare - Thông tin {subject}";
        Shown += (_, _) => WorkflowUi.FitToWorkingArea(this);
        ClientSize = new Size(560, location ? 520 : 600);
        MinimumSize = new Size(500, 480);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = MinimizeBox = false;
        // [COLOR CONFIG]: nền và chữ theo bảng màu KH duyệt.
        BackColor = AppColors.Workspace;
        ForeColor = AppColors.Text;
        var root = ProductUi.Layout(1, 3);
        root.Padding = new Padding(20, 14, 20, 12);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var heading = ProductUi.Layout(1, 2);
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(ProductUi.Label($"Thông tin {subject}", 18, true), 0, 0);
        var instruction = ProductUi.Label("Các trường có dấu * là bắt buộc");
        // [COLOR CONFIG]: hướng dẫn phụ màu muted.
        instruction.ForeColor = AppColors.Muted;
        heading.Controls.Add(instruction, 0, 1);
        root.Controls.Add(heading, 0, 0);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        var fields = ProductUi.Layout(2, location ? 5 : 6);
        fields.Dock = DockStyle.Top;
        fields.Height = location ? 280 : 368;
        fields.Padding = new Padding(12);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng nhóm trường.
        fields.BackColor = AppColors.Surface;
        if (location)
        {
            Field(fields, 0, "Kho *", LiveUi.Choice("cboWarehouse", "Kho của vị trí"));
            Field(fields, 1, "Mã vị trí *", Input("txtLocationCode", maxLength: 50));
            Field(fields, 2, "Tên vị trí", Input("txtLocationName", maxLength: 100));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            var hint = ProductUi.Label("Mã vị trí được phân biệt trong từng kho", 9);
            // [COLOR CONFIG]: ghi chú liên hệ kho/vị trí.
            hint.ForeColor = AppColors.Muted;
            fields.Controls.Add(hint, 0, 3);
            fields.SetColumnSpan(hint, 2);
        }
        else
        {
            Field(fields, 0, "Mã kho *", Input("txtWarehouseCode", maxLength: 50));
            Field(fields, 1, "Tên kho *", Input("txtWarehouseName", maxLength: 200));
            var address = Input("txtAddress", maxLength: 255);
            address.Multiline = true;
            Field(fields, 2, "Địa chỉ", address, 110);
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            fields.Controls.Add(ProductUi.Label("Người quản lý", 10, true), 0, 3);
            fields.Controls.Add(LiveUi.Choice("cboManager", "Người quản lý kho"), 1, 3);
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        }
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var active = new CheckBox
        {
            Name = "chkIsActive", Text = "Đang hoạt động", Checked = true,
            Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0), TabIndex = 4,
            // [COLOR CONFIG]: nền mint và chữ tối cho trạng thái.
            BackColor = AppColors.Mint, ForeColor = AppColors.Text
        };
        int activeRow = location ? 4 : 5;
        fields.Controls.Add(active, 0, activeRow);
        fields.SetColumnSpan(active, 2);
        scroll.Controls.Add(fields);
        root.Controls.Add(scroll, 0, 1);
        var footer = ProductUi.Layout(2, 1);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236));
        footer.Padding = new Padding(0, 14, 0, 0);
        var note = ProductUi.Label(_services is null ? "Xem trước • Chưa lưu dữ liệu" : "Các trường có dấu * là bắt buộc", 8.5F);
        // [COLOR CONFIG]: ghi chú phụ.
        note.ForeColor = AppColors.Muted;
        footer.Controls.Add(note, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Width = 90;
        close.DialogResult = DialogResult.Cancel;
        close.TabIndex = 5;
        var save = ProductUi.Button("btnSave", $"Lưu {subject}", primary: true);
        save.Width = 135;
        save.Enabled = _services is not null;
        save.TabIndex = 6;
        save.Margin = Padding.Empty;
        save.AccessibleDescription = "Lưu thông tin";
        save.Click += async (_, _) => await SaveAsync();
        actions.Controls.Add(close);
        actions.Controls.Add(save);
        CancelButton = close;
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        if (warehouse is not null) LoadWarehouse(warehouse);
        if (locationDto is not null) LoadLocation(locationDto);
        if (!location && services is not null)
            Shown += async (_, _) =>
            {
                var save = Controls.Find("btnSave", true).Single();
                save.Enabled = false;
                try
                {
                    var managers = await services.RunAsync(() => services.Warehouses.GetActiveManagersAsync(services.Session));
                    if (IsDisposed || Disposing) return;
                    var picker = Controls.Find("cboManager", true).OfType<ComboBox>().Single();
                    picker.DisplayMember = "Name";
                    picker.ValueMember = "Id";
                    var choices = new List<LookupItemDto> { new(0, "", "Không chọn người quản lý") };
                    choices.AddRange(managers);
                    // Preserve an existing disabled manager until the user explicitly changes the assignment.
                    if (warehouse?.ManagerId is int managerId && choices.All(item => item.Id != managerId))
                        choices.Add(new LookupItemDto(managerId, "", (warehouse.ManagerName ?? "Người quản lý hiện tại") + " (không còn hợp lệ)"));
                    picker.DataSource = choices;
                    picker.SelectedValue = warehouse?.ManagerId ?? 0;
                    save.Enabled = true;
                }
                catch (Exception ex)
                {
                    if (!IsDisposed && !Disposing) MessageBox.Show(this, LiveUi.LoadError(ex), "Người quản lý kho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        if(location && services is not null) Shown+=async(_,_)=> { try { var choices=await services.RunAsync(()=>services.Warehouses.GetActiveAsync(services.Session)); if (IsDisposed || Disposing) return; var picker=Controls.Find("cboWarehouse",true).OfType<ComboBox>().Single(); picker.DisplayMember="Name"; picker.ValueMember="Id"; picker.DataSource=new[]{new LookupItemDto(0,"","Chọn kho")}.Concat(choices).ToList(); picker.SelectedValue=locationDto?.WarehouseId ?? 0; } catch(Exception ex) { if (!IsDisposed && !Disposing) MessageBox.Show(this,LiveUi.LoadError(ex),"Kho",MessageBoxButtons.OK,MessageBoxIcon.Warning); } };
    }

    private void LoadWarehouse(WarehouseDto value) { Input("txtWarehouseCode").Text = value.WarehouseCode; Input("txtWarehouseName").Text = value.WarehouseName; Input("txtAddress").Text = value.Location ?? string.Empty; Check().Checked = value.IsActive; }
    private void LoadLocation(LocationDto value) { Input("txtLocationCode").Text = value.LocationCode; Input("txtLocationName").Text = value.LocationName ?? string.Empty;  Check().Checked = value.IsActive; }
    private AntdUI.Input Input(string name) => Controls.Find(name, true).OfType<AntdUI.Input>().FirstOrDefault() ?? throw new InvalidOperationException($"Không tìm thấy control {name}.");
    private CheckBox Check() => Controls.Find("chkIsActive", true).OfType<CheckBox>().FirstOrDefault() ?? throw new InvalidOperationException("Không tìm thấy trạng thái.");
    private bool _saving;

    private async Task SaveAsync()
    {
        if (_services is null || _saving) return;
        _saving = true;
        var save = Controls.Find("btnSave", true).First();
        save.Enabled = false;
        try
        {
            if (_location) {
                var choice=Controls.Find("cboWarehouse",true).OfType<ComboBox>().Single().SelectedItem as LookupItemDto;
                if(choice is null || choice.Id==0) { MessageBox.Show(this,"Vui lòng chọn kho.","Kiểm tra thông tin"); return; }
                var locationRequest=new LocationRequestDto { WarehouseId=choice.Id,LocationCode=Input("txtLocationCode").Text,LocationName=Input("txtLocationName").Text,IsActive=Check().Checked };
                await _services.RunAsync(()=>_id.HasValue ? _services.Locations.UpdateAsync(_services.Session,_id.Value,locationRequest) : _services.Locations.CreateAsync(_services.Session,locationRequest));
                DialogResult=DialogResult.OK; Close(); return;
            }
            var manager = Controls.Find("cboManager", true).OfType<ComboBox>().Single().SelectedItem as LookupItemDto;
            var request = new WarehouseRequestDto { WarehouseCode = Input("txtWarehouseCode").Text, WarehouseName = Input("txtWarehouseName").Text, Location = Input("txtAddress").Text, ManagerId = manager is { Id: > 0 } ? manager.Id : null, IsActive = Check().Checked };
            await _services.RunAsync(() => _id.HasValue ? _services.Warehouses.UpdateAsync(_services.Session, _id.Value, request) : _services.Warehouses.CreateAsync(_services.Session, request));
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _saving = false; if (!IsDisposed) save.Enabled = true; }
    }

    private static AntdUI.Input Input(string name, string placeholder = "", int maxLength = 0, bool dropdown = false)
    {
        var input = ProductUi.Input(name, placeholder);
        if (maxLength > 0) input.MaxLength = maxLength;
        input.ReadOnly = dropdown;
        if (dropdown)
        {
            input.SuffixSvg = MainIcons.Get("chevron-down");
            input.AccessibleDescription = "Danh sách lựa chọn chưa có dữ liệu";
        }
        return input;
    }

    private static void Field(TableLayoutPanel fields, int row, string caption, Control input, int height = 54)
    {
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        fields.Controls.Add(ProductUi.Label(caption, 10, true), 0, row);
        input.Margin = new Padding(0, 5, 0, 5);
        input.TabIndex = row;
        input.AccessibleName = caption.Replace(" *", "");
        fields.Controls.Add(input, 1, row);
    }
}
