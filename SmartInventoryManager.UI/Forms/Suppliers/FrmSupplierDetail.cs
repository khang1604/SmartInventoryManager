using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Suppliers;

public sealed class FrmSupplierDetail : Form
{
    private readonly UiServices? _services;
    private readonly int? _supplierId;

    public FrmSupplierDetail() : this((UiServices?)null, (int?)null)
    {
    }

    public FrmSupplierDetail(UiServices services, SupplierDto? supplier = null) : this(services, supplier?.SupplierId)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _supplierId = supplier?.SupplierId;
        Shown += (_, _) => { if (supplier is not null) LoadSupplier(supplier); };
    }

    private FrmSupplierDetail(UiServices? services, int? supplierId)
    {
        _services = services;
        _supplierId = supplierId;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 10F);
        Text = "VioWare - Thông tin nhà cung cấp";
        Shown += (_, _) => WorkflowUi.FitToWorkingArea(this);
        ClientSize = new Size(640, 710);
        MinimumSize = new Size(560, 550);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        // [COLOR CONFIG]: nền và chữ của biểu mẫu theo bảng màu KH duyệt.
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
        heading.Controls.Add(ProductUi.Label("Thông tin nhà cung cấp", 18, true), 0, 0);
        var instruction = ProductUi.Label("Các trường có dấu * là bắt buộc");
        // [COLOR CONFIG]: chữ phụ của hướng dẫn.
        instruction.ForeColor = AppColors.Muted;
        heading.Controls.Add(instruction, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        var sections = ProductUi.Layout(1, 3);
        sections.Dock = DockStyle.Top;
        sections.Height = 510;
        sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 218));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 238));
        sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var general = Section("Thông tin chung", 44);
        AddField(general, 0, 0, "Mã nhà cung cấp *", "txtSupplierCode", 50, 0);
        AddField(general, 1, 0, "Tên nhà cung cấp *", "txtSupplierName", 200, 1);
        AddField(general, 0, 1, "Mã số thuế", "txtTaxCode", 50, 2, span: true);
        sections.Controls.Add(general.Parent!, 0, 0);
        var contact = Section("Thông tin liên hệ", 64);
        AddField(contact, 0, 0, "Số điện thoại", "txtPhone", 20, 3);
        AddField(contact, 1, 0, "Email", "txtEmail", 100, 4);
        AddField(contact, 0, 1, "Địa chỉ", "txtAddress", 255, 5, span: true, multiline: true);
        sections.Controls.Add(contact.Parent!, 0, 1);
        sections.Controls.Add(new CheckBox
        {
            Name = "chkIsActive", Text = "Đang hoạt động", Checked = true,
            Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0), TabIndex = 6,
            // [COLOR CONFIG]: nền mint và chữ tối của trạng thái hoạt động.
            BackColor = AppColors.Mint, ForeColor = AppColors.Text
        }, 0, 2);
        scroll.Controls.Add(sections);
        root.Controls.Add(scroll, 0, 1);

        var footer = ProductUi.Layout(2, 1);
        footer.Padding = new Padding(0, 14, 0, 0);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 276));
        var note = ProductUi.Label("Các trường có dấu * là bắt buộc", 8.5F);
        // [COLOR CONFIG]: ghi chú phụ về trạng thái biểu mẫu.
        note.ForeColor = AppColors.Muted;
        footer.Controls.Add(note, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Width = 90;
        close.DialogResult = DialogResult.Cancel;
        close.TabIndex = 7;
        var save = ProductUi.Button("btnSave", "Lưu nhà cung cấp", primary: true);
        save.Width = 175;
        save.Margin = Padding.Empty;
        save.Enabled = _services is not null;
        save.TabIndex = 8;
        save.AccessibleDescription = "Chưa sẵn sàng: chức năng lưu nhà cung cấp đang được hoàn thiện";
        save.Click += async (_, _) => await SaveAsync();
        actions.Controls.Add(close);
        actions.Controls.Add(save);
        CancelButton = close;
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private void LoadSupplier(SupplierDto supplier)
    {
        FindControl<AntdUI.Input>("txtSupplierCode").Text = supplier.SupplierCode;
        FindControl<AntdUI.Input>("txtSupplierName").Text = supplier.SupplierName;
        FindControl<AntdUI.Input>("txtTaxCode").Text = supplier.TaxCode ?? string.Empty;
        FindControl<AntdUI.Input>("txtPhone").Text = supplier.Phone ?? string.Empty;
        FindControl<AntdUI.Input>("txtEmail").Text = supplier.Email ?? string.Empty;
        FindControl<AntdUI.Input>("txtAddress").Text = supplier.Address ?? string.Empty;
        FindControl<CheckBox>("chkIsActive").Checked = supplier.IsActive;
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
            var request = new SupplierRequestDto
            {
                SupplierCode = FindControl<AntdUI.Input>("txtSupplierCode").Text,
                SupplierName = FindControl<AntdUI.Input>("txtSupplierName").Text,
                TaxCode = FindControl<AntdUI.Input>("txtTaxCode").Text,
                Phone = FindControl<AntdUI.Input>("txtPhone").Text,
                Email = FindControl<AntdUI.Input>("txtEmail").Text,
                Address = FindControl<AntdUI.Input>("txtAddress").Text,
                IsActive = FindControl<CheckBox>("chkIsActive").Checked
            };
            await _services.RunAsync(() => _supplierId.HasValue
                ? _services.Suppliers.UpdateAsync(_services.Session, _supplierId.Value, request)
                : _services.Suppliers.CreateAsync(_services.Session, request));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu nhà cung cấp", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _saving = false; if (!IsDisposed) save.Enabled = true; }
    }

    private T FindControl<T>(string name) where T : Control => Controls.Find(name, true).OfType<T>().FirstOrDefault() ?? throw new InvalidOperationException($"Không tìm thấy control {name}.");

    private static TableLayoutPanel Section(string caption, int lastInputHeight)
    {
        var section = ProductUi.Layout(1, 2);
        section.Margin = new Padding(0, 0, 0, 14);
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        // [COLOR CONFIG]: nền trắng của nhóm thông tin.
        section.BackColor = AppColors.Surface;
        var title = ProductUi.Label(caption, 12, true);
        title.Padding = new Padding(16, 0, 0, 0);
        // [COLOR CONFIG]: nền nhạt của tiêu đề nhóm.
        title.BackColor = AppColors.Workspace;
        section.Controls.Add(title, 0, 0);
        var fields = ProductUi.Layout(2, 4);
        fields.Padding = new Padding(16, 8, 4, 10);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, lastInputHeight));
        section.Controls.Add(fields, 0, 1);
        return fields;
    }

    private static void AddField(TableLayoutPanel fields, int column, int row, string caption,
        string name, int maxLength, int tabIndex, bool span = false, bool multiline = false)
    {
        var label = ProductUi.Label(caption, 10, true);
        var input = ProductUi.Input(name);
        if (maxLength > 0) input.MaxLength = maxLength;
        input.Multiline = multiline;
        input.TabIndex = tabIndex;
        input.AccessibleName = caption.Replace(" *", "");
        fields.Controls.Add(label, column, row * 2);
        fields.Controls.Add(input, column, row * 2 + 1);
        if (span)
        {
            fields.SetColumnSpan(label, 2);
            fields.SetColumnSpan(input, 2);
        }
    }
}
