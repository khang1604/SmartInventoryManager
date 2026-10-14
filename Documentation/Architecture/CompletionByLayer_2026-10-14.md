# Tiến độ hoàn thiện theo ba tầng — 14/10/2026

Chuẩn: `.agent/SystemContract.md` mới, được người dùng chọn ngày 14/10/2026. Người dùng hiện đảm nhận toàn bộ ba tầng và cho phép sửa liên tầng, commit riêng từng tầng. Triển khai từ dễ đến khó; quyết định nghiệp vụ chưa đủ bằng chứng được để mở. SQL cũ trong `Database/` giữ nguyên. Báo cáo này ghi một đợt triển khai, không chứng nhận toàn bộ dự án đã hoàn thiện.

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
3. Khi thu hồi phân kho STAFF: xóa UserWarehouse hay cần trạng thái deactivate? Khi đổi role/kho inactive, assignments được giữ hay xóa?
4. Audit cần giữ lịch sử mọi lần reject/resubmit/cancel hay chỉ lý do hiện hành? Chốt các fields completion/cancel/reject và adjustment metadata tối thiểu.
5. Chuyển kho dùng reference chung cấp từ đâu, và cách đảm bảo mã không trùng? Không tự thêm TransferReceipt.
6. Notification bắt buộc lưu cùng nghiệp vụ nào, gửi cho roles/kho nào, hay best-effort? Cách xử lý subscriber thất bại sau commit?
7. Report tổng theo full scope hay theo search/type đang xem; export toàn dữ liệu filter hay trang hiện tại?
8. Dashboard dùng date nào, khoảng thời gian mặc định, top-N và cách xử lý đồng hạng? Valuation/advanced analytics có công thức approved nào?
