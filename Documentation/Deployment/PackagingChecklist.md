# Đóng gói VioWare và cấu hình máy khách

Bản hiện hành: **v1.0.4**, Windows x64 self-contained. Source phục vụ chấm đồ án được giữ đầy đủ; binary phát hành qua GitHub Releases. Kết quả và giới hạn kiểm tra nằm tại [ProjectStatus.md](../Architecture/ProjectStatus.md).

## Cập nhật từ bản cũ

Đóng VioWare, tải bộ cài v1.0.4 từ GitHub Release và chạy bằng cùng tài khoản Windows đã cài bản trước. Giữ thư mục cài đặt mà bộ cài tự nhận diện. Không cần gỡ phần mềm; cấu hình kết nối đã lưu được giữ lại. Nếu đã có Tailscale thì không cần cài lại. Bản sửa phân kho này không thay đổi schema và không cần chạy migration. Đây là bộ cài đầy đủ hỗ trợ cài đè, không phải gói vá riêng.

## Cài trước, cấp quyền sau

1. Người nhận cài VioWare; .NET10 đóng kèm. Không cài SQL Server trên máy khách.
2. Có thể chọn cài MSI Tailscale chính thức nếu máy chưa có. MSI hiển thị bước cài riêng và có thể yêu cầu ADMIN Windows. Installer không đăng nhập VPN hoặc lưu authkey.
3. Chưa có quyền server: đóng màn hình cấu hình bằng **Để sau / Đóng**; không yêu cầu mạng/DB hoặc tạo ADMIN để cài phần mềm.
4. Quản trị viên gửi lời mời VPN, duyệt thiết bị và giới hạn quyền máySQL/cổngSQL trong policy/firewall. Cài xong hoặc chấp nhận lời mời không tự cấp quyền SQL.
5. Mở shortcut **VioWare - Configure server**. Nhập server (không kèm instance/cổng), cổng, database và SQLlogin được cấp. Chọn WindowsAuthentication chỉ khi tài khoản/domain thực sự có quyền.
6. Kiểm tra kết nối → lưu → đăng nhập VioWare bằng tài khoản do quản trị viên cấp. Không có bootstrap ADMIN trong bộ cài máy khách.

Cấu hình được bảo vệ bằng DPAPI CurrentUser tại `%LOCALAPPDATA%\VioWare\connection.dat`. Không lưu password dạng rõ. Người khác trên máy không dùng được profile của user này; mã chạy với cùng quyền Windows vẫn có thể giải mã, nên DPAPI không thay thế quyềnSQL/VPN. Không gửi file profile cho máy khác; nhập lại khi đổi tài khoản Windows.

Thứ tự chọn connection: **environment SMART_INVENTORY_CONNECTION_STRING → profileDPAPI → XMLlegacy**. Environment có ưu tiên được thông báo ở màn hình cấu hình. Profile hỏng/khônggiảimãđược quay lại cấu hình, không âm thầm chọn DB khác. Đổi thông tin sau lần test thành công phải test lại trước Save. Probe chỉ đọc, không migrate/seed; không phải chứng nhận đầy đủ quyền ghi/schema.

SQL luôn bật mã hóa. Mặc định không tin chứng chỉ tự ký; quản trị viên cần certificate hợp lệ cho tên/IP client sử dụng. Checkbox chấp nhận chứng chỉ tự ký chỉ dùng theo hướng dẫn cho môi trường kiểm soát; không mặc định bật cho production.

## Build lại từ source

Từ repository root, trên Windows có SDK.NET10 và InnoSetup6.7.3+:

```powershell
dotnet restore SmartInventoryManager.slnx
dotnet build SmartInventoryManager.slnx -c Release --no-restore -m:1 -nr:false
dotnet publish SmartInventoryManager.UI/SmartInventoryManager.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:Version=1.0.4 -o artifacts/publish/app-1.0.4-win-x64 -m:1 -nr:false
winget download --id Tailscale.Tailscale --version 1.104.1 --exact --source winget --architecture x64 --download-directory artifacts/tools/Tailscale --accept-package-agreements --accept-source-agreements
```

MSI phải có tên `Tailscale_1.104.1_Machine_X64_wix_en-US.msi` trong `artifacts/tools/Tailscale`, SHA256:

```text
6DA8BCDB0D45A214CAE0E8A88C2987AD48DEC134BF04DFFB9C05F1354C02B0B4
```

Sau đó mở `Installer/VioWare.iss` bằng InnoSetupCompiler → **Build → Compile**, hoặc chạy `ISCC.exe Installer/VioWare.iss`. Output:

```text
artifacts/installer/VioWare-Setup-1.0.4-win-x64-self-contained.exe
```

Script giữ AppId cố định để cập nhật, cài per-user tại `%LOCALAPPDATA%\Programs\VioWare`. VioWare không cần nâng quyền; bước cài VPN có thể cần. MSI chỉ được chạy khi người cài chọn task và chưa phát hiện Tailscale ở vị trí cài chuẩn. Không update/gỡ Tailscale đã có. Có link tải chính thức ở màn hình cấu hình nếu VPN nằm tại đường dẫn khác.

Giữ cả payload .NET/resources/nativeDLL khi đóng gói. PDB, XMLcredentials, connection.dat và launchSettings không được đưa vào installer. ProfileDPAPI nằm ngoài thư mụcapp nên upgrade/uninstall không tự xóa. Installer không thay firewall, không mởrouterport, không chạySQL hoặc ghi đè cấu hình riêng.

Bootstrap máy chủ riêng (quản trị viên, không phân phối cho máy khách):

```powershell
dotnet publish SmartInventoryManager.Setup/SmartInventoryManager.Setup.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/setup-win-x64 -m:1 -nr:false
```

Setup yêu cầu environmentconnection và nhập tương tác. Apply migrations bằng tài khoản triển khai riêng trước; appSQLlogin không có quyền migrate. Chi tiết tại [ExpressAndVpnSetup.md](ExpressAndVpnSetup.md), quyềnruntime tại [ConfigureRuntimePermissions.sql](ConfigureRuntimePermissions.sql).

## Đổi sang máy chủ công ty

IT phê duyệt SQLedition, network/VPN/certificate. Backup/restore DB để giữ dữ liệu/tài khoản hoặc migrations tạo DBtrắng; SQLlogin cấpserver phải tạo/map lại đúngSID/quyền. Kiểmtra backuprestore, toàn bộ workflow với runtimeSQLlogin rồi chốt thời điểm chuyển; tránh ghi song song haiDB. Máy khách đổi server/cổng trong cấu hình và test lại; không phải sửa source hoặc phát hành lại installer chỉ vì đổi IP.

## Kiểm tra trước dùng thật

- [ ] Cài mới/upgrade/uninstall trên máy sạch, cả khi chưa cóVPN/DB.
- [ ] MSI Tailscale khi thiếu VPN, UAC, người dùng từ chối/hủy càiVPN.
- [ ] SQL quaVPN từmáykhác, policy/firewall giới hạn, certificate và quyềnghi thực tế.
- [ ] WindowsDPI125/150/200%, HighContrast, màn hình nhỏ và máyin.
- [ ] Backup/restore production và SQLlogin sau restore.

Build, tests, harnessDPAPI và nativeform trên máy phát triển không thay thế các ca trên. Runtime self-contained không tự nhận bảnvá.NETglobal; cậpnhật runtime cần publish lại. `artifacts/` và `.temp/` không commit.