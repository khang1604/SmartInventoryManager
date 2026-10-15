# SMART INVENTORY MANAGER

Ứng dụng desktop quản lý kho dành cho doanh nghiệp/cửa hàng có một hoặc nhiều kho. Dự án phục vụ môn Lập trình Windows Forms, sử dụng C#, .NET và SQL Server.

> Tài liệu này mô tả cách tham gia và phát triển dự án. Danh sách chức năng là phạm vi dự kiến, không phải xác nhận tất cả đã hoàn thiện. File hoặc màn hình khung chưa có nghiệp vụ được xem là chưa triển khai.

**Cập nhật: 15/10/2026.** Quy tắc dùng chung tại [.github/copilot-instructions.md](.github/copilot-instructions.md); tiến độ và giới hạn nghiệm thu tại [báo cáo hoàn thiện ba tầng](Documentation/Architecture/ProjectStatus.md), trạng thái schema tại [Database Rebuild Checklist](Documentation/Architecture/DatabaseRebuildChecklist.md). `.agent/SystemContract.md` là bản contract hiện hành trên máy làm việc; `.agent/` không được chia sẻ qua Git. Các quyết định người dùng đã chốt sau contract được ghi trong quy tắc dùng chung và báo cáo; không suy rằng mọi yêu cầu contract đã có code.

## 1. Công nghệ sử dụng

| Thành phần | Công nghệ / quy định |
|---|---|
| Hệ điều hành | Windows phù hợp yêu cầu hỗ trợ của .NET và Visual Studio; ưu tiên Windows 11 |
| Ngôn ngữ | C# |
| Nền tảng mặc định | **.NET 10**, không phải “.NET Framework 10” |
| Giao diện | Windows Forms - AntdUI |
| Môi trường phát triển mặc định | Visual Studio 2026, workload `.NET desktop development` |
| Database | Microsoft SQL Server; instance local phù hợp cho phát triển |
| ORM | Entity Framework Core **10.0.12** |
| Truy vấn | LINQ |
| Truy cập SQL bổ sung | ADO.NET / Microsoft.Data.SqlClient |
| Quản lý thư viện | NuGet, thông qua Visual Studio hoặc dotnet CLI |
| Quản lý mã nguồn | Git / GitHub |
| Kiểm thử | MSTest **3.8.3**, Microsoft.NET.Test.Sdk **17.14.1** |

Dependencies đang khai báo (Data pin bản vá được duyệt System.Security.Cryptography.Pkcs 9.0.21): Microsoft.Data.SqlClient 7.1.1, AntdUI 2.4.12, ClosedXML 0.105.1, QuestPDF 2026.9.1. Đây là các thư viện đã có trong project, không phải yêu cầu cài thêm hoặc tự nâng phiên bản.

Phiên bản thật được xác định bởi `.csproj`, `global.json` và `Directory.Packages.props` nếu có. Nếu repository đã dùng .NET 8/EF Core 8.x, giữ cấu hình đó và dùng Visual Studio tương thích. Không tự nâng phiên bản hoặc trộn EF6 với EF Core.

- EF: CRUD thông thường.
- LINQ: truy vấn, lọc, tổng hợp và phân tích.
- ADO.NET: Stored Procedure, truy vấn báo cáo hoặc SQL phức tạp khi cần.
- Không sử dụng Next.js, Mantine, MongoDB, npm/yarn hoặc công nghệ web trong dự án này.
- Thêm thư viện mới, đổi công nghệ hoặc nâng phiên bản lớn phải được nhóm thống nhất trước.

## 2. Phạm vi chức năng

- Đăng nhập và phân quyền Admin / Quản lý kho / Nhân viên kho.
- Quản lý sản phẩm, danh mục, nhà cung cấp, kho và vị trí kho.
- Nhập kho, xuất kho, kiểm kê, điều chỉnh và chuyển kho.
- Theo dõi tồn theo sản phẩm và từng kho; lưu lịch sử biến động.
- Cảnh báo hết hàng, sắp hết hàng và tồn vượt mức.
- Dashboard, thống kê, biểu đồ, báo cáo và phân tích theo tiêu chí đã thống nhất.

### 2.1. Trạng thái triển khai và nghiệm thu

