# Chuẩn bị bộ cài VioWare

Đây là hướng dẫn đóng gói, kết quả kiểm tra hiện hành nằm trong [ProjectStatus.md](../Architecture/ProjectStatus.md). Chưa có bộ cài Inno Setup hoặc nghiệm thu trên máy sạch.

## Payload đã publish ngày 15/10/2026

Từ repository root:

```powershell
dotnet publish SmartInventoryManager.UI/SmartInventoryManager.UI.csproj -c Release --no-restore --self-contained false -o .temp/FinalAcceptance/publish/app -m:1 -nr:false
dotnet publish SmartInventoryManager.Setup/SmartInventoryManager.Setup.csproj -c Release --no-restore --self-contained false -o .temp/FinalAcceptance/publish/setup -m:1 -nr:false
```

- App chính: `.temp/FinalAcceptance/publish/app/SmartInventoryManager.UI.exe`.
- Bootstrap ADMIN riêng: `.temp/FinalAcceptance/publish/setup/SmartInventoryManager.Setup.exe`. Công cụ này không phải trình cài Inno Setup, không tự migrate.
- Đưa toàn bộ cây thư mục `app` vào bộ cài, gồm DLL, `runtimes`, resources, `.deps.json` và `.runtimeconfig.json`; không chỉ copy EXE. Có thể bỏ PDB khỏi bản phân phối.
- Đây là framework-dependent, app cần **.NET 10 Desktop Runtime** phù hợp kiến trúc EXE trên máy đích. Máy phát triển có runtime chưa chứng minh máy đích đã có. SQL Server và database được cấu hình riêng; không nằm trong payload này.
- `launchSettings.json` chỉ phục vụ chạy từ IDE, không cấu hình ứng dụng đã cài. Không đưa file này, TEST credentials, `.agent`, `.temp` QA, seed/demo hoặc legacy SQL vào payload app.

## Kết nối và schema

1. Quản trị viên xác nhận server/database triển khai, backup và người chịu trách nhiệm migration trước khi cấu hình. App không tự migrate/seed khi mở.
2. Đặt `SMART_INVENTORY_CONNECTION_STRING` cho tiến trình/người dùng chạy app. Nếu dùng XML fallback, đặt `SmartInventoryManager.UI.dll.config` cạnh EXE với cấu trúc sau và thay placeholder ở máy đích:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="SmartInventoryManagerDb"
         connectionString="Server=YOUR_SERVER;Database=YOUR_DATABASE;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;" />
  </connectionStrings>
</configuration>
```

Mẫu dùng Windows Authentication; quyền SQL và chứng chỉ phải được quản trị viên cấu hình phù hợp. XML không tự lấy giá trị từ launchSettings; biến môi trường có ưu tiên cao hơn XML. Không hard-code mật khẩu thật vào repository hoặc script installer dùng chung. Mẫu trên chưa trỏ tới DB nào.

3. Apply migrations đã review từ source/EF CLI được pin theo README; xác nhận không pending/model mismatch. Không chạy SQL schema legacy chồng lên migrations.
4. Với DB mới đã có schema, dùng bootstrap riêng và biến môi trường nói trên: chạy `SmartInventoryManager.Setup.exe --check`, sau đó bỏ `--check` để tạo ADMIN đầu tiên qua nhập tương tác. Nếu đã có ADMIN active, công cụ không ghi đè. Setup không đọc XML fallback của UI.

## Kiểm tra bộ cài sau khi có Inno Setup

### Tạo bằng Script Wizard

1. Mở Inno Setup Compiler, chọn **File → New → Create a new script file using the Script Wizard**.
2. Application name: **VioWare**; version: phiên bản phát hành team chốt (ví dụ `1.0.0`); publisher: tên nhóm của bạn.
3. Application folder: chọn thư mục cài VioWare theo wizard; bật tạo thư mục ứng dụng.
4. Main executable: chọn `C:\Project\SmartInventoryManager\.temp\FinalAcceptance\publish\app\SmartInventoryManager.UI.exe`.
5. Other application files: **Add folder** chọn toàn bộ thư mục `app` ở trên, đồng ý gồm subfolders. Đảm bảo có DLL và `runtimes`; loại PDB nếu muốn giảm dung lượng. Không chọn cả `.temp/FinalAcceptance`.
6. Tạo Start Menu shortcut và desktop shortcut nếu cần; không chọn license giả nếu nhóm chưa có tài liệu license.
7. Compiler output folder: `C:\Project\SmartInventoryManager\.temp\Installer\Output`; tên file, ví dụ `VioWare-Setup-1.0.0`. Tạo thư mục trước nếu wizard yêu cầu. Icon dùng `SmartInventoryManager.UI\Resources\Images\vioware.ico` trong repository.
8. Lưu script tại `C:\Project\SmartInventoryManager\.temp\Installer\VioWare.iss`, chọn **Build → Compile**. File installer nằm trong output folder đã chọn.
9. Trước khi phân phối, thực hiện checklist bên dưới. Wizard cơ bản chưa tự cài/check .NET Runtime, chưa cấu hình SQL hoặc chạy migrations/bootstrap. Cài .NET 10 Desktop Runtime phù hợp kiến trúc và cấu hình DB trước khi mở ứng dụng.

Tham khảo chính thức: [Creating Installations](https://jrsoftware.org/ishelp/topic_creatinginstallations.htm), [Files/subdirectories](https://jrsoftware.org/ishelp/topic_filessection.htm).

- [ ] Chốt kiến trúc máy đích, đường dẫn cài, phiên bản, icon, shortcut và runtime prerequisite theo payload thực tế.
- [ ] Cài trên máy sạch; mở qua shortcut bằng tài khoản Windows thường, kiểm tra kết nối SQL và đăng nhập.
- [ ] Kiểm tra xuất PDF/Excel vào thư mục người dùng có quyền ghi; PDF tiếng Việt, nhiều trang và native DLL đầy đủ.
- [ ] Kiểm tra cập nhật không ghi đè cấu hình kết nối riêng của máy; uninstall không xóa DB hoặc file người dùng đã xuất.
- [ ] Kiểm tra Windows DPI125/150/200%, màn hình nhỏ, chuyển màn hình và máy in/PDF viewer thực tế.
- [ ] Ghi lại ca ADMIN cuối cùng và bootstrap trắng SQL còn chưa kiểm tra lại; chỉ dùng môi trường cô lập khi được duyệt.

Payload publish nằm trong `.temp` và không commit. Không lấy việc publish thành công làm chứng nhận bộ cài hoặc production.
