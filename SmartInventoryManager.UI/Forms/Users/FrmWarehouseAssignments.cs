using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmWarehouseAssignments : Form
{
    public FrmWarehouseAssignments(UiServices services, UserListItemDto user)
    {
        Text = "Phân kho — " + user.Username;
        Shown += (_, _) => WorkflowUi.FitToWorkingArea(this);
        MinimumSize = new Size(620, 500);
        Size = new Size(820, 600); StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        var choice = LiveUi.Choice("cboAssignmentWarehouse", "Kho được phân công");
        choice.DisplayMember = "Name"; choice.ValueMember = "Id";
        choice.DataSource = new List<LookupItemDto> { new(0, "", "Chọn kho") };
        var page = new DataListPage<WarehouseAssignmentDto>(services, "Phân kho của " + user.Username,
            [("WarehouseName", "Kho"), ("IsActive", "Trạng thái")],
            (_, _, _, _) => services.Assignments.GetAsync(services.Session, user.UserId));
        page.AddFilter("Kho", choice);
        page.Action("Phân kho / kích hoạt lại", async () =>
        {
            if (choice.SelectedItem is not LookupItemDto { Id: > 0 } selected)
                throw new ArgumentException("Vui lòng chọn kho.");
            await services.RunAsync(() => services.Assignments.SetActiveAsync(services.Session, user.UserId, selected.Id, true));
            await page.RefreshAsync();
        }, "plus-lg");
        page.SelectedAction("Đổi trạng thái", async (row, _) =>
        {
            if (MessageBox.Show(this, row.IsActive ? "Ngừng phân kho này? Lịch sử được giữ nguyên." : "Kích hoạt lại phân kho này?",
                "Phân kho", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            await services.RunAsync(() => services.Assignments.SetActiveAsync(services.Session, user.UserId, row.WarehouseId, !row.IsActive));
        }, "pencil");
        Controls.Add(page);
        Shown += async (_, _) =>
        {
            try
            {
                var rows = await services.RunAsync(() => services.Warehouses.GetAllAsync(services.Session));
                choice.DataSource = new[] { new LookupItemDto(0, "", "Chọn kho") }
                    .Concat(rows.Where(row => row.IsActive).Select(row => new LookupItemDto(row.WarehouseId, row.WarehouseCode, row.WarehouseName))).ToList();
            }
            catch (Exception ex) { MessageBox.Show(this, LiveUi.LoadError(ex), "Phân kho", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
    }
}