| Nhóm | Trạng thái đến 14/10/2026 |
|---|---|
| Phân quyền/phân kho | Ba role; ADMIN quản lý assignment, ngừng/kích hoạt lại giữ row. STAFF đổi kho làm việc, Business kiểm tra assignment active; đã test UI kho A/B |
| Nhập/xuất/tồn | Sáu trạng thái; Complete post tồn một lần. SQL đã kiểm tra duplicate completion, transfer đồng thời, hai phiếu xuất cạnh tranh, complete/cancel race và rollback khi ghi log lỗi |
| Ngưỡng/cảnh báo | `0 < ReorderPoint < MaximumStock`, bốn trạng thái tồn; publisher sau commit, log cùng transaction và notification best-effort cho ADMIN/manager đúng kho |
| Dashboard | Daily nhập/xuất theo CreatedAt; mặc định 30 ngày gồm hôm nay; top10 dòng sản phẩm–kho tồn thấp; bốn trạng thái và recent20 |
| Báo cáo/export | Totals và Excel/PDF lấy toàn bộ dataset khớp bộ lọc được phép; PDF phiếu A4 cơ bản đã kiểm tra phân trang. Chưa nghiệm thu SaveFileDialog/máy in thật/dataset rất lớn |
| Bootstrap | CLI tương tác tạo ba role và ADMIN đầu tiên/PBKDF2; nghiệm thu trên SQL trắng, đăng nhập thật và guard ADMIN active đạt; DB tạm đã dọn |
| Audit/analytics/UI | CompletedBy/At, CancelReason/RejectReason, lịch sử từ chối phân trang20, movement before/after/difference, shared transfer sequence; giá trị ước tính/ABC/luân chuyển/đề xuất nhập; ẩn scrollbar và sửa màu Chi tiết |
| Giai đoạn sau | Bình quân gia quyền, tồn đầu kỳ/chi phí chuyển và điều chỉnh, retention/archive, production; Designer/DPI/máy in thật chưa nghiệm thu đầy đủ |

Kết quả nghiệm thu Release 15/10: **174/174 tests PASS** (169 unit + 5 SQL, không skip), build **0 lỗi/0 cảnh báo**; schema **19 bảng**, 6 migrations không pending. SQL bổ sung kiểm tra tài khoản/phân kho, tranh chấp username/email và manager, lịch sử phiếu 20+1 dòng. Native ADMIN kiểm tra form phiếu/lịch sử, báo cáo và xuất Excel; PDF/Excel được kiểm tra bằng harness. SQL ADMIN cuối cùng/bootstrap DB trắng, DPI Windows thật và máy in còn chưa nghiệm thu lại. Chi tiết tại [báo cáo duy nhất](Documentation/Architecture/ProjectStatus.md); chuẩn bị bộ cài tại [hướng dẫn đóng gói](Documentation/Deployment/PackagingChecklist.md).

## 3. Phân công và phối hợp

| Thành viên | Phụ trách chính |
|---|---|
| **Bảo Anh** | Database, ERD, Entity, DbContext, kiến trúc, SQL scripts; Repository/UnitOfWork khi có giá trị rõ ràng |
| **Khang** | Business Services, nhập/xuất, tồn kho, điều chỉnh/chuyển kho, backend cảnh báo, kiểm tra và kiểm thử nghiệp vụ |
| **Khiêm** | Windows Forms UI, Dashboard, Analytics, biểu đồ, báo cáo và thông báo trên giao diện |

- Mỗi người sửa phần được giao. Được đọc code tầng khác để hiểu cách kết nối.
- Nếu cần sửa phần người khác, thống nhất thành phần, mục đích và quyền sửa trước.
- Database, DTO, Interface, Common, đăng nhập và cấu hình solution là các điểm cần phối hợp; không tự đổi hợp đồng chung.
- Khi thay đổi liên tầng, nêu rõ ai làm phần dữ liệu, ai làm nghiệp vụ, ai nối giao diện.
- Không tạo Service/Repository thay thế hoặc SQL trực tiếp trên Form để đi vòng qua phần chưa hoàn thiện.

## 4. Cấu trúc và kiến trúc

| Đường dẫn | Nội dung |
|---|---|
| `SmartInventoryManager.sln` hoặc `.slnx` | Solution của dự án |
| `SmartInventoryManager.UI/` | Forms, Controls, Components, Resources, Program.cs |
| `SmartInventoryManager.Business/` | Services, Interfaces, DTOs, Validators, Events |
| `SmartInventoryManager.Data/` | Context, Entities, Configurations, Repository/UnitOfWork nếu được dùng |
| `SmartInventoryManager.Common/` | Constants, Enums, Helpers, Session |
| `SmartInventoryManager.Tests/` | Unit tests và SQL Server integration tests opt-in |
| `SmartInventoryManager.Setup/` | Bootstrap ADMIN tương tác riêng, không chạy khi mở UI |
| `SmartInventoryManager.Data/Migrations/` | Nguồn quản lý schema EF và snapshot |
| `Database/` | SQL legacy/seed/tham khảo; không thay thế migrations |
| `Documentation/` | Yêu cầu, thiết kế, báo cáo và tài liệu tham khảo |
| `.github/copilot-instructions.md` | Quy tắc AI Agent dùng chung, được chia sẻ qua Git |
| `.agent/RULE.md` | Bản quy tắc cá nhân, không commit |

