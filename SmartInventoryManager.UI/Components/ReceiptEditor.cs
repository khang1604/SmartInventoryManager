using System.Globalization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class ReceiptEditor : UserControl
{
    private readonly Form _owner;
    private readonly UiServices _services;
    private readonly bool _import;
    private readonly bool _requestedEdit;
    private int? _id;
    private string _status = "DRAFT";
    private int _createdBy;
    private bool _busy;
    private bool _editable;
    private bool _dirty;
    private bool _binding;
    private bool _loaded;
    private readonly TextBox _code = new() { Name = "txtReceiptCode", Width = 220, MaxLength = 50 };
    private readonly TextBox _note = new() { Name = "txtNote", Width = 380, MaxLength = 500 };
    private readonly ComboBox _warehouse = LiveUi.Choice("cboWarehouse", "Kho");
    private readonly ComboBox _supplier = LiveUi.Choice("cboSupplier", "Nhà cung cấp");
    private readonly Label _message = ProductUi.Label("Đang tải biểu mẫu…");
    private readonly Label _total = ProductUi.Label("Tổng tiền xem trước: 0 đ", 12, true);
    private readonly DataGridView _lines = LiveUi.Grid("dgvReceiptLines");
    private readonly FlowLayoutPanel _fields = LiveUi.Toolbar();
    private readonly FlowLayoutPanel _actions = LiveUi.Toolbar();
    private readonly AntdUI.Button _save = ProductUi.Button("btnSave", "Lưu nháp", primary: true);
    private readonly AntdUI.Button _add = ProductUi.Button("btnAddLine", "Thêm dòng", "plus-lg");
    private readonly AntdUI.Button _remove = ProductUi.Button("btnRemoveLine", "Xóa dòng", "dash-circle");
    private readonly AntdUI.Button _reload = ProductUi.Button("btnReload", "Làm mới", "arrow-clockwise");
    private readonly ErrorProvider _errors;
    private IReadOnlyList<ProductLookupDto> _products = Array.Empty<ProductLookupDto>();
    private IReadOnlyDictionary<int, int>? _stock;

    public ReceiptEditor(Form owner, UiServices services, bool import, int? id, bool edit)
    {
        _owner = owner; _services = services; _import = import; _id = id; _requestedEdit = edit;
        _errors = new ErrorProvider(owner) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        WorkflowUi.StyleForm(owner);
        owner.Text = "VioWare - " + (import ? "Phiếu nhập" : "Phiếu xuất");
        owner.ClientSize = new Size(1040, 700);
        owner.MinimumSize = new Size(800, 600);
        owner.StartPosition = FormStartPosition.CenterParent;
        Dock = DockStyle.Fill;
        var root = ProductUi.Layout(1, 6);
        root.Padding = new Padding(20, 14, 20, 14);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.Controls.Add(ProductUi.Label(import ? "Phiếu nhập kho" : "Phiếu xuất kho", 18, true), 0, 0);
        _fields.Controls.AddRange([LiveUi.Field("Mã phiếu (tự động)", _code), LiveUi.Field("Kho *", _warehouse)]);
        if (import) _fields.Controls.Add(LiveUi.Field("Nhà cung cấp *", _supplier));
        _fields.Controls.Add(LiveUi.Field("Ghi chú", _note));
        root.Controls.Add(_fields, 0, 1);
        var toolbar = LiveUi.Toolbar();
        toolbar.Controls.AddRange([_add, _remove, _reload]);
        var hint = ProductUi.Label("Có thể lưu nháp chưa có dòng hàng • Số lượng > 0 • Đơn giá ≥ 0");
        hint.Dock = DockStyle.None; hint.Size = new Size(520, 44);
        toolbar.Controls.Add(hint);
        root.Controls.Add(toolbar, 0, 2);
        _lines.ReadOnly = false;
        _lines.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "ProductId", HeaderText = "Sản phẩm *", MinimumWidth = 200,
            DisplayMember = nameof(LookupItemDto.Name), ValueMember = nameof(LookupItemDto.Id),
            FlatStyle = FlatStyle.Flat
        });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "Đơn vị", ReadOnly = true, MinimumWidth = 80 });
        if (!import) _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "Tồn hiện tại", ReadOnly = true, MinimumWidth = 100 });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Số lượng *", MinimumWidth = 100 });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Đơn giá (đ) *", MinimumWidth = 120 });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineTotal", HeaderText = "Thành tiền (đ)", ReadOnly = true, MinimumWidth = 140 });
        foreach (var column in new[] { "Quantity", "UnitPrice", "LineTotal" })
        {
            LiveUi.Column(_lines, column).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            LiveUi.Column(_lines, column).DefaultCellStyle.Format = column == "Quantity" ? "N0" : "N2";
        }
        _lines.DataError += (_, e) => { e.ThrowException = false; _message.Text = "Kiểm tra sản phẩm, số lượng nguyên và đơn giá hợp lệ."; };
        _lines.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_lines.IsCurrentCellDirty && _lines.CurrentCell is DataGridViewComboBoxCell)
                _lines.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _lines.CellValueChanged += (_, e) =>
        {
            if (_binding || e.RowIndex < 0) return;
            _dirty = true;
            if (_lines.Columns[e.ColumnIndex].Name == "ProductId")
            {
                var row = _lines.Rows[e.RowIndex];
                var product = _products.FirstOrDefault(p => p.ProductId == Convert.ToInt32(row.Cells["ProductId"].Value));
                if (product is not null)
                {
                    _binding = true;
                    row.Cells["Unit"].Value = product.Unit;
                    row.Cells["UnitPrice"].Value = _import ? product.PurchasePrice : product.SellingPrice;
                    if (!_import) row.Cells["CurrentStock"].Value = _stock is null ? null : _stock.GetValueOrDefault(product.ProductId);
                    _binding = false;
                }
            }
            UpdateTotal();
        };
        _code.TextChanged += (_, _) => { if (!_binding) _dirty = true; };
        _note.TextChanged += (_, _) => { if (!_binding) _dirty = true; };
        _warehouse.SelectedIndexChanged += async (_, _) =>
        {
            if (_binding || _busy) return;
            _dirty = true;
            try
            {
                await UpdatePermissionAsync();
                if (!_import) await LoadStockAsync();
            }
            catch (Exception ex) { if (!IsDisposed) _message.Text = LiveUi.Error(ex); }
        };
        _supplier.SelectedIndexChanged += (_, _) => { if (!_binding) _dirty = true; };
        _add.Click += (_, _) => { if (_editable && !_busy) { _lines.Rows.Add(); _dirty = true; } };
        _remove.Click += (_, _) =>
        {
            if (_editable && !_busy && _lines.CurrentRow is { } row)
            { _lines.Rows.Remove(row); _dirty = true; UpdateTotal(); }
        };
        _save.Click += async (_, _) => await SaveAsync();
        _reload.Click += async (_, _) => { if (!_busy && !_loaded) await LoadAsync(); };
        var close = ProductUi.Button("btnClose", "Đóng");
        close.Click += (_, _) => owner.Close();
        owner.CancelButton = close;
        // Enter trong bảng không thực hiện lưu hoặc hoàn tất phiếu.
        _actions.Controls.AddRange([_save, close]);
        root.Controls.Add(LiveUi.TableFrame(_lines), 0, 3);
        root.Controls.Add(_total, 0, 4);
        var footer = ProductUi.Layout(2, 1);
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        footer.Controls.Add(_message, 0, 0);
        footer.Controls.Add(_actions, 1, 0);
        root.Controls.Add(footer, 0, 5);
        Controls.Add(root);
        owner.FormClosing += (_, e) =>
        {
            if (_busy) { e.Cancel = true; return; }
            if (_dirty && MessageBox.Show(owner, "Đóng và bỏ thay đổi chưa lưu?", "Chưa lưu phiếu",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK)
                e.Cancel = true;
        };
        Load += async (_, _) => await LoadAsync();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _fields.Enabled = _lines.Enabled = !busy && _loaded;
        _actions.Enabled = !busy;
        _reload.Enabled = !busy && !_loaded;
        _reload.Visible = !_loaded;
        _add.Enabled = _remove.Enabled = _save.Enabled = !busy && _editable;
    }

    private async Task LoadAsync()
    {
        SetBusy(true);
        _binding = true;
        _lines.Rows.Clear();
        try
        {
            var warehouses = await _services.RunAsync(() => _services.Warehouses.GetActiveAsync(_services.Session));
            var suppliers = _import ? await _services.RunAsync(() => _services.Suppliers.GetActiveAsync(_services.Session)) : Array.Empty<LookupItemDto>();
            _products = await _services.RunAsync(() => _services.Products.GetActiveAsync(_services.Session));
            IReadOnlyList<ReceiptDetailViewDto> details = Array.Empty<ReceiptDetailViewDto>();
            int warehouseId = !_id.HasValue && _services.Authorization.IsStaff(_services.Session)
                ? _services.Session.CurrentWarehouseId ?? 0 : 0, supplierId = 0;
            string warehouseName = "", supplierName = "";
            if (_id.HasValue)
            {
                if (_import)
                {
                    var receipt = await _services.RunAsync(() => _services.Imports.GetByIdAsync(_services.Session, _id.Value))
                        ?? throw new KeyNotFoundException();
                    _code.Text = receipt.ReceiptCode; _note.Text = receipt.Note;
                    warehouseId = receipt.WarehouseId; warehouseName = receipt.WarehouseName;
                    supplierId = receipt.SupplierId; supplierName = receipt.SupplierName;
                    _status = receipt.Status; _createdBy=receipt.CreatedBy; details = receipt.Details;
                }
                else
                {
                    var receipt = await _services.RunAsync(() => _services.Exports.GetByIdAsync(_services.Session, _id.Value))
                        ?? throw new KeyNotFoundException();
                    _code.Text = receipt.ReceiptCode; _note.Text = receipt.Note;
                    warehouseId = receipt.WarehouseId; warehouseName = receipt.WarehouseName;
                    _status = receipt.Status; _createdBy=receipt.CreatedBy; details = receipt.Details;
                }
            }
            BindLookup(_warehouse, warehouses, warehouseId, warehouseName, "Chọn kho");
            if (_import) BindLookup(_supplier, suppliers, supplierId, supplierName, "Chọn nhà cung cấp");
            var productOptions = _products.Select(p => new LookupItemDto(p.ProductId, p.ProductCode, p.ProductCode + " • " + p.ProductName)).ToList();
            foreach (var line in details.Where(d => productOptions.All(p => p.Id != d.ProductId)))
                productOptions.Add(new LookupItemDto(line.ProductId, line.ProductCode, line.ProductCode + " • " + line.ProductName));
            ((DataGridViewComboBoxColumn)LiveUi.Column(_lines, "ProductId")).DataSource = productOptions;
            foreach (var line in details)
            {
                var row = _lines.Rows[_lines.Rows.Add()];
                row.Cells["ProductId"].Value = line.ProductId;
                row.Cells["Unit"].Value = line.Unit;
                row.Cells["Quantity"].Value = line.Quantity;
                row.Cells["UnitPrice"].Value = line.UnitPrice;
                row.Cells["LineTotal"].Value = line.LineTotal;
            }
            await UpdatePermissionAsync();
            if (!_import) await LoadStockAsync();
            UpdateTotal();
            _message.Text = PermissionHint();
            _dirty = false;
            _loaded = true;
        }
        catch (Exception ex) { _editable = false; _message.Text = LiveUi.LoadError(ex); }
        finally { _binding = false; SetBusy(false); }
    }

    private static void BindLookup(ComboBox control, IReadOnlyList<LookupItemDto> items, int id, string name, string placeholder)
    {
        var options = new List<LookupItemDto> { new(0, "", placeholder) };
        options.AddRange(items);
        if (id != 0 && options.All(x => x.Id != id)) options.Add(new LookupItemDto(id, "", name));
        control.DisplayMember = nameof(LookupItemDto.Name);
        control.ValueMember = nameof(LookupItemDto.Id);
        control.DataSource = options;
        control.SelectedValue = id;
    }

    private async Task LoadStockAsync()
    {
        var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
        _stock = null;
        foreach (DataGridViewRow row in _lines.Rows) row.Cells["CurrentStock"].Value = null;
        if (warehouseId == 0) return;
        var stock = await _services.RunAsync(() => _services.Inventory.GetByWarehouseAsync(_services.Session, warehouseId));
        if (IsDisposed || ((_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0) != warehouseId) return;
        _stock = stock.ToDictionary(item => item.ProductId, item => item.Quantity);
        foreach (DataGridViewRow row in _lines.Rows)
            if (row.Cells["ProductId"].Value is int productId) row.Cells["CurrentStock"].Value = _stock.GetValueOrDefault(productId);
    }

    private async Task UpdatePermissionAsync()
    {
        var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
        _editable = false;
        _save.Enabled = _add.Enabled = _remove.Enabled = false;
        _code.ReadOnly = true;
        _note.ReadOnly = _lines.ReadOnly = true;
        var allowed = warehouseId != 0 && await _services.RunAsync(() => _id.HasValue
            ? _services.Authorization.CanEditReceiptAsync(_services.Session, warehouseId, _createdBy, _status)
            : _services.Authorization.CanCreateDraftReceiptAsync(_services.Session, warehouseId));
        if (IsDisposed || ((_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0) != warehouseId) return;
        _editable = _requestedEdit && (_status is "DRAFT" or "REJECTED") && allowed;
        _code.ReadOnly = true;
        _note.ReadOnly = !_editable;
        _lines.ReadOnly = !_editable;
        // Cho phép chọn kho trước khi xác định quyền tạo mới; kho phiếu đã lưu không được đổi.
        _warehouse.Enabled = _requestedEdit && !_id.HasValue && !_services.Authorization.IsStaff(_services.Session);
        _supplier.Enabled = _editable;
        _save.Enabled = _add.Enabled = _remove.Enabled = _editable && !_busy;
        if (!_busy && _loaded) _message.Text = PermissionHint();
    }

    private string PermissionHint()
    {
        if (_requestedEdit && !_id.HasValue && ((_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0) == 0)
            return "Chọn kho để bắt đầu tạo phiếu nháp.";
        if (_editable) return $"{LiveUi.StatusText(_status)} • Nhập thông tin và lưu thay đổi.";
        if (!_requestedEdit || _status is not ("DRAFT" or "REJECTED"))
            return $"{LiveUi.StatusText(_status)} • Chỉ xem; phiếu không ở chế độ chỉnh sửa.";
        return "Chưa có quyền sửa phiếu tại kho đã chọn.";
    }

    private bool TryReadLines(out IReadOnlyList<ReceiptDetailDto> details, bool showErrors, bool requireLine)
    {
        var result = new List<ReceiptDetailDto>();
        var valid = true;
        foreach (DataGridViewRow row in _lines.Rows)
        {
            if (row.IsNewRow) continue;
            row.ErrorText = "";
            foreach (DataGridViewCell cell in row.Cells) cell.ErrorText = "";
            var id = row.Cells["ProductId"].Value is int value ? value : 0;
            var qtyOk = int.TryParse(Convert.ToString(row.Cells["Quantity"].Value, LiveUi.Culture), NumberStyles.Integer | NumberStyles.AllowThousands, LiveUi.Culture, out var quantity) && quantity > 0;
            var priceOk = decimal.TryParse(Convert.ToString(row.Cells["UnitPrice"].Value, LiveUi.Culture), NumberStyles.Number, LiveUi.Culture, out var price) && price >= 0;
            if (id == 0 || !qtyOk || !priceOk || result.Any(d => d.ProductId == id))
            {
                valid = false;
                if (showErrors)
                {
                    if (id == 0 || result.Any(d => d.ProductId == id)) row.Cells["ProductId"].ErrorText = "Chọn sản phẩm không trùng.";
                    if (!qtyOk) row.Cells["Quantity"].ErrorText = "Nhập số lượng nguyên lớn hơn 0.";
                    if (!priceOk) row.Cells["UnitPrice"].ErrorText = "Nhập đơn giá không âm.";
                }
            }
            else result.Add(new ReceiptDetailDto(id, quantity, price));
        }
        details = result;
        return valid && (!requireLine || result.Count > 0);
    }

    private void UpdateTotal()
    {
        decimal total = 0;
        _binding = true;
        try
        {
            foreach (DataGridViewRow row in _lines.Rows)
            {
                if (row.IsNewRow) continue;
                var qtyOk = int.TryParse(Convert.ToString(row.Cells["Quantity"].Value, LiveUi.Culture), NumberStyles.Integer | NumberStyles.AllowThousands, LiveUi.Culture, out var quantity);
                var priceOk = decimal.TryParse(Convert.ToString(row.Cells["UnitPrice"].Value, LiveUi.Culture), NumberStyles.Number, LiveUi.Culture, out var price);
                var amount = qtyOk && priceOk && quantity > 0 && price >= 0 ? checked(quantity * price) : 0;
                row.Cells["LineTotal"].Value = amount;
                total = checked(total + amount);
            }
            _total.Text = $"Tổng tiền xem trước: {total.ToString("N2", LiveUi.Culture)} đ";
        }
        catch (OverflowException) { _total.Text = "Giá trị tiền vượt giới hạn. Kiểm tra lại đơn giá và số lượng."; }
        finally { _binding = false; }
    }

    private async Task SaveAsync()
    {
        if (_busy || !_editable) return;
        _lines.EndEdit();
        _errors.Clear();
        var warehouseId = (_warehouse.SelectedItem as LookupItemDto)?.Id ?? 0;
        var supplierId = (_supplier.SelectedItem as LookupItemDto)?.Id ?? 0;
        if (warehouseId == 0) _errors.SetError(_warehouse, "Chọn kho.");
        if (_import && supplierId == 0) _errors.SetError(_supplier, "Chọn nhà cung cấp.");
        var linesValid = TryReadLines(out var details, true, requireLine: false);
        if (warehouseId == 0 || (_import && supplierId == 0) || !linesValid)
        {
            _message.Text = "Kiểm tra các trường có lỗi.";
            if (warehouseId == 0) _warehouse.Focus();
            else if (_import && supplierId == 0) _supplier.Focus();
            else _lines.Focus();
            return;
        }
        SetBusy(true);
        _message.Text = "Đang lưu phiếu nháp…";
        try
        {
            if (_import)
            {
                var request = new ImportReceiptDto { WarehouseId = warehouseId, SupplierId = supplierId, Note = _note.Text, Details = details };
                if (_id.HasValue) await _services.RunAsync(() => _services.Imports.UpdateDraftAsync(_services.Session, _id.Value, request));
                else _id = await _services.RunAsync(() => _services.Imports.CreateDraftAsync(_services.Session, request));
                if (_id.HasValue)
                {
                    var saved = await _services.RunAsync(() => _services.Imports.GetByIdAsync(_services.Session, _id.Value));
                    if (saved is not null) _code.Text = saved.ReceiptCode;
                }
            }
            else
            {
                var request = new ExportReceiptDto { WarehouseId = warehouseId, Note = _note.Text, Details = details };
                if (_id.HasValue) await _services.RunAsync(() => _services.Exports.UpdateDraftAsync(_services.Session, _id.Value, request));
                else _id = await _services.RunAsync(() => _services.Exports.CreateDraftAsync(_services.Session, request));
                if (_id.HasValue)
                {
                    var saved = await _services.RunAsync(() => _services.Exports.GetByIdAsync(_services.Session, _id.Value));
                    if (saved is not null) _code.Text = saved.ReceiptCode;
                }
            }
            _dirty = false;
            _message.Text = "Đã lưu phiếu nháp. Đóng để trở lại danh sách và hoàn tất khi cần.";
            _warehouse.Enabled = false;
        }
        catch (Exception ex) { _message.Text = LiveUi.Error(ex); }
        finally { SetBusy(false); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _errors.Dispose();
        base.Dispose(disposing);
    }
}
