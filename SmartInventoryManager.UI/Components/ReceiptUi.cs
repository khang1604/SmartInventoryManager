using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class ReceiptUi
{
    public static void Build(Form form, bool import)
    {
        var root = PreviewFormUi.Root(form, import ? "Phiếu nhập kho" : "Phiếu xuất kho", new Size(960, 700));
        form.MinimumSize = new Size(760, 600);
        var body = ProductUi.Layout(1, 4);
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var fields = PreviewFormUi.Fields(2);
        PreviewFormUi.Field(fields, 0, 0, "txtReceiptCode", "Mã phiếu *", 50);
        PreviewFormUi.Field(fields, 1, 0, "cboWarehouse", "Kho *", placeholder: "Chọn kho", unavailable: true);
        if (import)
            PreviewFormUi.Field(fields, 0, 1, "cboSupplier", "Nhà cung cấp *", placeholder: "Chọn nhà cung cấp", unavailable: true);
        else
            PreviewFormUi.Field(fields, 0, 1, "txtNote", "Ghi chú", 500);
        fields.Controls.Add(ProductUi.Label(import ? "Ngày nhập *" : "Ngày xuất *", 10, true), 1, 2);
        var date = new DateTimePicker
        {
            Name = "dtpReceiptDate", Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill, Margin = new Padding(3, 8, 15, 8), TabIndex = 3,
            AccessibleName = import ? "Ngày nhập" : "Ngày xuất"
        };
        fields.Controls.Add(date, 1, 3);
        body.Controls.Add(fields, 0, 0);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        var heading = ProductUi.Label("Chi tiết hàng hóa", 12, true);
        heading.Dock = DockStyle.None;
        heading.Size = new Size(180, 44);
        toolbar.Controls.Add(heading);
        toolbar.Controls.Add(WorkflowUi.Pending("btnAddLine", "Thêm dòng", "plus-lg"));
        toolbar.Controls.Add(WorkflowUi.Pending("btnRemoveLine", "Xóa dòng", "dash-circle"));
        if (import)
        {
            var note = ProductUi.Input("txtNote", "Ghi chú phiếu");
            note.MaxLength = 500;
            note.Dock = DockStyle.None;
            note.Size = new Size(300, 44);
            note.TabIndex = 4;
            toolbar.Controls.Add(note);
        }
        body.Controls.Add(toolbar, 0, 1);
        toolbar.SizeChanged += (_, _) => body.RowStyles[1].Height =
            toolbar.Controls.Cast<Control>().Sum(control => control.Width + control.Margin.Horizontal) > toolbar.ClientSize.Width ? 72 : 54;
        body.Controls.Add(WorkflowUi.TableWithEmpty("dgvReceiptLines",
            ["Mã sản phẩm", "Tên sản phẩm", "Đơn vị", "Số lượng", "Đơn giá (đ)", "Thành tiền (đ)"], "boxes"), 0, 2);
        var totals = ProductUi.Label("Tổng tiền: —    •    Chưa chọn hàng hóa; chức năng lưu và hoàn tất phiếu chưa sẵn sàng", 10, true);
        // [COLOR CONFIG]: chữ phụ của tổng tiền chưa có dữ liệu.
        totals.ForeColor = AppColors.Muted;
        body.Controls.Add(totals, 0, 3);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        body.Dock = DockStyle.Top;
        body.Height = 510;
        scroll.Controls.Add(body);
        scroll.SizeChanged += (_, _) => body.Height = Math.Max(510, scroll.ClientSize.Height);
        root.Controls.Add(scroll, 0, 1);
    }
}