Tham chiếu mặc định:

- UI → Business, Common.
- Business → Data, Common.
- Data → Common.
- Common không phụ thuộc ngược các tầng khác.

UI hiển thị và gọi Service. Business xử lý nghiệp vụ. Data truy xuất/lưu dữ liệu. Không đặt SQL, truy vấn DbContext hoặc công thức tồn kho trong Form. UI chỉ tham chiếu Data tại điểm khởi tạo khi kiến trúc đã chọn thực sự cần.

## 5. Chuẩn bị môi trường

Mỗi thành viên chuẩn bị:

1. Visual Studio tương thích framework của repository; cài workload `.NET desktop development` và SDK tương ứng.
2. Git.
3. SQL Server instance dùng cho phát triển. Có thể dùng LocalDB trên từng máy nếu phù hợp cấu hình nhóm, hoặc SQL Server Express/Developer phù hợp mục đích sử dụng.
4. SSMS nếu cần quản lý và chạy SQL script. SSMS là công cụ quản lý, không phải SQL Server và không phải gói NuGet.
5. GitHub Copilot trong Visual Studio nếu sử dụng AI Agent; đây là tùy chọn, không bắt buộc để build/chạy ứng dụng.

Trong Developer PowerShell kiểm tra:

```powershell
dotnet --list-sdks
git --version
```

`dotnet restore` chỉ tải thư viện của dự án; không cài SQL Server, SDK hoặc workload Visual Studio.

## 6. Lấy mã nguồn và chạy lần đầu

### 6.1. Clone repository

Lấy URL thật tại GitHub → Code. Thay `<URL_REPOSITORY>` và `<THU_MUC_REPOSITORY>` trong ví dụ sau bằng thông tin thực tế:

```powershell
git clone <URL_REPOSITORY>
cd <THU_MUC_REPOSITORY>
git branch -a
```

Nhánh chính của dự án là `develop`. Chuyển sang nhánh này để lấy code chung:

```powershell
git switch develop
git pull --ff-only origin develop
```

Nếu nhánh chỉ có trên remote, chạy `git fetch origin` rồi `git switch --track origin/develop`. Nếu chưa thấy `origin/develop`, báo người quản lý repository.

### 6.2. Mở solution

- Mở `.sln` hoặc `.slnx` hiện có trong Visual Studio.
- Không tạo project/solution mới sau khi clone.
- Chọn project UI làm Startup Project: chuột phải project UI → **Set as Startup Project**.

### 6.3. Restore và build

Chạy tại thư mục chứa solution. Nếu solution nằm trong thư mục con của repository, chuyển vào thư mục đó trước:

```powershell
dotnet restore
dotnet build --no-restore
```

Nếu báo thiếu SDK hoặc package không tương thích, kiểm tra cấu hình repository; không tự nâng framework để bỏ qua lỗi.

### 6.4. Thiết lập database

1. EF Core Migrations trong `SmartInventoryManager.Data/Migrations` là nguồn quản lý schema. Giữ SQL cũ để tham khảo; không chạy script tạo schema chồng lên migrations.
2. Cấu hình biến môi trường `SMART_INVENTORY_CONNECTION_STRING` cho đúng server/database. UI có fallback đọc `App.config` local khi không có biến môi trường; cấu hình bí mật không commit.
3. Review script migration và dữ liệu hiện có trước khi apply. Migration ngưỡng dương yêu cầu xử lý các sản phẩm ReorderPoint=0 bằng quyết định nghiệp vụ, không tự chọn số thay thế.
4. Với EF CLI 10.0.12 đã được cài trên máy, chạy `dotnet ef database update --project SmartInventoryManager.Data --startup-project SmartInventoryManager.UI` từ gốc repository sau khi xác nhận target và review migration. Factory đọc cùng biến môi trường, fallback `localhost/SmartInventoryManagerDB`; không dựa vào launchSettings của UI để cấu hình EF CLI. Ứng dụng không tự migrate khi mở.
5. DB mới chưa có role/admin: chạy `dotnet run --project SmartInventoryManager.Setup -- --check` để kiểm tra chỉ đọc; bỏ `--check` để bootstrap tương tác. Công cụ yêu cầu xác nhận tên DB, nhập mật khẩu được che, tạo đúng ADMIN/WAREHOUSE_MANAGER/STAFF và ADMIN đầu tiên bằng PBKDF2. Nếu đã có ADMIN active thì không ghi đè; không seed tài khoản khi mở UI.
6. Dữ liệu demo là thao tác riêng được duyệt. SQL seed cũ có thể ghi đè tài khoản; đọc kỹ trước khi chạy. Không dùng thông tin tài khoản demo trong production.

