# Database — trạng thái hiện hành 14/10/2026

EF Core Migrations là nguồn schema. Trên TEST `localhost/SmartInventoryManagerDB` đã có InitialCreate, RequirePositiveReorderPoint và AddUserWarehouseActiveState; pending migration rỗng, ReorderPoint<=0 bằng0. UserWarehouse có IsActive, composite PK và Restrict FKs. Không truy cập production, không xóa SQL cũ. App không auto-migrate/auto-seed. Bootstrap ADMIN tương tác riêng ở SmartInventoryManager.Setup; --check chỉ đọc, không ghi đè ADMIN active.

Ba SQL integration tests đã PASS; chi tiết phạm vi và giới hạn tại [CompletionByLayer_2026-10-14.md](CompletionByLayer_2026-10-14.md). Audit receipt/adjustment/shared transfer reference và valuation/analytics được người dùng bỏ qua để bàn team. Checklist bên dưới đã cập nhật theo bằng chứng code và kiểm thử đến ngày 14/10/2026; dấu hoàn thành trên môi trường TEST không có nghĩa đã triển khai production.

## Checklist hiện hành

# SmartInventoryManager — Database Rebuild Checklist

Phạm vi checklist: quản lý schema SQL Server bằng EF Core Migration từ model hiện hành gồm 17 entity. Migration tạo/cập nhật cấu trúc; seed là thao tác riêng. Đã apply các migration trên DB TEST được người dùng xác nhận; production chưa được cung cấp.

## 1. Audit model trước baseline

- [x] 17 entity có trong `InventoryDbContext`: `Role`, `User`, `Category`, `Product`, `Supplier`, `Warehouse`, `Location`, `Inventory`, `ImportReceipt`, `ImportDetail`, `ExportReceipt`, `ExportDetail`, `InventoryTransaction`, `StockAlert`, `Notification`, `SystemLog`, `UserWarehouse`.
- [x] Baseline migration có đủ 17 bảng SQL:
  - [x] `Roles`
  - [x] `Users`
  - [x] `Categories`
  - [x] `Products`
  - [x] `Suppliers`
  - [x] `Warehouses`
  - [x] `Locations`
  - [x] `Inventory`
  - [x] `ImportReceipts`
  - [x] `ImportDetails`
  - [x] `ExportReceipts`
  - [x] `ExportDetails`
  - [x] `InventoryTransactions`
  - [x] `StockAlerts`
  - [x] `Notifications`
  - [x] `SystemLogs`
  - [x] `UserWarehouses`
- [x] Giữ đúng 3 file trong `SmartInventoryManager.Data/Configurations`: `ProductConfiguration.cs`, `InventoryConfiguration.cs`, `ImportConfiguration.cs`.
- [x] `UserWarehouse` dùng composite PK `(UserId, WarehouseId)`; hai FK dùng `DeleteBehavior.Restrict`.
- [x] Không có `Users.WarehouseId`, `InventoryLocation`, `MinimumStock`, `StockAlertHistory`, `TransferReceipt`, `Customer`, hoặc `Outbox`.
- [x] Inventory có unique key logic `(ProductId, WarehouseId)` và check quantity không âm.
- [x] Import/Export dùng sáu trạng thái chính thức, mặc định `DRAFT`; quantity chi tiết dương; đơn giá và tổng tiền không âm.
- [x] Stock alert có filtered unique index `(ProductId, WarehouseId, AlertType) WHERE IsResolved = 0`.
- [x] Đã scaffold `20261012084010_InitialCreate`; SQL sinh ra có đúng 17 `CREATE TABLE`, gồm `UserWarehouses`, và filtered unique index cho open alerts.
- [x] Đã rà migration: FK dùng `NO ACTION` (EF `Restrict`); migration `Up` tạo schema từ đầu, không sửa/xóa dữ liệu cũ.
- [x] Sinh SQL migration script thành công để review mà không kết nối/apply lên SQL Server.

## 2. Migration baseline

- [x] Xác định trạng thái DB TEST trước apply: database hiện hữu có InitialCreate; không xem đây là DB rỗng hoặc chạy lại baseline để tạo chồng schema.
- [x] Sinh migration baseline từ model; DB TEST đã có `20261012084010_InitialCreate`.
- [x] Thống nhất target TEST `localhost / SmartInventoryManagerDB` cho ứng dụng và EF design-time; factory đọc `SMART_INVENTORY_CONNECTION_STRING`, fallback localhost thay SQLEXPRESS.
- [x] Apply `20261014010000_RequirePositiveReorderPoint` và `20261014090948_AddUserWarehouseActiveState` trên DB TEST đã xác nhận; không còn pending migration.
- [x] Đọc migration history, kiểm tra model/snapshot và truy vấn đủ 17 entity sau apply; kiểm tra CHECK ngưỡng dương và trạng thái assignment.
- [ ] Hoàn tất đối chiếu toàn bộ FK/index/CHECK thực tế với model; không dùng việc query 17 bảng thành công để thay kiểm tra đầy đủ schema.
- [x] Rà diff và commit migration: `7282f26` (ngưỡng dương), `455f838` (assignment IsActive).
- [ ] Peer review/duyệt thay đổi schema bởi thành viên khác trước khi merge develop.

