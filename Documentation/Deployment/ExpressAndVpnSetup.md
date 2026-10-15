# SQL Server Express và kết nối VPN cho VioWare

Cập nhật 15/10/2026: `localhost` giữ DB TEST `SmartInventoryManagerDB`; `localhost\SQLEXPRESS` là Express 64-bit 17.0.1000.7, chạy DB mới `SmartInventoryManagerProductionDB`. Đã apply 6 migrations, đối chiếu 19 bảng/model và bootstrap tương tác 3 role/1 ADMIN. Không đọc hoặc lưu mật khẩu của người dùng.

TCP cổng 14330 đã được xác nhận sau restart. Máy chủ Tailscale `nguyenkhang` có IP `100.86.177.49`, trạng thái Connected theo ảnh người dùng. SQL login `vioware_app` được map bằng [ConfigureRuntimePermissions.sql](ConfigureRuntimePermissions.sql) vào role `VioWareRuntime`; impersonation xác nhận quyền runtime, không sysadmin/db_owner/schema. Người dùng báo đã đăng nhập SQL Authentication và VioWare tại máy chủ thành công; chưa kiểm thử độc lập mọi thao tác ghi production.

**Còn cần nghiệm thu:** SQL từ máy khác qua VPN, policy/firewall giới hạn, chứng chỉ và backup/restore. Kết nối local thành công không chứng minh kết nối từ xa. Hướng dẫn máy khách hiện hành tại [PackagingChecklist.md](PackagingChecklist.md).
## 1. Cài Express riêng

Microsoft cung cấp [SQL Server2025 Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) miễn phí cho ứng dụng production nhỏ. Express2025 giới hạn50GB/database cùng các giới hạn tài nguyên trong [bảng edition](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17).