Môi trường đã nghiệm thu là **TEST `localhost/SmartInventoryManagerDB`**; server production riêng chưa được cung cấp. DB TEST có 6 migrations đến `20261015004156_AddReceiptLifecycleAudit`; probe ngày15/10 đã xác nhận 19 bảng khớp model và không có pending migrations/model changes. Không tự áp dụng kết luận này cho DB trên máy khác.

Ví dụ connection string local, thay database/instance bằng tên thật đã thống nhất:

```text
Server=(localdb)\MSSQLLocalDB;Database=SmartInventoryDb;Integrated Security=True;
```

Hoặc nếu máy đã có instance SQL Server Express tên `SQLEXPRESS`:

```text
Server=.\SQLEXPRESS;Database=SmartInventoryDb;Integrated Security=True;
```

Đây là ví dụ kết nối cho máy khác, không phải target đã được suite SQL hiện tại cho phép. Thông tin đăng nhập và cấu hình chứa bí mật phải được lưu local, không commit. Khi chạy CLI trong PowerShell, đặt biến môi trường của phiên làm việc bằng connection đã được phê duyệt. F5 dùng launch profile cấu hình trên máy; không ghi bí mật vào launchSettings đang được Git theo dõi.

Nếu dùng `App.config` local, tên connection là `SmartInventoryManagerDb`. UI đọc config cạnh binary hoặc trong thư mục làm việc; không có build hook tự copy root App.config. Setup yêu cầu biến môi trường, không dùng fallback này.

### 6.5. Chạy ứng dụng

Nhấn **F5** trong Visual Studio, hoặc nếu project có đúng tên/đường dẫn bên dưới:

```powershell
dotnet run --project SmartInventoryManager.UI/SmartInventoryManager.UI.csproj
```

Nếu project UI đang có tên khác, dùng đường dẫn `.csproj` thực tế. DB mới dùng bootstrap ở mục 6.4; tài khoản demo chỉ dùng khi seed đã được duyệt. Màn hình chạy được chưa chứng minh mọi nghiệp vụ đã nghiệm thu.

## 7. Quy tắc nghiệp vụ quan trọng

- Tồn kho theo sản phẩm + kho, không chỉ theo sản phẩm.
- Business Service là nơi điều phối cập nhật tồn. Không dùng Trigger tự tăng/giảm Inventory.
- Phiếu, chi tiết, tồn và lịch sử phải nhất quán trong transaction.
- Một phiếu chỉ tác động tồn một lần; chống gửi lại và thao tác đồng thời.
- Không xuất vượt tồn hoặc cho tồn âm khi chưa có nghiệp vụ cho phép.
- Sự kiện cập nhật UI/cảnh báo phát sau commit thành công.
- Điều chỉnh phải có lý do, người thực hiện và số lượng trước/sau.

Phạm vi kho: ADMIN không bị giới hạn một kho; WAREHOUSE_MANAGER theo `Warehouse.ManagerId`; STAFF theo `UserWarehouse.IsActive` và kho đang chọn đã được xác minh. User/kho phải active. Ngừng phân kho giữ row để kích hoạt lại; `Location` không cấp quyền kho.

Phiếu có sáu trạng thái `DRAFT`, `PENDING_APPROVAL`, `APPROVED`, `REJECTED`, `COMPLETED`, `CANCELLED`; chỉ Complete mới tác động tồn. Posting nội bộ đi qua InventoryService cùng context/transaction. Log nghiệp vụ được lưu trong transaction; events và notification cảnh báo phát sau commit. Notification best-effort chỉ gửi ADMIN active và manager active của kho, lỗi gửi không chạy lại nghiệp vụ tồn.

