# SMART INVENTORY MANAGER

Ứng dụng desktop quản lý kho dành cho doanh nghiệp/cửa hàng có một hoặc nhiều kho. Dự án phục vụ môn Lập trình Windows Forms, sử dụng C#, .NET và SQL Server.

> Tài liệu này mô tả cách tham gia và phát triển dự án. Danh sách chức năng là phạm vi dự kiến, không phải xác nhận tất cả đã hoàn thiện. File hoặc màn hình khung chưa có nghiệp vụ được xem là chưa triển khai.

## 1. Công nghệ sử dụng

| Thành phần | Công nghệ / quy định |
|---|---|
| Hệ điều hành | Windows phù hợp yêu cầu hỗ trợ của .NET và Visual Studio; ưu tiên Windows 11 |
| Ngôn ngữ | C# |
| Nền tảng mặc định | **.NET 10**, không phải “.NET Framework 10” |
| Giao diện | Windows Forms |
| Môi trường phát triển mặc định | Visual Studio 2026, workload `.NET desktop development` |
| Database | Microsoft SQL Server; instance local phù hợp cho phát triển |
| ORM | Entity Framework Core 10.x nếu solution dùng .NET 10 |
| Truy vấn | LINQ |
| Truy cập SQL bổ sung | ADO.NET / Microsoft.Data.SqlClient |
| Quản lý thư viện | NuGet, thông qua Visual Studio hoặc dotnet CLI |
| Quản lý mã nguồn | Git / GitHub |

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
| `Database/` | Script tạo và cập nhật database |
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

Nếu nhóm sử dụng nhánh `develop` và nhánh đã tồn tại trên remote:

```powershell
git switch develop
git pull --ff-only origin develop
```

Không tự tạo `develop` trên remote nếu nhóm chưa thiết lập. Nếu nhóm chỉ dùng `main`, dùng `main` theo cùng quy trình.

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

1. Lấy bản SQL script/schema được Bảo Anh xác nhận đã hoàn thiện.
2. Kết nối đúng SQL Server instance trên máy bằng SSMS hoặc công cụ nhóm đang dùng.
3. Chạy script theo thứ tự và hướng dẫn của bản schema đó. Các file được đánh số `01` đến `08` trong `Database/` là cấu trúc dự kiến; file chỉ có comment chưa phải script hoạt động.
4. Seed data chỉ chạy khi đã được nhóm duyệt. Không tự tạo tài khoản/mật khẩu chung hoặc chạy seed nhiều lần gây trùng dữ liệu.
5. Cấu hình connection string bằng cơ chế ứng dụng đã triển khai. Nếu chưa có cơ chế đọc cấu hình, cần hoàn thiện với người phụ trách; không tự tạo `appsettings.json` hoặc mặc định `App.config` sẽ được đọc.
6. Không tự chạy `Update-Database`, migration và script tạo schema cùng lúc. Nhóm chọn một nguồn quản lý schema rõ ràng.

Ví dụ connection string local, thay database/instance bằng tên thật đã thống nhất:

```text
Server=(localdb)\MSSQLLocalDB;Database=SmartInventoryDb;Integrated Security=True;
```

Hoặc nếu máy đã có instance SQL Server Express tên `SQLEXPRESS`:

```text
Server=.\SQLEXPRESS;Database=SmartInventoryDb;Integrated Security=True;
```

Đây là ví dụ kết nối, không khẳng định database hoặc instance này đã có trên máy. Thông tin đăng nhập và cấu hình chứa bí mật phải được lưu theo cơ chế local của nhóm, không commit.

### 6.5. Chạy ứng dụng

Nhấn **F5** trong Visual Studio, hoặc nếu project có đúng tên/đường dẫn bên dưới:

```powershell
dotnet run --project SmartInventoryManager.UI/SmartInventoryManager.UI.csproj
```

Nếu project UI đang có tên khác, dùng đường dẫn `.csproj` thực tế. Tài khoản demo do nhóm cung cấp sau khi phần đăng nhập và seed đã hoàn thiện. Màn hình khung chạy được chưa chứng minh chức năng nghiệp vụ đã hoàn thành.

## 7. Quy tắc nghiệp vụ quan trọng

- Tồn kho theo sản phẩm + kho, không chỉ theo sản phẩm.
- Business Service là nơi điều phối cập nhật tồn. Không dùng Trigger tự tăng/giảm Inventory.
- Phiếu, chi tiết, tồn và lịch sử phải nhất quán trong transaction.
- Một phiếu chỉ tác động tồn một lần; chống gửi lại và thao tác đồng thời.
- Không xuất vượt tồn hoặc cho tồn âm khi chưa có nghiệp vụ cho phép.
- Sự kiện cập nhật UI/cảnh báo phát sau commit thành công.
- Điều chỉnh phải có lý do, người thực hiện và số lượng trước/sau.

