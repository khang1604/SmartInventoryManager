using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class ReceiptRejectionHistoryForm : Form
{
    private readonly UiServices _services;
    private readonly bool _import;
    private readonly int _receiptId;
    private readonly bool _statusHistory;
    private int _page = 1;
    private readonly DataGridView _grid;
    private readonly Label _message = ProductUi.Label("Đang tải…");
    private readonly AntdUI.Button _previous = ProductUi.Button("btnPrevious", "Trang trước");
    private readonly AntdUI.Button _next = ProductUi.Button("btnNext", "Trang sau");
    private bool _busy;

    // Khởi tạo form hiển thị lịch sử từ chối hoặc chuyển trạng thái phiếu.
    public ReceiptRejectionHistoryForm(UiServices services, bool import, int receiptId, bool statusHistory = false)
    {
        _services = services; _import = import; _receiptId = receiptId;
        _statusHistory = statusHistory;
        _grid = statusHistory
            ? LiveUi.Grid("dgvReceiptStatusHistory", [("FromStatus", "Trạng thái trước"), ("ToStatus", "Trạng thái sau"), ("ActorName", "Người thực hiện"), ("ChangedAt", "Thời điểm"), ("Reason", "Lý do")])
            : LiveUi.Grid("dgvRejectionHistory", [("ActorName", "Người từ chối"), ("RejectedAt", "Thời điểm"), ("Reason", "Lý do")]);
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name is "FromStatus" or "ToStatus" && e.Value is string value)
                e.Value = LiveUi.StatusText(value);
        };
        LiveUi.Column(_grid, statusHistory ? "ChangedAt" : "RejectedAt").DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
        WorkflowUi.StyleForm(this); Text = statusHistory ? "Lịch sử xử lý phiếu" : "Lịch sử từ chối phiếu"; ClientSize = new Size(950, 550); MinimumSize = new Size(650, 400); StartPosition = FormStartPosition.CenterParent;
        var root = ProductUi.Layout(1, 3); root.Padding = new Padding(16);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.Controls.Add(_message, 0, 0); root.Controls.Add(LiveUi.TableFrame(_grid), 0, 1);
        var actions = LiveUi.Toolbar(); actions.Controls.AddRange([_previous, _next]);
        var close = ProductUi.Button("btnClose", "Đóng"); close.DialogResult = DialogResult.Cancel; CancelButton = close; actions.Controls.Add(close); root.Controls.Add(actions, 0, 2); Controls.Add(root);
        _previous.Click += async (_, _) => { if (_busy || _page <= 1) return; _page--; await LoadAsync(); };
        _next.Click += async (_, _) => { if (_busy || !_next.Enabled) return; _page++; await LoadAsync(); };
        LiveUi.Column(_grid, "Reason").FillWeight = 200;
        UiDetails.AddColumn(_grid, true);
        Shown += async (_, _) => await LoadAsync();
    }
    // Tải lịch sử từ chối hoặc chuyển trạng thái phiếu theo phân trang.
    private async Task LoadAsync()
    {
        _busy = true; _previous.Enabled = _next.Enabled = false;
        try
        {
            int total;
            if (_statusHistory)
            {
                var data = await _services.RunAsync(() => _services.ReceiptAudits.GetStatusChangesAsync(_services.Session, _import, _receiptId, _page));
                _grid.DataSource = data.Rows.ToList(); total = data.Total;
            }
            else
            {
                var data = await _services.RunAsync(() => _services.ReceiptAudits.GetRejectionsAsync(_services.Session, _import, _receiptId, _page));
                _grid.DataSource = data.Rows.ToList(); total = data.Total;
            }
            _message.Text = total == 0 ? "Chưa có lịch sử xử lý."
                : $"{total} lần xử lý • Trang {_page} • 20 dòng/trang";
            _previous.Enabled = _page > 1; _next.Enabled = _page * 20 < total;
        }
        catch (Exception ex) { _grid.DataSource = null; _message.Text = LiveUi.LoadError(ex); }
        finally { _busy = false; }
    }
}