Retry tự động hiện chỉ áp dụng SQL deadlock1205 đã rollback, tối đa hai lần; không retry timeout/mất kết nối hoặc kết quả commit chưa rõ. Hết retry trả `CONCURRENCY_RETRY_EXHAUSTED`. Đây không phải cam kết mọi lỗi đồng thời đều được tự xử lý.

Audit đã triển khai theo quyết định TeamChot: completion actor/time, lý do riêng và history từ chối; before/after/difference và shared reference chuyển kho. Analytics giai đoạn đầu dùng giá trị ước tính, ABC, luân chuyển90ngày và đề xuất nhập; bình quân gia quyền kế toán để giai đoạn sau.

Ngưỡng cấu hình hợp lệ: `0 < ReorderPoint < MaximumStock`.

| Điều kiện | Trạng thái |
|---|---|
| `Quantity = 0` | `OUT_OF_STOCK` |
| `0 < Quantity <= ReorderPoint` | `LOW_STOCK` |
| `ReorderPoint < Quantity <= MaximumStock` | `NORMAL` |
| `Quantity > MaximumStock` | `OVERSTOCK` |

Tính trạng thái tại Business; UI hiển thị kết quả bằng tiếng Việt. Không hard-code ngưỡng demo hoặc tính lại công thức khác ở biểu đồ.

## 8. Quy trình Git của nhóm

| Nhánh | Mục đích | Cách sử dụng |
|---|---|---|
| `develop` | Nhánh chính chứa code chung của nhóm | Lấy code mới và nhận Pull Request đã được kiểm tra; không commit/push trực tiếp |
| `feature/<ten-viec>` | Thêm chức năng | Tạo từ `develop`, làm việc rồi gửi Pull Request về `develop` |
| `fix/<ten-loi>` | Sửa lỗi | Tạo từ `develop`, sửa rồi gửi Pull Request về `develop` |
| `chore/<ten-viec>` | Bảo trì, cấu hình | Tạo từ `develop`, sửa rồi gửi Pull Request về `develop` |
| `Backend` | Nhánh đang được chủ dự án chỉ định cho đợt hoàn thiện hiện tại | Commit/push theo ủy quyền hiện hành, review qua PR về `develop`; tên nhánh không tự cấp quyền sửa liên tầng |

Mỗi nhánh dành cho một nhiệm vụ. Tên nhánh ngắn, không dấu, không khoảng trắng. Nhóm không dùng quy trình gộp `develop` sang `main`.

### 8.1. Bắt đầu nhiệm vụ

Chạy tại thư mục gốc repository. Commit hoặc bảo quản công việc hiện tại trước khi đổi nhánh:

```powershell
git status
git switch develop
git pull --ff-only origin develop
git switch -c feature/import-receipt
```

Thay `feature/import-receipt` bằng tên nhiệm vụ, ví dụ `feature/dashboard` hoặc `fix/export-stock`. Nếu pull không thành công vì lịch sử khác nhau, kiểm tra và phối hợp xử lý; không reset hoặc force push.

### 8.2. Lưu thay đổi

Làm đúng phần việc được giao. Kiểm tra thay đổi và chỉ thêm file thuộc nhiệm vụ vào commit. Thay đường dẫn ví dụ bằng file thực tế:

```powershell
git diff
git add SmartInventoryManager.Business/Services/ImportService.cs
git commit -m "feat(import): implement stock import workflow"
```

### 8.3. Cập nhật code chung và kiểm tra

Khi công việc đã được lưu và không còn thay đổi chưa commit:

```powershell
git fetch origin
git merge origin/develop
dotnet restore
dotnet build --no-restore
```

Nếu có xung đột, xử lý từng file, phối hợp người phụ trách, stage và commit kết quả gộp rồi build lại. Không chọn bỏ toàn bộ code của đồng đội để hết xung đột. Chạy các kiểm thử liên quan theo mục 11 trước khi push.

### 8.4. Push và gửi Pull Request

```powershell
git push -u origin feature/import-receipt
```

Trên GitHub, tạo Pull Request:

- **Base:** `develop`.
- **Compare:** nhánh nhiệm vụ của bạn, ví dụ `feature/import-receipt`.
- Ghi ngắn gọn nội dung thay đổi và kết quả kiểm tra.
- Cần ít nhất một thành viên khác kiểm tra và chấp thuận trước khi merge. Thay đổi liên tầng cần người phụ trách liên quan xem.
- Merge vào `develop` sau khi xử lý ý kiến và các kiểm tra đã đạt.

