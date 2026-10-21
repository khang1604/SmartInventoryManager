# VioWare — Smart Inventory Manager

Đồ án ứng dụng desktop quản lý nhiều kho bằng C# và Windows Forms. Hệ thống hỗ trợ quản lý hàng hóa, nhập/xuất, tồn kho, phân quyền, cảnh báo và báo cáo.

**Mã nguồn đồ án được giữ đầy đủ**: UI, Business, Data, Common, migrations, tests, công cụ bootstrap và tài liệu. SQL legacy được giữ để tham khảo; schema hiện hành do EF Core Migrations quản lý.

## Tải và cài phần mềm

**[Tải bộ cài Windows x64 v1.0.1](https://github.com/khang1604/SmartInventoryManager/releases/tag/v1.0.1)** — tải file `VioWare-Setup-1.0.1-win-x64-self-contained.exe` trong Assets. Repo private: cần đăng nhập GitHub bằng tài khoản được cấp quyền để xem/tải release.

1. Chạy bộ cài. .NET đã đóng kèm; máy khách không cần SQL Server hoặc Visual Studio.
2. Nếu chưa có Tailscale, có thể chọn cài VPN chính thức trong bộ cài; Windows có thể yêu cầu quyền quản trị.
3. Chưa được mời/duyệt truy cập server: chọn **Đóng**. Không cần tạo ADMIN hoặc kết nối DB ngay khi cài.
4. Sau khi quản trị viên cấp quyền VPN, mở **VioWare - Configure server** trong Start Menu (hoặc chạy `SmartInventoryManager.UI.exe --configure`).
5. Nhập địa chỉ, cổng, tên dữ liệu và SQL login do quản trị viên cấp → **Kiểm tra kết nối → Lưu và tiếp tục**.
6. Đăng nhập VioWare bằng tài khoản ADMIN/manager/STAFF do quản trị viên cung cấp. Đây là tài khoản khác SQL login và Tailscale.

Thông tin kết nối được bảo vệ bằng Windows DPAPI theo người dùng, lưu tại `%LOCALAPPDATA%\VioWare\connection.dat`; giữ khi cập nhật. Cấu hình này không kèm trong installer. Biến `SMART_INVENTORY_CONNECTION_STRING` có ưu tiên hơn cấu hình đã lưu, rồi đến XML legacy. Không chia sẻ mật khẩu hoặc file cấu hình riêng qua repo.

**Cài xong không tự có quyền truy cập server.** Quản trị viên vẫn phải duyệt quyền Tailscale/firewall và cấp tài khoản. Chứng chỉ máy chủ phải được cấu hình phù hợp; chỉ bật chấp nhận chứng chỉ tự ký khi được quản trị viên hướng dẫn.

## Thuyết trình cơ bản

| Nhóm tính năng | Nội dung demo |
|---|---|
| Đăng nhập/phân quyền | ADMIN, WAREHOUSE_MANAGER, STAFF; phạm vi kho được kiểm tra trong Business |
| Danh mục nền | Sản phẩm, danh mục, nhà cung cấp, kho/vị trí; trạng thái hoạt động |
| Nhập và xuất | Lập nháp → gửi duyệt → duyệt → hoàn tất; từ chối/gửi lại/hủy có audit và lịch sử |
| Tồn và vận hành | Tồn từng sản phẩm–kho; điều chỉnh có lý do; chuyển kho hai phía trong một transaction |
| Cảnh báo | Hết hàng, sắp hết, vượt ngưỡng; cập nhật sau biến động tồn |
| Dashboard | Tổng quan, nhập/xuất theo ngày, tồn thấp và lịch sử gần nhất |
| Báo cáo | Lọc theo phạm vi được phép; Excel/PDF và PDF phiếu A4 |
| Phân tích | Giá trị tồn ước tính, ABC theo giá trị xuất, luân chuyển90ngày và đề xuất nhập |
| Quản trị | Người dùng, phân kho STAFF, reset mật khẩu, thông báo và nhật ký |

Gợi ý trình diễn: đăng nhập ADMIN → tạo dữ liệu nền → nhập và hoàn tất → xuất và hoàn tất → xem tồn/lịch sử/cảnh báo → chuyển kho/điều chỉnh → Dashboard/báo cáo/Excel/PDF → phân quyền STAFF. Chỉ demo thao tác ghi trên DB demo được duyệt.

Các quy tắc chính:

- Chỉ phiếu **COMPLETED** mới tác động tồn, đúng một lần; lưu tồn/movement/audit/log nguyên tử.
- `0 < ReorderPoint < MaximumStock`; tồn0 là hết hàng, tồn dương≤ReorderPoint là sắp hết, tồn>MaximumStock là vượt ngưỡng.
- Giá trị tồn **ước tính** = `Quantity × PurchasePrice` hiện tại, không phải giá vốn kế toán bình quân.
- ABC dùng giá trị xuất của phiếu COMPLETED trong12tháng; đề xuất nhập = `MaximumStock − Quantity` khi chạm ngưỡng, không tự tạo phiếu.
- Giá vốn bình quân và kiểm kê theo phiên thuộc giai đoạn sau. Giới hạn nghiệm thu ghi trong báo cáo chi tiết.

## Công nghệ và thành viên

| Phần | Công nghệ / phụ trách |
|---|---|
| UI | Windows Forms, AntdUI — Khiêm |
| Business | C#, LINQ, services, transaction/validation — Khang |
| Data và kiến trúc | SQL Server, EF Core10.0.12, ADO.NET — Bảo Anh |
| Xuất file / kiểm thử | ClosedXML, QuestPDF / MSTest |
| Triển khai | .NET10 self-contained win-x64, Inno Setup; Tailscale cho mạng riêng |

Luồng phụ thuộc: **UI → Business → Data → Common**; UI gọi services, Data truy cập DB. Migrations là nguồn schema; app không tự migrate hoặc seed tài khoản.

## Chạy source để chấm đồ án

Cần Visual Studio2026 với workload .NET desktop development, SDK.NET10 và SQL Server phù hợp.

```powershell
git clone https://github.com/khang1604/SmartInventoryManager.git
cd SmartInventoryManager
git checkout Backend
dotnet restore SmartInventoryManager.slnx
dotnet build SmartInventoryManager.slnx -c Release
dotnet test SmartInventoryManager.Tests/SmartInventoryManager.Tests.csproj -c Release --filter "TestCategory!=SQLServer"
dotnet run --project SmartInventoryManager.UI
```

Bản bàn giao cố định: tag `v1.0.1` hoặc nhánh `Backend`; khi repo được archive vẫn clone/pull/tải source được. Không push thay đổi vào bản bàn giao; owner có thể unarchive nếu cần sửa. File `Properties/launchSettings.example.json` là mẫu cấu hình local, không có credentials; không commit file launchSettings thật.

DB mới: cấu hình connection triển khai riêng, apply migrations đã review rồi dùng `SmartInventoryManager.Setup` bootstrap tương tác trên máy quản trị. Máy khách không chạy bootstrap hoặc migrations. Không dùng bộ test SQL mutation trên DB production.

## Tài liệu đồ án

- [Báo cáo và thuyết trình Word](Documentation/Reports/ProjectReport.docx)
- [Kiến trúc hiện hành](Documentation/Architecture/CurrentArchitecture.md)
- [Tiến độ, kết quả kiểm tra và giới hạn nghiệm thu](Documentation/Architecture/ProjectStatus.md)
- [Checklist database](Documentation/Architecture/DatabaseRebuildChecklist.md)
- [Đóng gói và cấu hình máy khách](Documentation/Deployment/PackagingChecklist.md)
- [SQL Express và kết nối VPN](Documentation/Deployment/ExpressAndVpnSetup.md)

Thư mục `artifacts/`, `.temp/`, `bin/`, `obj/` và cấu hình credentials không thuộc source bàn giao. Installer được phân phối qua GitHub Releases, không commit binary vào Git.