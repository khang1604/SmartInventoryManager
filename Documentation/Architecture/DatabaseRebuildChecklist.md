# Database Rebuild Checklist — hiện hành 14/10/2026

EF Core Migrations là nguồn schema duy nhất. TEST được phép: localhost/SmartInventoryManagerDB; production riêng chưa được cung cấp. Checklist này cập nhật theo code và kiểm tra SQL thật, thay trạng thái pending trước quyết định TeamChot. SQL legacy được giữ, không chạy chồng lên migration. Chi tiết tại [báo cáo hiện hành](ProjectStatus.md) và [kiến trúc](CurrentArchitecture.md).

## 1. Model và baseline

- [x] Baseline InitialCreate tạo17bảng; không sửa migration đã apply.
- [x] Model hiện hành19entity/bảng nghiệp vụ sau khi thêm ReceiptRejection và ReceiptStatusChange bằng migration mới.
- [x] Giữ đúng3configuration: ProductConfiguration, InventoryConfiguration, ImportConfiguration.
- [x] UserWarehouse composite PK(UserId,WarehouseId), Restrict FKs, IsActive; STAFF active/currentwarehouse/assignment active là căn cứ quyền.
- [x] Inventory unique ProductId/WarehouseId, quantity>=0; product0<ReorderPoint<MaximumStock.
- [x] Sáu trạng thái phiếu; details quantity>0, prices/totals>=0; không thêm role mới.
- [x] Không thêm InventoryLocation, MinimumStock, TransferReceipt, Customer hoặc Outbox.
- [x] Open alerts filtered unique ProductId/WarehouseId/AlertType WHERE IsResolved=0; index trùng baseline được bỏ bằng migration riêng.

## 2. Migrations đã áp dụng trên TEST

- [x] 20261012084010_InitialCreate.
- [x] 20261014010000_RequirePositiveReorderPoint; precheck không tự chọn giá trị thay dữ liệu sai.
- [x] 20261014090948_AddUserWarehouseActiveState.
- [x] 20261014135633_AddReceiptAuditAndMovementMetadata: cột audit nullable, ReceiptRejections, movement metadata, transfer sequence/uniqueness.
- [x] 20261014151012_RemoveRedundantStockAlertIndex; giữ UX_StockAlerts_Open_ProductWarehouseType đúng model.
- [x] Đối chiếu đủ18bảng thực tế: cột/type/length/precision/nullability, PK, FK mapping và delete behaviors, index cột/unique/filter, CHECK expression/trust với model.
- [x] Migration history/pending/model consistency đạt; không còn pending trên TEST.
- [x] Data commits e756339 và4513bf8, ngoài các commit baseline/ngưỡng/assignment trước đó.
- [ ] Peer review/duyệt schema và merge develop bởi team.

## 3. Audit và workflows

- [x] Import/Export CompletedBy+CompletedAt ghi cùng transaction Complete; legacy unknown giữNULL.
- [x] CancelReason/RejectReason riêng, bắt buộc khi thực hiện; không ghi đè Note.
- [x] ReceiptRejections actor/time/reason mỗi lần, CHECK đúng một loại phiếu; history20/trang, không detail snapshots/purge.
- [x] Sửa/resubmit giữ nearestRejectReason và toàn bộ history; không hard-delete draft có rejectionhistory.
- [x] Movement BeforeQuantity/AfterQuantity/SignedDifference; signed=after−before; legacy cả3NULL, dữ liệu mới nhất quán với stock.
- [x] Transfer ReferenceId chung SQL sequence;2TransactionId và2type riêng; atomic, uniqueness mỗi phía; khôngMAX+1.
- [x] SQL kiểm tra duplicatecompletion, thiếu tồn, chuyển kho đồng thời, xuất cạnh tranh, Complete/Cancelrace, deadlock1205 và rollback khi ghi log lỗi:4tests PASS.
- [x] Business stock/log cùngtransaction, publisher/notification saucommit best-effort, không replaystock khi gửi lỗi.
- [x] Người dùng chốt15/10: latest SubmittedBy/At, ApprovedBy/At, CancelledBy/At và full transition history, không snapshot details; model/service/UI/PDF đã nối. 20261015004156_AddReceiptLifecycleAudit đã apply TEST; schema19bảng/model consistency đã đối chiếu15/10.
- [ ] Retention/archive và bảo đảm replay delivery sau commit nếu cần: chưa có policy/Outbox được duyệt.

## 4. Bootstrap, seed và fixture

- [x] App startup không tự migrate/seed; Setup tương tác riêng dùng environmentconnection, tạo3roles và firstactiveADMIN/PBKDF2; không ghi đè ADMIN active.
- [x] Nghiệm thu actual Setup trên SQL trắng, actualAuthService login, kiểm tra guard ADMIN active.
- [x] DB tạm localhost/SmartInventoryManagerBootstrapTest_20261014 được phép tạo/dọn; đã drop đúng target. DB TEST chính khôngdrop/reset.
- [x] STAFF/hai kho fixture của vòng trước và UI audit fixture vòng này được dọn đúng scope sau kiểm tra; không đổi tài khoản demo.
- [x] SQL integration fixtures tự dọn, opt-in đúng target; không tính skipped/inconclusive làPASS.
- [x] Seed tách migration;08_SeedData.sql giữguardplaceholder/DBcódữliệu; SQL cũ giữ nguyên.
- [ ] Danh sách assignment/seed production được chủ nghiệp vụ phê duyệt.
- [ ] Trước chạy11_SeedAdminAccount.sql trên môi trường thật phải review tác dụng cập nhật admin đang có; ưu tiên Setup cho DBtrắng.

## 5. Analytics và schema authority

- [x] Giai đoạn đầu dùng giá trịước tínhQty×PurchasePrice; ABCcompletedexportdetail12tháng, deadstock90ngày/earliestmovement, suggestions Max−Qty theo quyết định đã duyệt, giữ warehouseauthorization.
- [ ] Bình quân gia quyền: chốt tồn đầu kỳ, chi phí transfer và adjustmentvaluation trước giai đoạn2.
- [x] SQL03/04 làlegacyreference;05/06 chưa triển khai,07 không dùng; không dùng SQL legacy thay migration hoặc trigger tựpoststock.
- [ ] Views/SP/trigger chỉ triển khai nếu có nhu cầu/hợp đồng riêng được duyệt.

## 6. Triển khai và nghiệm thu còn lại

- [x] TEST connection, schema/model,152unit/4SQL PASS, bootstrap trắng và exportQA có bằng chứng trong báo cáo.
- [x] Pkcs9.0.21 Data được duyệt/pin; vulnerability audit Data không báo vulnerablepackages.
- [ ] Production server/database, migrationowner, backup/restore và nghiệm thu triển khai riêng.
- [ ] Designer/DPI/HighContrast, các kích thước màn hình khác, SaveFileDialog/máy in thật và datasetlớn; native checks hiện tại không thay đầy đủ các bước này.

Dấu[x] chỉ xác nhận phạm vi đã kiểm tra; không có nghĩa hoàn thiện toàn bộ contract hoặc triển khai production. Không rollback audit bằngDown khi chưa backup/đánh giá mất dữ liệu history.

Lưu ý: kết quả18bảng/152unit/4SQL ở trên là snapshot14/10; không dùng để chứng minh migration lifecycle mới hoặc các bài SQL/UI chưa chạy lại. Xem ProjectStatus.md cho trạng thái mới.

- [x] 15/10: latestactor/time + ReceiptStatusChanges apply thành công,6migrations khôngpending;157unit PASS. SQLmutation/UIacceptance của lifecycle mới chưa chạy lại.
