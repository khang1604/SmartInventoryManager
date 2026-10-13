# SmartInventoryManager — Database Rebuild Checklist

Phạm vi checklist: tái tạo schema SQL Server bằng EF Core Migration từ model hiện hành gồm 17 entity. Migration chỉ tạo cấu trúc; không chạy seed, không áp dụng lên SQL Server, và không backfill dữ liệu.

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

- [ ] Xác nhận database đích rỗng và chưa có migration history. Người dùng cho biết database đã xóa; truy vấn trực tiếp chưa xác minh được do quyền/kết nối SQL Server.
- [x] Sinh migration baseline từ model hiện tại; chưa chạy `database update`.
- [ ] Chọn thống nhất server/instance cho ứng dụng và EF design-time. Hiện `InventoryDbContextFactory`/`App.config` dùng `.\SQLEXPRESS`, còn Visual Studio launch profile dùng `localhost`.
- [ ] Sau khi có quyền kết nối và xác định đúng database sạch, chạy `dotnet ef database update` trên môi trường đích đã xác nhận.
- [ ] Đọc lại `__EFMigrationsHistory`, danh sách bảng, FK, index và check constraint sau khi apply.
- [ ] Chỉ commit migration sau khi kiểm tra diff và hoàn tất review.

## 3. Seed và phân công kho

- [x] Không có nguồn phân công STAFF–warehouse trong các entity, migration cũ hoặc seed SQL đã kiểm tra.
- [x] `Warehouse.ManagerId` chỉ biểu diễn manager của kho; không dùng làm phân công STAFF.
- [x] Receipt/transaction lịch sử không đủ căn cứ để suy ra phân công hiện tại.
- [ ] Có danh sách phân công do chủ nghiệp vụ/quản trị dữ liệu phê duyệt, với `UserId` và `WarehouseId` hợp lệ, trước khi nhập bất kỳ assignment nào.
- [ ] Xác thực tài khoản STAFF và kho còn hoạt động; không tạo assignment cho ADMIN/WAREHOUSE_MANAGER theo suy luận.
- [ ] Thiết kế seed riêng, an toàn và xác định; không đưa dữ liệu assignment giả vào migration.
- [ ] `08_SeedData.sql` hiện là skeleton: dừng nếu password hash còn placeholder và không cho chạy lại trên DB đã có dữ liệu. Không chạy file này như migration.

## 4. Audit còn thiếu cần bàn giao

- [ ] Receipt chưa có các cột riêng cho actor/time của submit, approve, reject, complete và cancel; `Note` không thay thế rõ nghĩa các audit này.
- [ ] Hợp đồng yêu cầu lý do reject/cancel, nhưng entity chưa có trường riêng. Business hiện ghi lý do reject vào `Note`; hàm cancel không nhận lý do.
- [ ] `InventoryTransaction` có actor/time, quantity, `ReferenceType` và `ReferenceId`; `TransferAsync` hiện chưa gán một shared `ReferenceId` cho cặp transfer.
- [ ] Trước khi thêm cột audit vào baseline, chốt schema với Data + Business owners để các service thực sự ghi những giá trị đó trong cùng transaction.
- [ ] Không thêm assignment audit fields hoặc lifecycle fields cho `UserWarehouse` khi chưa có phê duyệt rõ.

## 5. SQL-managed files

- [x] EF Migration là nguồn duy nhất cho bảng, cột, PK/FK, check, default, unique constraint và index.
- [x] `03_CreateConstraints.sql`/`04_CreateIndexes.sql` tự ghi là legacy reference, không thực thi; không dùng thay migration.
- [x] `05_CreateViews.sql`/`06_CreateStoredProcedures.sql` tự ghi chưa triển khai; `07_CreateTriggers.sql` ghi không dùng.
- [ ] Nếu cần views/SP/trigger về sau, triển khai riêng sau migration và kiểm tra tương thích với model; trigger không được tự post stock.
- [ ] Kiểm tra lại `11_SeedAdminAccount.sql` trên môi trường dùng thật trước khi chạy vì file cập nhật thông tin/hash admin khi user đã tồn tại.

## 6. Apply và xác minh môi trường

- [ ] Cấp quyền chỉ đọc schema/history trước khi đối chiếu bất kỳ database hiện hữu nào.
- [ ] Dùng một server/instance đã xác nhận; không suy luận DB không tồn tại chỉ từ việc tài khoản không nhìn thấy nó.
- [ ] Tạo/cập nhật database chỉ sau khi xác nhận rõ target. Không apply migration vào database production hoặc DB không được chỉ định.
- [ ] Kiểm tra 17 bảng, relationship, cascade/restrict, unique/check constraints, filtered index và migration history.
- [ ] Hoàn thiện seed được phê duyệt và kiểm tra tạo tài khoản STAFF + gán kho qua `UserWarehouse`.
- [ ] Bàn giao các thay đổi Business cần thiết cho Khang và luồng phân công kho UI cho Khiêm.