### 8.5. Sau khi Pull Request được merge

Khi không còn thay đổi chưa commit:

```powershell
git switch develop
git pull --ff-only origin develop
git branch -d feature/import-receipt
```

Có thể xóa nhánh nhiệm vụ trên GitHub sau khi merge nếu không còn dùng. Nếu Git từ chối xóa nhánh cục bộ sau squash merge, giữ nhánh để kiểm tra lại; không tự dùng `-D`.

Không force push `develop` hoặc viết lại lịch sử trong công việc hằng ngày.

## 9. Quy ước commit

| Prefix | Khi sử dụng | Ví dụ đúng với dự án |
|---|---|---|
| `feat` | Thêm chức năng | `feat(export): validate available stock` |
| `fix` | Sửa lỗi | `fix(inventory): prevent duplicate stock updates` |
| `docs` | Sửa tài liệu | `docs: update team setup guide` |
| `style` | Định dạng, không đổi logic | `style: format C# source files` |
| `refactor` | Tái cấu trúc, không đổi hành vi | `refactor(product): simplify product validation` |
| `perf` | Cải thiện hiệu năng | `perf(report): optimize inventory query` |
| `test` | Thêm/sửa kiểm thử | `test(export): cover insufficient stock` |
| `chore` | Cấu hình, dependency, bảo trì | `chore(agent): add shared project instructions` |

Không ghi thông điệp chung chung như `update`, `fix all`, `done`. Không đưa mật khẩu hoặc dữ liệu nhạy cảm vào commit/PR.

## 10. Hướng dẫn sử dụng Copilot / AI Agent

### 10.1. Bật quy tắc dùng chung

File `.github/copilot-instructions.md` được commit cùng repository. Sau khi pull, mỗi thành viên tự bật trên Visual Studio của mình:

1. Vào **Tools → Options**.
2. Tìm **GitHub → Copilot → Copilot Chat**; giao diện mới có thể nằm trong **All Settings**.
3. Bật **Enable custom instructions** hoặc tùy chọn đọc `.github/copilot-instructions.md` tương ứng.
4. Mở cuộc trò chuyện mới.
5. Kiểm tra **References** trong câu trả lời có file hướng dẫn.

`.agent/RULE.md` là bản cá nhân, không tự thay thế file Copilot đọc và không được push. Khi sửa quy tắc dùng chung, tạo PR để nhóm review; đồng bộ bản cá nhân nếu đang giữ. Không dùng bản contract cũ hoặc snapshot audit để ghi đè quyết định mới đã được người dùng/team xác nhận.

### 10.2. Cách giao việc

Mỗi prompt cần có:

- **Người/phạm vi:** làm theo phần của ai.
- **Mục tiêu:** chức năng cụ thể cần hoàn thành.
- **File/contract:** file liên quan, đầu vào/đầu ra, mẫu đang dùng.
- **Ràng buộc:** giữ công nghệ, quyền sửa, quy tắc nghiệp vụ.
- **Kiểm tra:** build, test và tiêu chí đạt.

Agent được đọc tầng khác để hiểu hợp đồng. Sửa sang phần khác hoặc thêm gói chưa được duyệt phải được cho phép rõ ràng. Không bật quyền rộng chỉ để agent vượt qua quy tắc của nhóm.

### 10.3. Prompt cho Khang

```text
Làm theo phạm vi của Khang: Backend + Warehouse Operations.
Đọc .github/copilot-instructions.md trước khi sửa.

Mục tiêu: triển khai ExportService theo hợp đồng và mô hình đã có.
Chỉ sửa phần Business và các test nghiệp vụ được giao.

Yêu cầu:
- Kiểm tra số lượng hợp lệ và đủ tồn tại đúng kho.
- Mỗi phiếu chỉ giảm tồn một lần.
- Phiếu, tồn và lịch sử được lưu nhất quán.
- Phát sự kiện sau commit; UI refresh lỗi không được xuất lại.
- Không tự sửa Data, UI hoặc thêm thư viện.

Kiểm tra: xuất 50 khi chỉ có 20 phải bị từ chối, không đổi tồn.
Trước khi sửa, liệt kê file dự kiến thay đổi rồi thực hiện.
Nếu cần thay đổi ngoài phạm vi, báo hợp đồng cần bổ sung.
Sau cùng, build/test phù hợp và báo kết quả thực tế.
```

### 10.4. Prompt cho Bảo Anh

