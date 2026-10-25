# VioWare — Smart Inventory Manager

VioWare là đồ án quản lý kho xây dựng bằng C# và Windows Forms, phục vụ theo dõi hàng hóa tại nhiều kho. Hệ thống kết nối SQL Server, quản lý quy trình nhập/xuất và ghi nhận lịch sử thay đổi tồn kho.

## Chức năng chính

- Quản lý sản phẩm, danh mục, nhà cung cấp, kho và vị trí lưu trữ.
- Lập phiếu nhập/xuất, gửi duyệt, duyệt, từ chối, hủy và hoàn tất phiếu.
- Theo dõi tồn theo sản phẩm–kho; điều chỉnh có lý do và chuyển hàng giữa hai kho.
- Cảnh báo hết hàng, sắp hết và vượt mức tồn; tra cứu lịch sử xử lý.
- Tổng quan hoạt động kho, báo cáo Excel/PDF và in phiếu A4.
- Phân tích giá trị tồn ước tính, ABC theo giá trị xuất, luân chuyển hàng và đề xuất nhập.
- Quản lý tài khoản, phân kho, thông báo và nhật ký hoạt động.

ADMIN quản trị hệ thống; WAREHOUSE_MANAGER phụ trách các kho được giao; STAFF làm việc tại các kho được phân công và chọn một kho cho mỗi phiên làm việc. Chỉ phiếu hoàn tất mới cập nhật tồn kho. Việc lưu phiếu, thay đổi tồn và ghi lịch sử được thực hiện trong cùng giao dịch.

## Công nghệ và tổ chức mã nguồn

Ứng dụng sử dụng .NET 10, Windows Forms, AntdUI, SQL Server, Entity Framework Core và ADO.NET. ClosedXML phục vụ xuất Excel, QuestPDF phục vụ xuất PDF; MSTest dùng cho kiểm thử. Bộ cài Windows x64 được đóng gói bằng Inno Setup, kèm .NET runtime. Có thể kết nối máy chủ qua mạng riêng Tailscale.

| Thành phần | Vai trò |
|---|---|
| SmartInventoryManager.UI | Giao diện và thao tác người dùng |
| SmartInventoryManager.Business | Phân quyền, kiểm tra dữ liệu và xử lý nghiệp vụ |
| SmartInventoryManager.Data | Entity, truy cập dữ liệu và EF Core Migrations |
| SmartInventoryManager.Common | Kiểu dữ liệu và tiện ích dùng chung |
| SmartInventoryManager.Setup | Khởi tạo tài khoản quản trị đầu tiên bằng công cụ tương tác |
| SmartInventoryManager.Tests / SmartInventoryManager.UI.Tests | Kiểm thử nghiệp vụ và giao diện |
| Installer | Mã đóng gói bộ cài và script cấp quyền triển khai |

Cơ sở dữ liệu được quản lý bằng EF Core Migrations. Ứng dụng không tự chạy migrations hay tự tạo tài khoản khi mở. Giá trị tồn trong báo cáo là số lượng nhân giá mua hiện tại; giá vốn bình quân và kiểm kê theo phiên thuộc giai đoạn tiếp theo.

## Thành viên

| Thành viên | Phần phụ trách |
|---|---|
| Mai Bảo Anh | Cơ sở dữ liệu và kiến trúc |
| Nguyễn Võ Nguyên Khang | Backend và nghiệp vụ kho |
| Đỗ Gia Khiêm | Giao diện, tổng quan và phân tích |

## Chạy mã nguồn

Cần Windows, .NET 10 SDK, Visual Studio hỗ trợ .NET 10 với workload **.NET desktop development** và SQL Server. Mở `SmartInventoryManager.slnx`, thiết lập kết nối đến cơ sở dữ liệu đã được áp dụng migrations và chọn `SmartInventoryManager.UI` làm project khởi động.

```powershell
dotnet restore SmartInventoryManager.slnx
dotnet build SmartInventoryManager.slnx -c Release
dotnet run --project SmartInventoryManager.UI
```

Có thể mở màn hình cấu hình máy chủ bằng shortcut **VioWare - Configure server** hoặc tham số `--configure`. Tài khoản đăng nhập phần mềm do quản trị viên cấp; thông tin kết nối riêng không được lưu trong repo.

## Bộ cài

[Tải bộ cài và xem các phiên bản tại GitHub Releases](https://github.com/khang1604/SmartInventoryManager/releases).

Bộ cài hỗ trợ cập nhật bằng cách cài đè. Máy khách cần được cấp quyền kết nối tới máy chủ trước khi sử dụng.