Không dùng Edition Upgrade để hạ Developer thành Express; Microsoft mô tả downgrade cần cài lại instance trong [hướng dẫn edition](https://learn.microsoft.com/en-us/sql/database-engine/install-windows/upgrade-to-a-different-edition-of-sql-server-setup). Với dự án này, giữ instanceTEST và cài instanceExpress mới:

1. Download đúng mục **SQL Server2025 Express**, chọn Custom để chủ động chọn instance.
2. Trong SQL Server Installation Center, chọn **New SQL Server standalone installation**; nếu hỏi instance đang có, chọn tạo instance mới, không upgrade defaultinstance.
3. Feature: **Database Engine Services**. Named instance: **SQLEXPRESS**; nếu tên đã tồn tại, dừng và kiểm tra thay vì ghi đè.
4. Database Engine Configuration: **Add Current User** làm SQL administrator. Nếu cần máy khác ngoài domain dùng SQLlogin, chọn Mixed Mode và tự đặt mật khẩu quản trị mạnh; không gửi mật khẩu qua chat, không dùng sa cho ứng dụng. Giữ service account mặc định.
5. Hoàn tất, mở SSMS đã có, kết nối `localhost\SQLEXPRESS` bằng Windows Authentication.
6. Chạy truy vấn chỉ đọc để xác nhận đúng Express:

```sql
SELECT @@SERVERNAME AS ServerName,
       SERVERPROPERTY('Edition') AS Edition,
       SERVERPROPERTY('ProductVersion') AS ProductVersion;
```

Không gỡ SQL Developer, không detach/drop TEST hoặc chép MDF. DB production mới sẽ tạo từ migrations sau khi xác nhận `localhost\SQLEXPRESS`; targetlocalhost trước đây không còn là targetproduction dự kiến.

## 2. Migration và ADMIN

Đã chạy6migrations tạo `SmartInventoryManagerProductionDB` trên đúng `localhost\SQLEXPRESS`, không đổi TEST. Khi cậpnhật schema tiếp theo, dùng tài khoản triển khai có quyền schema, kiểm tra target/backup trước ghi; không dùng suiteSQLtest vì guard chỉ cho TESTdefaultinstance.

Bootstrap riêng trong `artifacts/publish/setup-win-x64` đã hoàn tất bằng nhập tươngtác trênmáychủ, có1ADMINactive và3role. Không copy admin/demo hoặc seedSQL cũ. Còn cần tạo SQLlogin ứng dụng riêng chỉ cho DBproduction và quyền cần thiết, kiểm tra CRUD/log/sequence trước phân phối; không cấp sysadmin/db_owner mặc định. Quyền hoàn chỉnh cần review theo EF/ADO.NET và TransferReferenceSequence, không đoán từ chỉCRUD.

## 3. VPN và SQL TCP

Đề xuất Tailscale; hiện mới hướng dẫn, chưa cài hoặc tạo tailnet. Theo [quickstart chính thức](https://tailscale.com/docs/how-to/quickstart), cài Tailscale trên máy SQL và các máy thử, mỗi người đăng nhập tài khoản riêng được mời vào cùng tailnet hoặc dùng device sharing do chủtailnet cấu hình. Không chia sẻ mật khẩu tài khoản VPN.

1. Chủtailnet xác nhận máy SQL và người dùng/máy khách được phép. Lấy địa chỉIP/DNS Tailscale của máy SQL; không dùng localhost/LANIP từ mạng khác.
2. SQL Server Configuration Manager → SQL Server Network Configuration → **Protocols for SQLEXPRESS** → bật TCP/IP. Chọn cổng tĩnh còn trống, ví dụ14330: IPAll TCPDynamicPorts để trống, TCPPort14330; kiểm tra ListenAll/IPbinding phù hợp. Chỉ restart **SQL Server(SQLEXPRESS)** sau khi lưu; không restart defaultinstanceTEST.
3. Windows Firewall chỉ cho TCPcổngSQL đã chọn trên interfaceVPN và nguồn làIPcácmáykhách được duyệt. Tailnetpolicy cũng giới hạn đúng server/cổng. Không tắt firewall, không mở toànInternet, không port-forward router. Có cổngtĩnh thì client dùngTCPIP,cổng, không cần mở SQLBrowserUDP1434.
4. Trên máy khách cóVPN kết nối, chạy:

```powershell
Test-NetConnection -ComputerName <VPN_IP_OR_DNS_OF_SQL_SERVER> -Port 14330
```

Thay placeholder bằng IP/DNS thực. PortTrue mới chỉ chứng minh đườngTCP, chưa chứng minh SQLlogin/quyền/schema.

5. Cấp SQLlogin riêng cho ứng dụng trên DBproduction; trên các máy không có domain chung, WindowsAuthentication không tự hoạt động chỉ vì cùngVPN. Cấu hình certificateSQL phù hợp DNS/clienttrust để dùng `Encrypt=True;TrustServerCertificate=False`. VPN không tự làm certificateSQL trở thành trusted.
6. Đặt connection theo mẫu dưới trên máy khách, không commit credentials hoặc nhúng vào installer chung:

```text
Server=tcp:<VPN_IP_OR_DNS>,14330;Database=SmartInventoryManagerProductionDB;User ID=<APP_SQL_LOGIN>;Password=<SECRET>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;
```

Máy khách dùng form cấu hình server: nhập địa chỉ, cổng, database và SQL login → kiểm tra → lưu DPAPI. Ưu tiên connection: biến `SMART_INVENTORY_CONNECTION_STRING` → profile DPAPI → XML legacy. Phải mở lại app sau đổi environment; SQL login khác tài khoản ADMIN/STAFF của VioWare.

Theo [hướng dẫn TCP củaMicrosoft](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-a-server-to-listen-on-a-specific-tcp-port?view=sql-server-ver17) và [firewallSQL](https://learn.microsoft.com/en-us/sql/sql-server/install/configure-the-windows-firewall-to-allow-sql-server-access?view=sql-server-ver17). Cần IPVPN/client/certificate thực trước khi tạo rule hoặc chuỗikếtnối dùngthật.

## 4. Trước khi dùng thật

- Máychủ phải bật, SQLExpress/VPN hoạtđộng; kiểmtra sau restart.
- Kiểmtra migration/schema, bootstrap, quyềnSQL và đăngnhập từ máy khác mạng.
- Lập lịch backup và thử restore bằng công cụ phùhợpExpress; không dựa vào SQLAgent như editionkhác.
- Chạy caCRUD/xuấtPDF/Excel trên fixtuređượcduyệt; không chạy testmutation vào production.
- Giữ TESTdefaultinstance riêng; không dùng DBproduction chứa dữliệuthật làm môi trườngtest.