```text
Làm theo phạm vi của Bảo Anh: Database + Architecture.
Đọc .github/copilot-instructions.md trước khi sửa.

Mục tiêu: hoàn thiện mô hình tồn theo sản phẩm và kho dựa trên
schema và hợp đồng đã được nhóm thống nhất.

Chỉ sửa Data và SQL script liên quan. Bảo đảm khóa, FK,
constraint và cơ chế concurrency phù hợp. Không tạo Trigger
tự tăng/giảm Inventory, không sửa Business/UI hoặc tự chạy
script làm thay đổi database thật.

Nếu hợp đồng chung cần đổi, nêu ảnh hưởng để phối hợp.
Build và kiểm tra phù hợp; báo phần chưa kiểm chứng.
```

### 10.5. Prompt cho Khiêm

```text
Làm theo phạm vi của Khiêm: UI + Dashboard + Analytics.
Đọc .github/copilot-instructions.md trước khi sửa.

Mục tiêu: hoàn thiện màn hình tồn kho, dùng Service/DTO đã có.
Chỉ sửa UI và phần kiểm thử giao diện liên quan.

Hiển thị sản phẩm, kho, số lượng, đơn vị và trạng thái trả về
từ Business. Dùng control WinForms/thư viện đã được duyệt.
UI tiếng Việt, có tìm kiếm/lọc phù hợp, trạng thái tải/rỗng/lỗi,
TabIndex hợp lý và layout không cắt chữ khi đổi DPI.

Không truy vấn DbContext trực tiếp từ Form, không tính lại
StockStatus, không tự sửa Business/Data hoặc cài gói UI mới.
Nếu Service còn thiếu, báo hợp đồng cần có, không dùng dữ liệu
giả để tuyên bố chức năng đã hoàn thành.

Build, kiểm tra Designer và chạy UI nếu có môi trường.
```

### 10.6. Kiểm tra code do AI tạo

- Xem danh sách file đổi và `git diff` trước khi commit.
- Kiểm tra không sửa ngoài phân công hoặc cài gói không được duyệt.
- Kiểm tra không có code giả, lỗi bị nuốt, mật khẩu hard-code hoặc dữ liệu mất khi thất bại.
- Đối chiếu điều kiện tồn và transaction bằng test phù hợp.
- Build thành công không thay thế review nghiệp vụ hoặc kiểm tra giao diện.
- Nếu agent chưa chạy được kiểm tra, PR phải nêu rõ giới hạn đó.

## 11. Kiểm thử và điều kiện trước PR

Chạy tại thư mục solution:

```powershell
dotnet restore
dotnet build --no-restore
```

Project test MSTest hiện có. Chạy unit tests sau build:

```powershell
dotnet test SmartInventoryManager.Tests/SmartInventoryManager.Tests.csproj --no-build --filter "TestCategory!=SQLServer"
```

Ba SQL tests là opt-in. Chỉ khi đã được phép dùng target TEST và ghi/dọn fixture, cấu hình connection riêng rồi chạy:

```powershell
$env:SMART_INVENTORY_SQL_TEST_CONNECTION_STRING = 'Server=localhost;Database=SmartInventoryManagerDB;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True'
$env:SMART_INVENTORY_SQL_TESTS_ALLOW_MUTATION = '1'
dotnet test SmartInventoryManager.Tests/SmartInventoryManager.Tests.csproj --no-build --filter "TestCategory=SQLServer"
```

Suite hiện chỉ cho phép chính xác `localhost/SmartInventoryManagerDB`, cần migration đã apply và ADMIN active; dùng fixture riêng và tự dọn, không drop/reset DB. Không có đủ biến opt-in thì SQL tests báo inconclusive/skipped, không phải PASS. Không đổi guard để chạy trên production. Nếu target test của team thay đổi, phải thống nhất và cập nhật suite trước.

Dùng analyzer và `.editorconfig` của repository; kiểm tra whitespace/style trên file sửa, không tự format toàn bộ code của đồng đội. Không tắt audit/analyzer để che cảnh báo; review dependency rồi chọn bản vá tương thích được duyệt. Kết quả lần nghiệm thu gần nhất ở mục 2.1 là bằng chứng đã chạy, không phải kết quả mỗi lần clone/build.

- Unit test: Business, validation, tính tồn/trạng thái, cảnh báo, phân quyền khi có thể kiểm tra độc lập.
- Integration test: dùng database test riêng cho truy vấn, lưu dữ liệu, constraint, transaction và concurrency.
- Không dùng EF InMemory để kết luận hành vi SQL Server đã đúng.
- Không chạy test ghi dữ liệu trên database thật.
- UI: kiểm tra mở Designer, chạy tác vụ bị ảnh hưởng, bàn phím, tiếng Việt dài, resize và scaling 100/125/150/200% khi có môi trường.

