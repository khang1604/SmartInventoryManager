using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.UI.Components;

internal sealed class ReceiptRejectionHistoryForm : Form
{
    private readonly UiServices _services;
    private readonly bool _import;
    private readonly int _receiptId;
    private int _page = 1;
    private readonly DataGridView _grid = LiveUi.Grid("dgvRejectionHistory", [("ActorName", "Người từ chối"), ("RejectedAt", "Thời điểm"), ("Reason", "Lý do")]);
    private readonly Label _message = ProductUi.Label("Đang tải…");
    private readonly AntdUI.Button _previous = ProductUi.Button("btnPrevious", "Trang trước");
    private readonly AntdUI.Button _next = ProductUi.Button("btnNext", "Trang sau");
    private bool _busy;

    public ReceiptRejectionHistoryForm(UiServices services, bool import, int receiptId)
    {
        _services = services; _import = import; _receiptId = receiptId;
        WorkflowUi.StyleForm(this); Text = "Lịch sử từ chối phiếu"; ClientSize = new Size(850, 500); MinimumSize = new Size(650, 400); StartPosition = FormStartPosition.CenterParent;
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
    private async Task LoadAsync()
    {
        _busy = true; _previous.Enabled = _next.Enabled = false;
        try
        {
            var data = await _services.RunAsync(() => _services.ReceiptAudits.GetRejectionsAsync(_services.Session, _import, _receiptId, _page));
            _grid.DataSource = data.Rows.ToList(); _message.Text = $"{data.Total} lần từ chối • Trang {_page} • 20 dòng/trang";
            _previous.Enabled = _page > 1; _next.Enabled = _page * data.PageSize < data.Total;
        }
        catch (Exception ex) { _grid.DataSource = null; _message.Text = LiveUi.LoadError(ex); }
        finally { _busy = false; }
    }
}
