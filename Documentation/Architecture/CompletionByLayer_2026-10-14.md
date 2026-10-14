## Cập nhật hiện hành ngày 14/10/2026 — sau nghiệm thu

Phần này có ưu tiên so với các snapshot lịch sử phía dưới. Chuẩn là `.agent/SystemContract.md` hiện hành; người dùng đã cho phép hoàn thiện cả ba tầng, commit theo từng tầng và dùng `localhost / SmartInventoryManagerDB` làm DB TEST. Server production riêng chưa được cung cấp. SQL cũ được giữ nguyên; không drop/reset database. Các commit code/tài liệu đến `e0efd85` đã push Backend theo yêu cầu người dùng; remote develop đã nhận qua PR #50. README/rule/checklist tiếp tục được đồng bộ theo các quyết định đã chốt.

| Tầng | Công việc đã triển khai, từ dễ đến khó | Commit |
|---|---|---|
| Data | Ngưỡng `0 < ReorderPoint < MaximumStock`; assignment IsActive; hai migration đã apply test. Query ADO.NET daily movement có parameters, scope kho và CreatedAt | `7282f26`, `455f838`, `3af355d` |
| Business | Assignment active/revoke/reactivate; totals/export full filter; Dashboard 30 ngày gồm hôm nay, top 10 dòng sản phẩm–kho theo Quantity rồi mã sản phẩm/kho, recent20 với actor/reference receipt | `1e7463e`, `413e69b`, `28df10a` |
| Business | Posting receipt nội bộ qua InventoryService, cùng context/transaction; log nghiệp vụ trong transaction; publisher chung sau commit, ReceiptCompleted tương thích, subscriber lỗi không replay stock | `28df10a` |
| Business | SQL1205 retry tối đa hai lần khi SQL xác nhận rollback, transaction mới và clear tracker; không retry timeout/lost connection/unknown commit. Hết retry có BusinessConflictException/CONCURRENCY_RETRY_EXHAUSTED | `28df10a` |
| Business | Cảnh báo sau commit best-effort cho ADMIN active và manager active đúng kho; không tự replay stock. Log có ở completion/adjust/transfer/product update-active/assignment, chưa phủ mọi CRUD | `28df10a` |
| Tests | Ba SQL integration tests opt-in: posting/duplicate completion/insufficient-stock rollback/log-notification, deadlock thật và Dashboard ADO.NET | `bf57c25` |
| Setup | CLI bootstrap tương tác riêng, 3 role, ADMIN đầu tiên, PBKDF2; không auto-seed khi mở UI; --check chỉ đọc, ADMIN active hiện hữu không bị ghi đè | `381ba7a`, `8941447` |
| UI | Dashboard grouped daily chart, bốn trạng thái, top10 horizontal, recent actor/ref; log ADMIN; menu STAFF hạn chế; đổi kho xóa cache, cập nhật footer và giới hạn cảnh báo theo current kho | `d904a20` |
| UI | PDF từng phiếu nhập/xuất A4 từ chi tiết được Business kiểm tra scope; mã/ngày/trạng thái/kho/người tạo/sản phẩm–SL–đơn giá–thành tiền; lặp table header, giữ nguyên dòng qua trang. In bằng trình đọc PDF, chưa có print dialog trực tiếp | `d904a20` |

### Kết quả kiểm chứng

