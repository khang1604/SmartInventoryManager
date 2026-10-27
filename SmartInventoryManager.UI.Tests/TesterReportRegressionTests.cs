using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI.Tests;

[TestClass]
public sealed class TesterReportRegressionTests
{
    // Kiểm tra tên vai trò giữ tiếng Việt khi thêm cột chi tiết và tải lại dữ liệu.
    [TestMethod]
    public void UserRolesStayTranslatedWhenDetailsColumnIsAddedAndDataRefreshes() => Sta(() =>
    {
        using var services = SyntheticServices();
        IReadOnlyList<UserListItemDto> rows = new[]
        {
            new UserListItemDto(1, "synthetic.admin", "Synthetic Admin", null, RoleNames.Admin, true, DateTime.Today),
            new UserListItemDto(2, "synthetic.manager", "Synthetic Manager", null, RoleNames.WarehouseManager, true, DateTime.Today),
            new UserListItemDto(3, "synthetic.staff", "Synthetic Staff", null, RoleNames.Staff, true, DateTime.Today)
        };
        using var page = new DataListPage<UserListItemDto>(services, "Synthetic users",
            [("Username", "Tên đăng nhập"), ("RoleName", "Vai trò")],
            (_, _, _, _) => Task.FromResult(rows), pageSize: () => 50);
        page.AddDetailsColumn();
        page.BindingContext = new BindingContext();
        page.CreateControl();
        var grid = (DataGridView)page.Controls.Find("dgvData", true).Single();
        grid.CreateControl();
        foreach (var pass in new[] { 1, 2 })
        {
            page.RefreshAsync().GetAwaiter().GetResult();
            Assert.AreEqual(3, grid.Rows.Count);
            var expected = new[] { "Quản trị viên", "Quản lý kho", "Nhân viên kho" };
            for (var i = 0; i < rows.Count; i++)
            {
                var cell = grid.Rows[i].Cells["RoleName"];
                Assert.AreEqual(rows[i].RoleName, cell.Value, "Formatting must not change the stored role.");
                Assert.AreEqual(expected[i], cell.FormattedValue, $"Role display failed after refresh {pass}.");
            }
        }
    });

    // Kiểm tra thông báo tra cứu rỗng dùng tiếng Việt và các khóa khác vẫn có nội dung dự phòng.
    [TestMethod]
    public void EmptyLookupMessageIsVietnameseAndUnrelatedKeysKeepFallback() => Sta(() =>
    {
        var previous = AntdUI.Localization.Provider;
        var language = AntdUI.Localization.CurrentLanguage;
        try
        {
            VietnameseUiLocalization.Apply();
            var provider = AntdUI.Localization.Provider;
            Assert.AreEqual("Không có dữ liệu phù hợp.", AntdUI.Localization.Get("NoData", "Untranslated"));
            Assert.AreEqual("Original fallback", AntdUI.Localization.Get("SyntheticUnknownKey", "Original fallback"));
            VietnameseUiLocalization.Apply();
            Assert.AreSame(provider, AntdUI.Localization.Provider);
        }
        finally
        {
            AntdUI.Localization.Provider = previous;
            AntdUI.Localization.SetLanguage(language);
        }
    });

    // Kiểm tra form phân kho mở được với biểu tượng hiện có và hiển thị danh tính nhân viên.
    [TestMethod]
    public void AssignmentDialogConstructsWithExistingIconsAndShowsIdentity() => Sta(() =>
    {
        using var services = SyntheticServices();
        using var form = new SmartInventoryManager.Forms.Users.FrmWarehouseAssignments(services,
            new(3, "synthetic.staff", "Synthetic Staff", null, RoleNames.Staff, true, DateTime.Today));
        Assert.AreEqual("Synthetic Staff (synthetic.staff)", form.Controls.Find("lblAssignmentUser", true).Single().Text);
        Assert.IsFalse(form.Controls.Find("btnAssignmentSave", true).Single().Enabled);
        Assert.IsInstanceOfType<AntdUI.SelectMultiple>(form.Controls.Find("cboAssignmentWarehouse", true).Single());
    });

