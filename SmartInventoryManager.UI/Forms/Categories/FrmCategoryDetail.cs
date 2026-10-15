using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Forms.Categories;

public sealed class FrmCategoryDetail : Form
{
    private readonly UiServices? _services;
    private readonly int? _categoryId;

    public FrmCategoryDetail() : this((UiServices?)null, (int?)null)
    {
    }

    public FrmCategoryDetail(UiServices services, CategoryDto? category = null) : this(services, category?.CategoryId)
    {
        _services = services;
        _categoryId = category?.CategoryId;
        Shown += async (_, _) =>
        {
            if (category is not null) LoadCategory(category);
        };
    }

    private FrmCategoryDetail(UiServices? services, int? categoryId)
    {
        _services = services;
        _categoryId = categoryId;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 10F);
        Text = "VioWare - Thông tin danh mục";
        Shown += (_, _) => WorkflowUi.FitToWorkingArea(this);
        ClientSize = new Size(520, 620);
        MinimumSize = new Size(480, 500);
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
        heading.Controls.Add(ProductUi.Label("Thông tin danh mục", 18, true), 0, 0);
        var instruction = ProductUi.Label("Các trường có dấu * là bắt buộc");
        // [COLOR CONFIG]: màu chữ phụ của hướng dẫn.
        instruction.ForeColor = AppColors.Muted;
        heading.Controls.Add(instruction, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var fields = ProductUi.Layout(1, 5);
        fields.Padding = new Padding(16);
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        // [COLOR CONFIG]: nền trắng của nhóm trường thông tin.
        fields.BackColor = AppColors.Surface;
        fields.Controls.Add(ProductUi.Label("Tên danh mục *", 10, true), 0, 0);
        var name = ProductUi.Input("txtCategoryName");
        name.Margin = Padding.Empty;
        name.MaxLength = 100;
        name.TabIndex = 0;
        name.AccessibleName = "Tên danh mục, bắt buộc";
        fields.Controls.Add(name, 0, 1);
        fields.Controls.Add(ProductUi.Label("Mô tả", 10, true), 0, 2);
        var description = ProductUi.Input("txtDescription");
        description.Multiline = true;
        description.MaxLength = 255;
        description.Margin = new Padding(0, 0, 0, 12);
        description.TabIndex = 1;
        description.AccessibleName = "Mô tả danh mục";
        fields.Controls.Add(description, 0, 3);
        fields.Controls.Add(new CheckBox
        {
            Name = "chkIsActive", Text = "Đang hoạt động", Checked = true,
            Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0), TabIndex = 2,
            // [COLOR CONFIG]: nền mint và chữ tối của trạng thái hoạt động.
            BackColor = AppColors.Mint, ForeColor = AppColors.Text
        }, 0, 4);
        root.Controls.Add(fields, 0, 1);

        var footer = ProductUi.Layout(2, 1);
        footer.Padding = new Padding(0, 14, 0, 0);
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 246));
        var note = ProductUi.Label("Các trường có dấu * là bắt buộc", 8.5F);
        // [COLOR CONFIG]: ghi chú phụ về trạng thái biểu mẫu.
        note.ForeColor = AppColors.Muted;
        footer.Controls.Add(note, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Width = 90;
        close.DialogResult = DialogResult.Cancel;
        close.TabIndex = 3;
        var save = ProductUi.Button("btnSave", "Lưu danh mục", primary: true);
        save.Width = 145;
        save.Margin = Padding.Empty;
        save.Enabled = _services is not null;
        save.TabIndex = 4;
        save.AccessibleDescription = "Chưa sẵn sàng: chức năng lưu danh mục đang được hoàn thiện";
        save.Click += async (_, _) => await SaveAsync();
        actions.Controls.Add(close);
        actions.Controls.Add(save);
        CancelButton = close;
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private void LoadCategory(CategoryDto category)
    {
        FindControl<AntdUI.Input>("txtCategoryName").Text = category.CategoryName;
        FindControl<AntdUI.Input>("txtDescription").Text = category.Description ?? string.Empty;
        FindControl<CheckBox>("chkIsActive").Checked = category.IsActive;
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
            var request = new CategoryRequestDto { CategoryName = FindControl<AntdUI.Input>("txtCategoryName").Text, Description = FindControl<AntdUI.Input>("txtDescription").Text, IsActive = FindControl<CheckBox>("chkIsActive").Checked };
            await _services.RunAsync(() => _categoryId.HasValue ? _services.Categories.UpdateAsync(_services.Session, _categoryId.Value, request) : _services.Categories.CreateAsync(_services.Session, request));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu danh mục", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _saving = false; if (!IsDisposed) save.Enabled = true; }
    }

    private T FindControl<T>(string name) where T : Control => Controls.Find(name, true).OfType<T>().FirstOrDefault() ?? throw new InvalidOperationException($"Không tìm thấy control {name}.");
}