- Solution 6 projects build thành công, 0 lỗi; 1 cảnh báo NU1900 do dịch vụ NuGet vulnerability không truy cập được. Không tắt NuGet audit để che cảnh báo.
- Unit tests: **142 PASS, 0 fail, 0 skip**. SQL opt-in tests: **3 PASS, 0 fail, 0 skip** trên DB TEST. Chạy ngoài sandbox vì IPC/testhost và kết nối SQL bị chặn trong sandbox; các lần sandbox aborted không được tính là pass.
- SQL probe sau cleanup: kết nối được, pending migration rỗng, sản phẩm ReorderPoint<=0 bằng0. Bằng chứng kiểm tra model/17 entity trước đó vẫn là lịch sử đã chạy, không phải test mới trong lượt cuối.
- Bootstrap --check xác nhận localhost/SmartInventoryManagerDB có ADMIN active; không ghi đè. Unit chứng minh tạo3roles/hash/log và từ chối mật khẩu sai. Chưa nghiệm thu nhập bootstrap tương tác trên DB SQL trắng.
- Native UI ADMIN: manager names/vị trí lọc theo kho; tìm STAFF fixture; deactivate assignment B giữ2rows, reactivate giữ2rows; user tự lưu fullname và reset password, đăng nhập STAFF thành công.
- Native UI STAFF: không có menu user/log; kho A tồn0/Hết hàng, đổi sang B tồn5; sau sửa Dashboard cả hai kho tải được và footer cập nhật B. Fixture `UITEST_f23c6de6`/hai kho test đã cleanup theo lựa chọn của người dùng; dữ liệu demo giữ nguyên.
- WinForms smoke PASS: 10 nhãn, manager ComboBox, khởi tạo form user. Chưa nghiệm thu Designer/keyboard/DPI đầy đủ hoặc mọi CRUD ở cả3role.
- Export QA: 3 PDF và2Excel sinh thành công; mở lại Excel kiểm tra số liệu đúng dataset. PDF phiếu60dòng/A4/5trang và2reportPDF đã render và xem PNG; sửa row splitting rồi xem lại cả5trang. Chưa nghiệm thu SaveFileDialog trên UI, máy in thật hay dataset rất lớn.
- Whitespace check cho13file sửa và style check cho5file bootstrap/PDF PASS; workspace load có warning NuGet. Không khẳng định toàn repository đã đạt format.
- Harness local SQL/fixture từng restore có NU1902/NU1903 cho transitive System.Security.Cryptography.Pkcs9.0.18. Cần review nâng bản vá tương thích đã được team duyệt; chưa đổi dependencies.

### Người dùng chọn bỏ qua để bàn với team — không tự triển khai

| Nhóm | Câu hỏi cần chốt | Các tầng ảnh hưởng |
|---|---|---|
| Audit phiếu | Có thêm CompletedBy/CompletedAt, CancelReason/RejectReason riêng? Giữ reject gần nhất qua edit/resubmit hay lịch sử đầy đủ? | Data fields/migration, Business lifecycle, UI và PDF |
| Adjustment/transfer | Có BeforeQuantity/AfterQuantity/SignedDifference? Cấp ReferenceId chung cho hai movement bằng SQL sequence hay phương án khác? | Data model/allocator, Business transaction, history UI |
| Định giá/analytics | Công thức valuation, ABC, Dead Stock, Reorder Recommendation và mốc thời gian nào? | Business query/metrics, Data, UI/report |

### Việc tiếp theo còn cần hoàn thiện, theo độ khó

| Thứ tự | Việc cần làm | Điều kiện/giới hạn |
|---|---|---|
| 1 | Nghiệm thu SaveFileDialog/PDF viewer/print thật; Designer và DPI/keyboard; manager UI và CRUD còn lại | Chạy checklist UI bổ sung; không dùng smoke thay nghiệm thu toàn bộ |
| 2 | Review cảnh báo dependency và test bản vá tương thích | Không tự nâng framework/major hoặc chọn package khi chưa review |
| 3 | Bổ sung SQL races different exports/transfer và complete-vs-cancel/edit, failure injection | Fixture riêng; suite3tests hiện tại chưa chứng minh hết các races |
| 4 | Nghiệm thu bootstrap tương tác với SQL database trắng, chuẩn bị deployment/recovery riêng | Target/quyền môi trường riêng được xác nhận; không vô hiệu hóa ADMIN demo để test |
| 5 | Đồng bộ ERD/class/use-case/demo tài liệu cuối, paging/performance large datasets nếu yêu cầu | Schema cuối phụ thuộc ba quyết định team; hiện export trả toàn bộ filter, không cam kết snapshot giữa query đồng thời |
| 6 | Triển khai ba nhóm nghiệp vụ bị bỏ qua ở bảng trên | Chỉ làm sau khi chủ dự án/team trả lời, không đoán công thức/schema |