Checklist trước PR:

- [ ] Đúng phạm vi; thay đổi liên tầng đã được thống nhất.
- [ ] Không thêm công nghệ/dependency chưa được duyệt.
- [ ] Build đạt; test liên quan đạt hoặc nêu rõ chưa chạy.
- [ ] Không có lỗi/cảnh báo mới do thay đổi; lỗi cũ được ghi rõ.
- [ ] Không commit bí mật, file máy cá nhân hoặc trạng thái agent.
- [ ] UI dùng tiếng Việt; chú thích `[COLOR CONFIG]` được giữ nếu sửa màu.
- [ ] PR mô tả hành vi thực tế, không báo chức năng khung là hoàn thiện.

## 12. File không commit và cấu hình cá nhân

`.gitignore` tối thiểu cần phù hợp cấu trúc hiện có:

```gitignore
.vs/
**/bin/
**/obj/
.agent/
*.user
*.suo
*.mdf
*.ldf
```

Các file cấu hình local chứa bí mật phải được bỏ qua theo tên thật nhóm chọn. Không bỏ qua toàn bộ `.github/`: nhóm cần chia sẻ `.github/copilot-instructions.md` và cấu hình cộng tác.

Nếu trước đây đã bỏ qua file Copilot trong `.git/info/exclude`, bỏ dòng đó khi nhóm quyết định chia sẻ file qua Git. Ignore không tự ngừng theo dõi file đã commit; xử lý với người phụ trách repository nếu cần.

## 13. Một số lỗi thường gặp

| Vấn đề | Cách kiểm tra |
|---|---|
| Không tìm thấy SDK / NETSDK1045 | Kiểm tra `dotnet --list-sdks`, target framework và global.json; cài SDK đúng |
| Restore thất bại | Kiểm tra mạng, nguồn NuGet và phiên bản gói; không tự đổi stack |
| Gói EF không tương thích | Đối chiếu target framework; các gói EF dùng cùng phiên bản cụ thể |
| Không kết nối SQL Server | Kiểm tra dịch vụ/instance, database, cách xác thực và cấu hình ứng dụng |
| Thiếu bảng hoặc cột | Kiểm tra database/schema đã tạo đúng bản, không tự chạy migration chồng script |
| Copilot chưa thấy quy tắc | Kiểm tra file ở gốc repository, tên .md, bật custom instructions và mở chat mới |
| Form Designer báo lỗi | Kiểm tra build, constructor và file Designer; không gọi database trong luồng thiết kế Form |
| Git pull không fast-forward | Dừng, kiểm tra nhánh/lịch sử và phối hợp nhóm; không force/reset để bỏ lỗi |

## 14. Tài liệu tham khảo

- [Tạo ứng dụng Windows Forms với .NET](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/get-started/create-app-visual-studio)
- [Cài đặt Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/install)
- [Tùy chỉnh Copilot Chat trong Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=visualstudio)

Mọi thay đổi về công nghệ, nghiệp vụ và phân công dùng chung cần được cập nhật trong tài liệu/quy tắc qua PR, tránh để mỗi thành viên áp dụng một phiên bản khác nhau.

### Bổ sung nghiệp vụ 15/10/2026

Người dùng chốt audit đầy đủ chuyển trạng thái: latest người/thời gian gửi duyệt, duyệt, hủy; ReceiptStatusChanges lưu mỗi chuyển trạng thái với actor/time/from/to/reason, không snapshot detail hoặc đoán dữ liệu cũ. Giữ lịch sử từ chối hiện có và không hard-delete phiếu đã có history. UI/PDF hiển thị latest audit; history xử lý20/trang. Best-effort và giá trị tồn ước tính giữ nguyên; WAC giai đoạn2. Kết quả kiểm tra của đợt14/10 không tự áp dụng cho code mới. Xem [báo cáo lifecycle](Documentation/Architecture/ProjectStatus.md).

Bổ sung sau rà soát15/10: bảo vệ namespaceusername/email, ADMINcuốicùng và managerđangphânkho; serverpaging phiếu/user/log/notification và lookup khôngcắt200. Ghi nhớ chỉ lưuusername, không password/token/autologin; ẩn nútđổiavatar; kiểm kêtheophiên cùngWAC đểgiaiđoạn2.