    // Chạy thao tác giao diện trên luồng STA và chuyển tiếp lỗi về ca kiểm thử.
    private static void Sta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA); thread.IsBackground = true; thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(20)), "Synthetic UI test exceeded 20 seconds.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    // Mô phỏng thao tác bấm nút trong ca kiểm thử giao diện.
    private static void Click(Control button) => typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, [EventArgs.Empty]);
    // Mô phỏng nhập từ khóa và phát sự kiện tìm kiếm trên hộp chọn.
    private static void Type(SearchableLookupComboBox box, string text)
    {
        box.Text = text;
        typeof(SearchableLookupComboBox).GetMethod("OnTextUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(box, [EventArgs.Empty]);
    }
    // Tạo bộ service cùng context bộ nhớ cho ca kiểm thử giao diện.
    private static UiServices SyntheticServices()
    {
        var session = new UserSession(); session.SignIn(new(1, "SYNTHETIC_ADMIN", "Synthetic Admin", null, RoleNames.Admin));
        return new(session, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, () => { }, null!, null!, null!, null!);
    }
    // Tạo hộp chọn kho với danh sách dữ liệu minh họa phục vụ kiểm thử.
    private static SearchableLookupComboBox Picker() => new()
    {
        BindingContext = new BindingContext(), DisplayMember = "Name", ValueMember = "Id",
        DataSource = new List<LookupItemDto> { new(0, "", "All warehouses"), new(1, "HCM01", "Central Warehouse"), new(2, "HN02", "Northern Warehouse") }
    };

    // Kiểm tra tìm kho theo mã và tên ngay trên dữ liệu đã tải.
    [TestMethod]
    public void WarehouseSearchFindsCodeAndNameWithoutDatabaseCalls() => Sta(() =>
    {
        using var picker = Picker();
        Type(picker, "hcm"); Assert.AreEqual(1, picker.Items.Count); Assert.AreEqual(-1, picker.SelectedIndex);
        picker.ResetSearch(); Type(picker, "Northern"); Assert.AreEqual(1, picker.Items.Count);
        Assert.AreEqual(2, ((LookupItemDto)picker.Items[0]!).Id);
        Assert.AreEqual(-1, picker.SelectedIndex, "Typing must not silently select a warehouse.");
        picker.SelectedIndex = 0; Assert.AreEqual(2, picker.SelectedValue);
    });

    // Kiểm tra gõ tìm kho không truy vấn theo lựa chọn và xóa tìm kiếm khôi phục toàn bộ mục.
    [TestMethod]
    public void WarehouseSearchDoesNotTriggerSelectionQueriesAndResetRestoresAll() => Sta(() =>
    {
        using var picker = Picker(); var changes = 0;
        picker.SelectedIndexChanged += (_, _) => changes++;
        Type(picker, "no-match"); Assert.AreEqual(0, picker.Items.Count); Assert.AreEqual(0, changes);
        picker.ResetSearch(); Assert.AreEqual(3, picker.Items.Count); Assert.AreEqual(0, picker.SelectedValue); Assert.AreEqual(1, changes);
    });

    // Kiểm tra TextBox mật khẩu có thể hiện ký tự và che lại khi bị vô hiệu hóa.
    [TestMethod]
    public void TextBoxPasswordCanBeShownAndRemasksWhenDisabled() => Sta(() =>
    {
        using var input = new TextBox { Name = "SyntheticPassword", Text = "Synthetic-only-value" };
        using var wrapper = PasswordVisibility.Wrap(input);
        var eye = wrapper.Controls.Find("btnShowSyntheticPassword", true).Single();
        Assert.IsTrue(input.UseSystemPasswordChar); Click(eye); Assert.IsFalse(input.UseSystemPasswordChar);
        input.Enabled = false; Assert.IsTrue(input.UseSystemPasswordChar); Assert.IsFalse(eye.Enabled);
    });

    // Kiểm tra ô mật khẩu AntdUI đổi hiện, ẩn mà không thay đổi nội dung.
    [TestMethod]
    public void AntdPasswordCanBeShownAndHiddenWithoutChangingValue() => Sta(() =>
    {
        using var input = new AntdUI.Input { Name = "SyntheticPassword", Text = "Synthetic-only-value" };
        using var wrapper = PasswordVisibility.Wrap(input);
        var eye = wrapper.Controls.Find("btnShowSyntheticPassword", true).Single();
        Click(eye); Assert.IsFalse(input.UseSystemPasswordChar); Click(eye); Assert.IsTrue(input.UseSystemPasswordChar);
        Assert.AreEqual("Synthetic-only-value", input.Text);
    });

    // Kiểm tra lỗi trùng mã kho giải thích cả trường hợp mã thuộc kho đã ngừng hoạt động.
    [TestMethod]
    public void DuplicateWarehouseCodeExplainsInactiveCodeReservation()
    {
        StringAssert.Contains(LiveUi.Error(new InvalidOperationException("Mã kho đã tồn tại.")), "kho đang ngừng hoạt động");
        Assert.IsFalse(LiveUi.Error(new InvalidOperationException("Mã kho đã tồn tại.")).Contains("Chưa thực hiện"));
    }

    // Kiểm tra tải lại sau đổi trạng thái vẫn chọn đúng kho khi thứ tự danh sách thay đổi.
    [TestMethod]
    public void WarehouseStatusRefreshKeepsSameRowAfterSortChanges() => Sta(() =>
    {
        using var services = SyntheticServices();
        IReadOnlyList<WarehouseDto> rows = [new(1, "A", "Synthetic A", null, null, true), new(2, "B", "Synthetic B", null, null, true)];
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code"), ("IsActive", "Active")],
            (_, _, _, _) => Task.FromResult(rows), pageSize: () => 50).PreserveSelection(row => row.WarehouseId);
        page.BindingContext = new BindingContext(); page.CreateControl();
        var grid = (DataGridView)page.Controls.Find("dgvData", true).Single(); grid.CreateControl();
        page.RefreshAsync().GetAwaiter().GetResult(); grid.CurrentCell = grid.Rows[1].Cells[0];
        rows = [new(2, "B", "Synthetic B", null, null, false), new(1, "A", "Synthetic A", null, null, true)];
        page.RefreshAsync().GetAwaiter().GetResult();
        var selected = (WarehouseDto)grid.CurrentRow!.DataBoundItem!;
        Assert.AreEqual(2, selected.WarehouseId); Assert.IsFalse(selected.IsActive); Assert.AreEqual(2, grid.Rows.Count);
    });

    // Kiểm tra xóa bộ lọc đặt lại trạng thái bổ sung và chỉ tải dữ liệu một lần.
    [TestMethod]
    public void ClearFiltersResetsAdditionalStateAndQueriesOnlyOnce() => Sta(() =>
    {
        using var services = SyntheticServices(); var queries = 0;
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code")],
            (_, _, _, _) => { queries++; return Task.FromResult<IReadOnlyList<WarehouseDto>>([]); }, pageSize: () => 50);
        var state = new ComboBox(); state.Items.AddRange(["All", "Active", "Inactive"]); state.SelectedIndex = 1;
        page.AddFilter("State", state); state.SelectedIndexChanged += (_, _) => page.RefreshAsync().GetAwaiter().GetResult();
        var reset = page.Controls.Find("btn1", true).Single();
        Click(reset); Assert.AreEqual(0, state.SelectedIndex); Assert.AreEqual(1, queries);
    });
    // Kiểm tra thông báo lỗi giữ hướng dẫn cần thiết và ẩn tên tham số kỹ thuật.
    [TestMethod]
    public void ValidationKeepsUsefulMessageAndHidesParameterNames()
    {
        Assert.AreEqual("Tên kho là bắt buộc.", LiveUi.Error(new ArgumentException("Tên kho là bắt buộc.", "request")));
        Assert.IsFalse(LiveUi.Error(new ArgumentNullException("dbContext")).Contains("dbContext"));
        Assert.IsFalse(LiveUi.Error(new ArgumentException("SQL connection failed")).Contains("SQL"));
    }

    // Kiểm tra thông báo bảo vệ vai trò và ADMIN sử dụng tên dễ hiểu với người dùng.
    [TestMethod]
    public void RoleAndAdminProtectionMessagesUseUserFacingNames()
    {
        Assert.AreEqual("Quản trị viên", LiveUi.RoleText("ADMIN"));
        Assert.AreEqual("Quản lý kho", LiveUi.RoleText("WAREHOUSE_MANAGER"));
        Assert.AreEqual("Nhân viên kho", LiveUi.RoleText("STAFF"));
        Assert.AreEqual("Quản trị viên", LiveUi.RoleText(" admin "));
        Assert.AreEqual("Quản lý kho", LiveUi.RoleText("warehouse_manager"));
        Assert.AreEqual("Chưa xác định", LiveUi.RoleText("UNSUPPORTED_ROLE"));
        Assert.IsFalse(LiveUi.Error(new InvalidOperationException("Hệ thống phải còn ít nhất một ADMIN đang hoạt động.")).Contains("ADMIN"));
        Assert.IsFalse(AccountFieldErrors.SafeMessage(new InvalidOperationException("Hệ thống phải còn ít nhất một ADMIN đang hoạt động.")).Contains("ADMIN"));
    }

    // Kiểm tra đổi trạng thái điều chỉnh bộ lọc hoạt động để kho vừa sửa vẫn hiển thị.
    [TestMethod]
    public void StateChangeExpandsActiveFilterAndKeepsWarehouseVisible() => Sta(() =>
    {
        using var services = SyntheticServices();
        var active = true;
        var state = new ComboBox(); state.Items.AddRange(["All", "Active", "Inactive"]); state.SelectedIndex = 1;
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code"), ("IsActive", "Active")],
            (_, _, _, _) => Task.FromResult<IReadOnlyList<WarehouseDto>>(state.SelectedIndex == 1 && !active ? [] : [new(7, "SYN7", "Synthetic Warehouse", null, null, active)]),
            pageSize: () => 50).PreserveSelection(row => row.WarehouseId);
        page.AddFilter("State", state);
        state.SelectedIndexChanged += (_, _) => page.RefreshAsync().GetAwaiter().GetResult();
        page.SelectedAction("Change state", (_, _) => { active = !active; state.SelectedIndex = 0; return Task.CompletedTask; });
        page.BindingContext = new BindingContext(); page.CreateControl();
        var grid = (DataGridView)page.Controls.Find("dgvData", true).Single(); grid.CreateControl();
        page.RefreshAsync().GetAwaiter().GetResult();
        Click(page.Controls.Find("btn2", true).Single());
        Assert.AreEqual(0, state.SelectedIndex); Assert.AreEqual(1, grid.Rows.Count);
        var warehouse = (WarehouseDto)grid.CurrentRow!.DataBoundItem!;
        Assert.AreEqual(7, warehouse.WarehouseId); Assert.IsFalse(warehouse.IsActive);
    });

    // Kiểm tra các form chỉnh sửa con không có nút thu nhỏ.
    [TestMethod]
    public void ChildEditorsDoNotOfferMinimize() => Sta(() =>
    {
        using var warehouse = new SmartInventoryManager.Forms.Warehouses.FrmWarehouseDetail();
        using var user = new SmartInventoryManager.Forms.Users.FrmUserDetail();
        Assert.IsFalse(warehouse.MinimizeBox); Assert.IsFalse(user.MinimizeBox);
    });

    // Gắn dữ liệu kho vào hộp chọn của trang danh sách để kiểm thử bộ lọc.
    private static SearchableLookupComboBox BindCore(DataListPage<WarehouseDto> page)
    {
        page.BindingContext = new BindingContext(); page.CreateControl();
        var picker = (SearchableLookupComboBox)page.Controls.Find("cboWarehouse", true).Single();
        picker.DisplayMember = "Name"; picker.ValueMember = "Id";
        picker.DataSource = new List<LookupItemDto> { new(1, "QA1", "Synthetic Warehouse 1"), new(2, "QA2", "Synthetic Warehouse 2") };
        return picker;
    }

    // Kiểm tra đặt lại hộp chọn kho khôi phục danh sách sau khi tìm không có kết quả.
    [TestMethod]
    public void CoreWarehouseResetRestoresChoicesAfterNoMatch() => Sta(() =>
    {
        using var services = SyntheticServices(); var queries = 0;
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code")],
            (_, _, _, _) => { queries++; return Task.FromResult<IReadOnlyList<WarehouseDto>>([]); }, requiresWarehouse: true, pageSize: () => 50);
        var picker = BindCore(page);
        Type(picker, "NO_MATCH_QA"); Assert.AreEqual(0, picker.Items.Count);
        var before = queries;
        Click(page.Controls.Find("btn1", true).Single());
        Assert.AreEqual(2, picker.Items.Count); Assert.AreEqual(1, picker.SelectedValue);
        Assert.IsFalse(picker.IsSearchPending); Assert.AreEqual(before + 1, queries);
    });

    // Kiểm tra gõ kho khác xóa dữ liệu cũ và chặn xuất đến khi chọn kho hợp lệ.
    [TestMethod]
    public void TypingDifferentWarehouseClearsDataAndBlocksExportUntilSelection() => Sta(() =>
    {
        using var services = SyntheticServices(); var queries = 0; var exports = 0;
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code")],
            (id, _, _, _) => { queries++; return Task.FromResult<IReadOnlyList<WarehouseDto>>([new(id, "QA" + id, "Synthetic", null, null, true)]); }, requiresWarehouse: true, pageSize: () => 50);
        var export = page.Action("Export fixture", () => { exports++; return Task.CompletedTask; });
        var picker = BindCore(page); page.RefreshAsync().GetAwaiter().GetResult();
        Assert.AreEqual(1, page.CurrentRows.Single().WarehouseId); var before = queries;
        Type(picker, "QA2"); Assert.AreEqual(0, page.CurrentRows.Count); Assert.IsFalse(export.Enabled);
        Click(export); Assert.AreEqual(0, exports); Assert.AreEqual(before, queries);
        picker.SelectedIndex = 0;
        Assert.IsFalse(picker.IsSearchPending); Assert.AreEqual(2, page.CurrentRows.Single().WarehouseId); Assert.IsTrue(export.Enabled);
    });

    // Kiểm tra đặt lại bộ lọc giữ kho làm việc hiện tại của nhân viên.
    [TestMethod]
    public void CoreResetRetainsStaffCurrentWarehouse() => Sta(() =>
    {
        using var services = SyntheticServices();
        services.Session.SignIn(new(3, "SYN_STAFF", "Synthetic Staff", null, RoleNames.Staff)); services.Session.SetCurrentWarehouse(2);
        using var page = new DataListPage<WarehouseDto>(services, "Synthetic", [("WarehouseCode", "Code")],
            (_, _, _, _) => Task.FromResult<IReadOnlyList<WarehouseDto>>([]), requiresWarehouse: true, pageSize: () => 50);
        var picker = BindCore(page); Type(picker, "NO_MATCH_QA");
        Click(page.Controls.Find("btn1", true).Single()); Assert.AreEqual(2, picker.SelectedValue);
    });

    // Kiểm tra cảnh báo phụ thuộc danh mục giải thích rõ lý do thao tác bị chặn.
    [TestMethod]
    public void CategoryDependencyWarningExplainsWhyActionIsBlocked()
    {
        Assert.AreEqual("Không thể ngừng hoạt động danh mục vì vẫn còn sản phẩm đang hoạt động thuộc danh mục này.",
            LiveUi.Error(new InvalidOperationException("Không thể ngừng danh mục đang có sản phẩm hoạt động.")));
    }

}