README, `.github/copilot-instructions.md` và checklist được đồng bộ trong đợt tài liệu được người dùng yêu cầu push; `.agent/RULE.md` đồng bộ local, không đưa vào Git. Connection launchSettings có trước giữ local ngoài commit. Dự án đã tiến thêm ở CORE, chưa chứng nhận hoàn thiện toàn bộ phạm vi hợp đồng.

---

## Snapshot lịch sử — đọc cùng cập nhật hiện hành ở trên

# Tiến độ hoàn thiện theo ba tầng — 14/10/2026

Chuẩn: `.agent/SystemContract.md` mới, được người dùng chọn ngày 14/10/2026. Người dùng hiện đảm nhận toàn bộ ba tầng và cho phép sửa liên tầng, commit riêng từng tầng. Triển khai từ dễ đến khó; quyết định nghiệp vụ chưa đủ bằng chứng được để mở. SQL cũ trong `Database/` giữ nguyên. Báo cáo này ghi một đợt triển khai, không chứng nhận toàn bộ dự án đã hoàn thiện.

## 0. Cập nhật sau quyết định của chủ dự án

### Xác minh DB test và apply migration

Người dùng xác nhận SQL Server đang kết nối là môi trường test, server chính nằm riêng. Đã xác minh target từ launch profile: **localhost / SmartInventoryManagerDB**, rồi apply hai migration `20261014010000_RequirePositiveReorderPoint` và `20261014090948_AddUserWarehouseActiveState`. Trước apply có 0 sản phẩm ReorderPoint<=0. Sau apply không còn pending migration; 10 assignments hiện hữu giữ nguyên và đều active. Không truy cập server chính.

SQL acceptance PASS: active assignment cấp scope; ngừng thu hồi current scope và giữ row; kích hoạt lại khôi phục scope; query report inventory filtered totals/rows/open alerts; unmatched filter trả 0; receipt report date/search/type trước aggregate; alert sau pending stock mutation lưu đúng quantity/type. Dữ liệu fixture riêng được rollback, kiểm tra không còn test user/product; schema migrations đã apply vẫn được giữ. Không drop/reset DB và không xóa SQL cũ. Harness local `.agent/SqlAcceptance` chưa phải integration suite trong repo/CI. Trước lượt bổ sung bên dưới chưa kiểm thử vòng receipt/race; UI save/DPI hoặc render file export vẫn chưa nghiệm thu.

Đã kiểm tra thêm vòng receipt trên fixture riêng: create ở context1, Submit/Approve/Complete nhập ở context mới; quantity5 và một movement. Hai context cùng Complete một phiếu xuất quantity2: đúng một succeeds, stock còn3, chỉ một export movement. Thao tác cạnh tranh nhận **SQL deadlock1205** qua InvalidOperationException wrapper; chưa có stable Business error contract/retry policy nên chưa coi giao diện xử lý conflict đạt yêu cầu. Phiếu xuất quantity4 khi tồn3 bị từ chối; trạng thái vẫn APPROVED, stock/movements không đổi. Cleanup transaction chỉ xóa records trong warehouse/product/category/supplier WFTEST có ID/code riêng; dữ liệu nghiệp vụ hiện hữu giữ nguyên. Tổng 17 assertions SQL PASS ở hai nhóm; xác minh lại EF CanConnect=True, pending migrations rỗng, pending model=False và cả17 entity query PASS. Không chạy race complete-vs-cancel/update/delete/approve, concurrent different exports/transfer hoặc failure injection từng stage.

Restore harness local có NuGet audit cảnh báo NU1902/NU1903 về dependency transitive System.Security.Cryptography.Pkcs 9.0.18; chưa nâng package sản phẩm ngoài phạm vi đợt này.