Ngưỡng cấu hình hợp lệ: `0 <= ReorderPoint < MaximumStock`.

| Điều kiện | Trạng thái |
|---|---|
| `Quantity = 0` | `OUT_OF_STOCK` |
| `0 < Quantity <= ReorderPoint` | `LOW_STOCK` |
| `ReorderPoint < Quantity <= MaximumStock` | `NORMAL` |
| `Quantity > MaximumStock` | `OVERSTOCK` |

Tính trạng thái tại Business; UI hiển thị kết quả bằng tiếng Việt. Không hard-code ngưỡng demo hoặc tính lại công thức khác ở biểu đồ.

## 8. Quy trình Git của nhóm

Quy trình đề xuất: `main` chứa bản ổn định, `develop` tích hợp công việc, các nhánh `feature/*` hoặc `fix/*` dành cho từng nhiệm vụ. Nếu repository đã chọn quy trình khác, giữ quy trình đó đến khi nhóm thống nhất đổi.

### 8.1. Bắt đầu nhiệm vụ

Trước khi chuyển nhánh, chạy `git status` và lưu/commit công việc hiện tại. Không pull khi còn thay đổi chưa xử lý.

```powershell
git status
git switch develop
git pull --ff-only origin develop
git switch -c feature/khang-import
```

Ví dụ nhánh:

- `feature/baoanh-database`
- `feature/baoanh-architecture`
- `feature/khang-import`
- `feature/khang-export`
- `feature/khang-inventory`
- `feature/khiem-ui`
- `feature/khiem-dashboard`
- `fix/export-stock-validation`

Không code trực tiếp trên `main`. Không sửa nhánh đang được thành viên khác dùng.

### 8.2. Phát triển và commit

- Làm đúng phạm vi, commit theo từng thay đổi có ý nghĩa.
- Build và chạy test liên quan trước khi push.
- Xem `git diff`; chỉ stage file thuộc nhiệm vụ. Tránh `git add .` khi repository còn thay đổi không liên quan hoặc cấu hình cá nhân.

Ví dụ, thay đường dẫn bằng file thực tế đã sửa:

```powershell
git status
git diff
git add SmartInventoryManager.Business/Services/ImportService.cs
git commit -m "feat(import): implement stock import workflow"
```

### 8.3. Cập nhật từ nhánh tích hợp

Sau khi lưu/commit công việc và working tree sạch:

```powershell
git fetch origin
git merge origin/develop
```

Nếu có conflict:

1. Kiểm tra các file xung đột; phối hợp người phụ trách khi cần.
2. Sửa theo logic đúng, không chọn toàn bộ một phía chỉ để hết conflict.
3. Stage file đã xử lý và hoàn tất merge.
4. Build/test lại.

Không dùng `reset --hard` hoặc force push để giải quyết conflict. Hướng dẫn này dùng merge; nếu nhóm chọn rebase thì thống nhất trước.

### 8.4. Push và tạo Pull Request

```powershell
git push -u origin feature/khang-import
```

Tạo PR trên GitHub:

- **Base:** `develop`.
- **Compare:** nhánh feature/fix đang làm.
- Mô tả vấn đề, thay đổi, cách kiểm tra và phần chưa hoàn thành.
- Nhờ ít nhất một thành viên review; thay đổi liên tầng cần người phụ trách liên quan xem.
- Merge khi đã xử lý ý kiến và các kiểm tra yêu cầu đạt.
- Xóa nhánh feature sau merge khi nhóm không còn cần; không xóa nhánh của người khác.

Sau một đợt tích hợp ổn định, nhóm review và tạo PR từ `develop` sang `main`. Nếu nhóm chỉ dùng `main`, thay `develop` bằng `main` trong các bước tương ứng.

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

`.agent/RULE.md` là bản cá nhân, không tự thay thế file Copilot đọc. Khi sửa quy tắc dùng chung, tạo PR để nhóm review; đồng bộ bản cá nhân nếu đang giữ. Bản dịch tiếng Việt trong Documentation có thể lưu cùng repo để tham khảo.

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

Khi đã có project test được build và framework test được duyệt:

```powershell
dotnet test --no-build
```

Nếu chưa có test, không coi việc lệnh chạy không lỗi là bằng chứng nghiệp vụ được kiểm thử. Dùng analyzer và `.editorconfig` của repository; chỉ chạy kiểm tra định dạng khi SDK/cấu hình hỗ trợ, không tự format toàn bộ code của đồng đội.

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
