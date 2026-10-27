using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

// Search is local to the authorized lookup; typing never issues a database query.
internal sealed class SearchableLookupComboBox : ComboBox
{
    private List<LookupItemDto> _options = [];
    private bool _binding;
    public bool IsSearchPending { get; private set; }
    public event EventHandler? SearchPending;
    // Khởi tạo hộp chọn có khả năng tìm kiếm theo nội dung nhập.
    public SearchableLookupComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDown;
        AutoCompleteMode = AutoCompleteMode.None;
        FormattingEnabled = true;
    }
    // Định dạng nhãn hiển thị cho từng mục trong hộp chọn tìm kiếm.
    protected override void OnFormat(ListControlConvertEventArgs e)
    {
        base.OnFormat(e);
        if (e.ListItem is LookupItemDto item)
            e.Value = string.IsNullOrWhiteSpace(item.Code) ? item.Name : $"{item.Code} — {item.Name}";
    }
    // Cập nhật dữ liệu tra cứu khi nguồn của hộp chọn thay đổi.
    protected override void OnDataSourceChanged(EventArgs e)
    {
        if (!_binding && DataSource is IEnumerable<LookupItemDto> options) _options = options.ToList();
        base.OnDataSourceChanged(e);
    }
    // Đồng bộ trạng thái tìm kiếm khi lựa chọn trong hộp chọn thay đổi.
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        if (!_binding)
        {
            if (SelectedIndex >= 0) IsSearchPending = false;
            base.OnSelectedIndexChanged(e);
        }
    }
    // Lọc các lựa chọn theo nội dung người dùng đang nhập.
    protected override void OnTextUpdate(EventArgs e)
    {
        var text = Text;
        var caret = SelectionStart;
        _binding = true;
        try
        {
            DataSource = _options.Where(item => item.Code.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || item.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)).ToList();
            SelectedIndex = -1; // Free text never grants access to a warehouse.
            Text = text;
            if (Focused && Items.Count > 0) DroppedDown = true;
            SelectionStart = Math.Min(caret, Text.Length);
            SelectionLength = 0;
        }
        finally { _binding = false; }
        IsSearchPending = true;
        SearchPending?.Invoke(this, EventArgs.Empty);
        base.OnTextUpdate(e);
    }
    // Xóa từ khóa tìm kiếm và khôi phục danh sách lựa chọn.
    public void ResetSearch()
    {
        _binding = true;
        try { IsSearchPending = false; DataSource = _options.ToList(); SelectedIndex = Items.Count > 0 ? 0 : -1; }
        finally { _binding = false; }
        base.OnSelectedIndexChanged(EventArgs.Empty);
    }
}