Ban đầu người dùng xác định tên DB là `SmartInventoryManagerDB`, sau đó xác nhận connection hiện tại là DB test; server chính riêng chưa được cung cấp. Chọn giữ assignment với trạng thái ngừng hoạt động. Chọn tổng báo cáo và export theo toàn bộ dữ liệu khớp kho/ngày/search/type. Các mục 1–5 bên dưới giữ bằng chứng đợt đầu; những trạng thái được cập nhật ở mục này có ưu tiên hiện hành.

| Tầng | Đã triển khai thêm | Commit |
|---|---|---|
| Data | UserWarehouse.IsActive; migration mới AddUserWarehouseActiveState, mặc định true cho assignments hiện hữu; PK/FK giữ nguyên | `455f838` |
| Business | ADMIN phân kho/ngừng/kích hoạt lại; activation chỉ cho STAFF active/kho active; ngừng giữ row, chỉ active assignment cấp quyền kho | `1e7463e` |
| Business | Report search/type/status được query trước totals và rows; bỏ cap5000 để trả đủ dữ liệu; filtered alert count theo inventory scope | `1e7463e`, `413e69b` |
| UI | ADMIN màn phân kho, chọn kho active không cắt danh sách; ngừng/kích hoạt lại. STAFF đổi kho và xóa cache pages sau đổi. Report summary/export truyền cùng kho/ngày/search/type/status | `616d510` |

Xác minh code: build0 warning/error; **138 unit tests PASS**, gồm ngừng/revoke/reactivate/deny nonadmin/deny assigning admin, search/type/status totals và >5000 rows trả đủ. EF snapshot/script PASS. Smoke WinForms controls PASS. Migration và SQL acceptance đã chạy trên DB test được người dùng xác nhận ở phần đầu mục0; các bước GUI và SQL concurrency vẫn chưa nghiệm thu. Chưa push.

Chưa tự quyết định việc bỏ/giữ assignments khi đổi role hoặc user/kho inactive: auth vẫn từ chối tài khoản/kho không hợp lệ; rows giữ nguyên, service cho ngừng assignment cũ nhưng kích hoạt lại phải thỏa role/active. Chưa thêm audit fields, outbox hay công thức analytics. Phân trang hiển thị có thể bổ sung sau; hiện Business trả đủ tập khớp filter theo quyết định người dùng, không cam kết snapshot giữa nhiều query khi có concurrent writes.

## 1. Data — đã triển khai trong đợt này

Commit `7282f26`: `fix(data): migrate positive reorder threshold and align connection target`.

- ProductConfiguration và snapshot yêu cầu `ReorderPoint > 0`; giữ `MaximumStock > ReorderPoint`.
- Thêm migration tiến lên `20261014010000_RequirePositiveReorderPoint`; không sửa baseline đã apply.
- Migration kiểm tra dữ liệu hiện hữu và THROW trước khi thay CHECK nếu có ReorderPoint <=0. Không tự sửa dữ liệu. Down trả CHECK về >=0.
- Design-time factory đọc `SMART_INVENTORY_CONNECTION_STRING`; fallback localhost/SmartInventoryManagerDB. App runtime vẫn có App.config fallback riêng; phải xác định đúng target khi triển khai.
- EF kiểm tra không có pending model changes; sinh SQL review thành công. **Chưa apply migration trên DB.**

Việc còn lại theo độ khó:

| Thứ tự | Công việc | Điều kiện còn thiếu |
|---|---|---|
| D1 | Xác minh server/database triển khai; rà sản phẩm ngưỡng 0, apply migration và kiểm tra history/CHECK | Target DB và cách sửa dữ liệu 0 nếu có phải được quyết định |
| D2 | Bootstrap role/admin, seed chạy lại an toàn; cập nhật hướng dẫn setup chính thức | Chốt provisioning admin và phạm vi dữ liệu demo; không dùng hash cố định chung |
| D3 | DB SQL test riêng, setup/seed/recovery và kiểm thử migration/rollback/concurrency | Tên DB test, connection và quyền reset cụ thể |
| D4 | Rà indexes trùng và artifacts cũ; quyết định query/ADO.NET cho report thực tế | Không xóa SQL; review DB/model và use case, metric/query contract |
| D5 | Bổ sung audit completion/cancel/reject/adjustment và shared transfer reference | Audit fields/semantics, reference allocator và lifecycle cần chốt trước |
| D6 | ERD, Architecture và tài liệu model | Hoàn thiện theo schema cuối sau các quyết định trên |

## 2. Business — đã triển khai trong đợt này

Commit `fcbf152`: `fix(business): enforce positive thresholds and supply warehouse display data`.

- Validation chung StockStatusCalculator/ProductValidator yêu cầu `0 < ReorderPoint < MaximumStock`, thông báo tiếng Việt; số lượng 0 vẫn là OUT_OF_STOCK với cấu hình hợp lệ.
- Tests mới từ chối ReorderPoint=0, threshold âm/bằng nhau; giữ kiểm thử số lượng tại các mốc ngưỡng và cấu hình tối thiểu 1/2.
- WarehouseDto.ManagerName và LocationDto.WarehouseName được query từ quan hệ thật; không đổi Entity/schema cho display names.
- WarehouseService.GetActiveManagersAsync chỉ trả WAREHOUSE_MANAGER active; có role/active-actor guard trước lookup. UI không dùng user list cap500 để suy danh sách manager đầy đủ.
- Regression xác minh display names và chỉ manager hợp lệ được trả về.

Việc còn lại theo độ khó:

| Thứ tự | Công việc | Điều kiện còn thiếu |
|---|---|---|
| B1 | Nghiệm thu SQL các sửa Submit/alerts/state/actor/category/manager/ngưỡng | DB test riêng; 136 unit tests không chứng minh SQL lock/race |
| B2 | Stable error codes và cách trả lỗi/reload/retry không replay | Chọn compatibility typed error/result và SQL error mapping |
| B3 | ADMIN quản trị phân kho STAFF và đổi current context | Xóa assignment hay deactivate; xử lý role/kho/user bị thu hồi |
| B4 | Tập trung stock posting qua InventoryService; official events sau commit và shared publisher | Boundary nội bộ, compatibility ReceiptCompletedEvent, consumer/failure contract |
| B5 | Lý do hủy/từ chối, completion actor/time; before/diff/after adjustment; reference chuyển kho | D5; không nhét metadata vào Note hoặc tạo mã ngẫu nhiên |
| B6 | Notification/SystemLog producer | Recipient, mandatory transaction hay best-effort, retry policy |
| B7 | Report paging/export, Dashboard và analytics contracts | Full-scope summary vs screen filter; date/range/topN/ties/valuation/ABC/dead-stock/reorder chưa chốt |

## 3. UI — đã triển khai trong đợt này

Commit `ea86338`: `fix(ui): complete warehouse selectors and admin user actions`.

- StockStatus enum chỉ format một lần; tránh đổi nhãn tiếng Việt thành “Chưa xác định”.
- Bổ sung nhãn PENDING_APPROVAL/APPROVED/REJECTED.
- ManagerName/WarehouseName bind được từ DTO Business vừa bổ sung.
- Form kho dùng ComboBox manager, nạp active managers và lưu ManagerId; giữ manager cũ không hợp lệ trong lựa chọn để tránh tự xóa assignment. Business vẫn từ chối manager không hợp lệ khi Save. Save bị khóa nếu tải danh sách manager thất bại.
- Thêm bộ lọc kho cho danh sách vị trí, sử dụng kho trong scope được Business cho phép.
- Form sản phẩm giới hạn ngưỡng dương và mặc định tối thiểu 1/2; Business vẫn xác minh quan hệ MaximumStock > ReorderPoint khi Save.
- ADMIN có Sửa thông tin/role/trạng thái và Đặt lại mật khẩu riêng qua UserService hiện có. Sửa thông tin giữ nguyên mật khẩu; reset chỉ gọi ResetPasswordAsync, không gộp hai writes.
- Sửa thông báo DbUpdateException: không hướng dẫn restart để apply migration vì ứng dụng không auto-migrate.