## 3. Seed và phân công kho

- [x] Phân công STAFF dùng entity `UserWarehouse` và service ADMIN riêng; không suy phân công từ dữ liệu cũ hoặc lịch sử phiếu.
- [x] `Warehouse.ManagerId` chỉ biểu diễn manager của kho; không dùng làm phân công STAFF.
- [x] Receipt/transaction lịch sử không đủ căn cứ để suy ra phân công hiện tại.
- [x] Fixture STAFF/hai kho TEST được người dùng cho phép; gán qua `UserWarehouse`, nghiệm thu rồi dọn đúng fixture.
- [ ] Nhận danh sách phân công production được chủ nghiệp vụ phê duyệt với `UserId`/`WarehouseId` hợp lệ.
- [x] `WarehouseAssignmentService` chỉ kích hoạt assignment cho STAFF active và kho active; ADMIN/WAREHOUSE_MANAGER không được cấp assignment STAFF.
- [x] Seed/bootstrap tách khỏi migration; không đưa assignment giả vào migration. Bootstrap tương tác chỉ tạo 3 role và ADMIN đầu tiên, lưu PBKDF2, không ghi đè ADMIN active.
- [x] Đã kiểm tra guard `08_SeedData.sql`: dừng khi hash còn placeholder hoặc DB đã có dữ liệu; giữ file cũ, không chạy như migration.
- [ ] Nghiệm thu bootstrap tương tác trên SQL DB trắng và chuẩn bị seed production được duyệt; unit tests và `--check` không thay bước nghiệm thu này.

## 4. Audit còn thiếu cần bàn giao

- [ ] Receipt chưa có các cột riêng cho actor/time của submit, approve, reject, complete và cancel; `Note` không thay thế rõ nghĩa các audit này.
- [ ] Hợp đồng yêu cầu lý do reject/cancel, nhưng entity chưa có trường riêng. Business hiện ghi lý do reject vào `Note`; hàm cancel không nhận lý do.
- [ ] `InventoryTransaction` có actor/time, quantity, `ReferenceType` và `ReferenceId`; `TransferAsync` hiện chưa gán một shared `ReferenceId` cho cặp transfer.
- [ ] Chốt schema audit với Data + Business owners; khi được duyệt thêm migration mới, không sửa baseline đã apply. Service phải ghi audit trong cùng transaction.
- [x] Người dùng duyệt lifecycle assignment giữ row + IsActive; đã triển khai migration, auth active-only và UI ngừng/kích hoạt lại. Chưa thêm assignment audit fields ngoài phê duyệt.

## 5. SQL-managed files

- [x] EF Migration là nguồn duy nhất cho bảng, cột, PK/FK, check, default, unique constraint và index.
- [x] `03_CreateConstraints.sql`/`04_CreateIndexes.sql` tự ghi là legacy reference, không thực thi; không dùng thay migration.
- [x] `05_CreateViews.sql`/`06_CreateStoredProcedures.sql` tự ghi chưa triển khai; `07_CreateTriggers.sql` ghi không dùng.
- [ ] Nếu cần views/SP/trigger về sau, triển khai riêng sau migration và kiểm tra tương thích với model; trigger không được tự post stock.
- [ ] Kiểm tra lại `11_SeedAdminAccount.sql` trên môi trường dùng thật trước khi chạy vì file cập nhật thông tin/hash admin khi user đã tồn tại.

## 6. Apply và xác minh môi trường

- [x] Kết nối và đọc history/schema của DB TEST được xác nhận; không cần cấp thêm quyền để thực hiện các probe đã chạy.
- [x] Dùng đúng target `localhost / SmartInventoryManagerDB`, được người dùng xác nhận là TEST; server chính nằm riêng.
- [x] Chỉ apply migration lên target TEST đã được cho phép; không truy cập hoặc thay đổi production.
- [x] Query 17 entity, migration history và pending model/migrations; SQL tests posting/duplicate completion/rollback/deadlock/Dashboard PASS.
- [ ] Đối chiếu toàn bộ relationship/cascade/restrict/unique/CHECK/filtered index thực tế; xem mục 2 cho phần đã kiểm tra.
- [x] Tạo STAFF TEST và gán hai kho qua service; UI xác minh ngừng/kích hoạt lại, đăng nhập, đổi kho A tồn0/B tồn5; cleanup theo yêu cầu.
- [x] Business và UI phân kho đã triển khai, commit và push Backend; tài liệu bàn giao tại CompletionByLayer_2026-10-14.md.
- [ ] Nhận server/database production, người phụ trách migration/backup/recovery và nghiệm thu triển khai riêng.

## Cập nhật contract 14/10/2026

- [x] Mapping, validation và migration tuân thủ `0 < ReorderPoint < MaximumStock`; `RequirePositiveReorderPoint` đã apply TEST sau precheck có 0 sản phẩm ReorderPoint<=0. Không sửa baseline đã apply hoặc tự chọn giá trị thay thế.
- [x] `UserWarehouse` giữ composite PK/hai FK Restrict; thêm IsActive bằng migration riêng được duyệt, không tạo lại entity.
- [ ] Audit phiếu, metadata điều chỉnh và shared reference chuyển kho: người dùng chọn bỏ qua để bàn team; chưa thêm schema cho các mục này.