Việc còn lại theo độ khó:

| Thứ tự | Công việc | Điều kiện còn thiếu |
|---|---|---|
| U1 | Runtime Save/Cancel/error/reload các form vừa sửa; mọi role, keyboard/DPI | Phiên test và dữ liệu fixture được xác định; smoke control không thay GUI acceptance |
| U2 | Bộ lọc category/product và resolved-alert history/actor | Chốt query/DTO và hành vi filter nếu chưa có |
| U3 | UI quản trị assignment/context switch; reason/error handling | B2/B3/B5 |
| U4 | Reports paging/export đúng filter và in phiếu đầy đủ | B7; cần kiểm tra file Excel/PDF sau export |
| U5 | Dashboard v1: daily import/export quantity, lowest-stock horizontal bars, bốn trạng thái, recent movement | Chốt date/range/topN/ties/source; không tự chọn công thức |
| U6 | Notification/log UI + consumers | B4/B6 |
| U7 | Analytics nâng cao/BONUS, docs/demo/login options | Formula/policy approved; làm sau CORE |

## 4. Kiểm tra và giới hạn

- Solution build: 0 warnings, 0 errors.
- Business tests: **136 passed, 0 failed, 0 skipped**.
- EF migrations has-pending-model-changes: không có thay đổi chưa phản ánh trong snapshot.
- EF script từ InitialCreate đến RequirePositiveReorderPoint: sinh thành công, có precheck/transaction/CHECK/history.
- WinForms smoke local: 10 nhãn trạng thái không trả unknown; manager control là ComboBox; form user khởi tạo thành công. Không mở cửa sổ hiển thị, không ghi DB.
- Chưa apply migration, chưa SQL integration races/rollback, chưa GUI CRUD/reset/role acceptance hoặc DPI/export.
- SQL cũ giữ nguyên; không thêm package sản phẩm. EF CLI 10.0.12 dùng local trong `.agent` để kiểm tra, cùng phiên bản project.
- Chưa push đợt này. Shared instruction/README/checklist có thay đổi tài liệu từ trước; launchSettings local không đưa vào ba commit code.

## 5. Câu hỏi cần chủ dự án quyết định

1. Migration triển khai lên server/database nào? Nếu có sản phẩm ReorderPoint=0, giá trị thay thế phải theo từng sản phẩm hay do admin sửa trên UI trước apply?
2. DB kiểm thử riêng tên gì, connection nào và có cho phép reset DB test đó để chạy lại fixture không?
3. Đã chốt ngừng hoạt động assignment và triển khai. Còn cần chốt có tự thay trạng thái assignment khi đổi role/kho/user inactive không; hiện giữ rows và auth từ chối quyền không hợp lệ.
4. Audit cần giữ lịch sử mọi lần reject/resubmit/cancel hay chỉ lý do hiện hành? Chốt các fields completion/cancel/reject và adjustment metadata tối thiểu.
5. Chuyển kho dùng reference chung cấp từ đâu, và cách đảm bảo mã không trùng? Không tự thêm TransferReceipt.
6. Notification bắt buộc lưu cùng nghiệp vụ nào, gửi cho roles/kho nào, hay best-effort? Cách xử lý subscriber thất bại sau commit?
7. Đã chốt và triển khai tổng/export theo toàn bộ dữ liệu khớp bộ lọc; không giới hạn trang/cap5000. Còn kiểm tra file export thực tế và hiệu năng dataset lớn.
8. Dashboard dùng date nào, khoảng thời gian mặc định, top-N và cách xử lý đồng hạng? Valuation/advanced analytics có công thức approved nào?
