# Hoàn thiện theo quyết định team — 14/10/2026

Trạng thái này thay thế các mục trì hoãn đã được chốt trong báo cáo trước. Chủ dự án cho phép triển khai cả ba tầng theo TeamChot, giữ SQL cũ và commit từng tầng. TEST là `localhost/SmartInventoryManagerDB`; production riêng chưa được cung cấp. `.agent` là tài liệu local, bản quyết định cần cho người clone được ghi lại ở đây và trong quy tắc dùng chung.

## Kết quả theo ba tầng

| Tầng | Đã triển khai | Bằng chứng |
|---|---|---|
| Data | CompletedBy/At, CancelReason/RejectReason cho phiếu nhập/xuất; ReceiptRejections với CHECK đúng một phiếu; movement Before/After/SignedDifference; sequence chuyển kho và uniqueness từng phía | `e756339`; migration mới, không sửa baseline; dữ liệu audit cũ giữ NULL |
| Data | Bỏ index open-alert trùng từ baseline, giữ index đúng model | `4513bf8`; đối chiếu SQL thật 18 bảng, cột/type/length/precision/nullability, PK/FK/delete behavior/index/filter/CHECK trust và model consistency đạt |
| Business | Audit cùng transaction; giữ lý do từ chối gần nhất qua sửa/gửi lại; toàn bộ lịch sử phân trang20; không xóa nháp có history; cancel bắt buộc lý do | `3ae79fa`; unit và SQL tests |
| Business | Transfer có movement riêng và ReferenceId chung từ sequence; điều chỉnh có before/after/difference; Complete audit cùng stock/log | `3ae79fa`; transfer concurrent, competing exports, complete/cancel race, injected log failure rollback trên SQL |
| Business | Analytics giai đoạn đầu và giá trị ước tính Dashboard/report; lịch sử cảnh báo đóng có người/thời gian; log thao tác tài khoản không chứa mật khẩu/hash | `3ae79fa`, `78e9755`; kiểm tra authorization và audit bằng unit tests |
| UI | Phân tích bốn tab, ngưỡng ABC, scope kho; Excel metadata; thông báo dữ liệu rỗng; đổi bộ lọc phải làm mới trước export | `86ee516`; Native ADMIN kiểm tra tab/empty ABC/filter invalidation/export guard; Excel QA |
| UI | Audit phiếu readonly/PDF, lịch sử từ chối20/trang; movement/reference/quantity audit; ẩn scrollbar, wheel, màu Chi tiết; danh mục sản phẩm và trạng thái cảnh báo | `86ee516`; Native UI và build; xem phạm vi dưới |

## Quy tắc nghiệp vụ đã xác nhận

- Giữ đúng ba role và sáu trạng thái phiếu. Resubmit đưa REJECTED về DRAFT; phải Submit lại trước duyệt. Chỉ Complete ảnh hưởng tồn một lần. Lịch sử từ chối gồm actor/time/reason, không snapshot detail, không tự purge/archive.
- SignedDifference = AfterQuantity − BeforeQuantity; reason/actor/time sử dụng metadata hiện có. Transfer có hai TransactionId riêng, chung ReferenceId từ SQL sequence; khoảng trống số do rollback được chấp nhận. Mã OUT/IN/MOV/TRF là cách trình bày từ ID, không tạo bảng TransferReceipt.
- Giá trị tồn giai đoạn đầu = current Quantity × current PurchasePrice, ghi rõ ước tính, không phải giá vốn kế toán.
- ABC dùng từng dòng xuất COMPLETED Quantity × UnitPrice, nhóm sản phẩm–kho trong 12 tháng đến hết hôm nay; giảm dần giá trị, đồng hạng theo mã sản phẩm/kho. Dòng vượt mốc thuộc nhóm đang tích lũy; mặc định A80/B95, UI cho đổi. Tổng0 không đủ giá trị phân nhóm.
- Luân chuyển xét 90 ngày gồm hôm nay, tồn dương, chỉ xuất COMPLETED là nhu cầu; earliest movement là mốc theo dõi. Không history hoặc chưa đủ90ngày là chưa đủ dữ liệu. Không suy từ ngày tạo sản phẩm.
- Đề xuất nhập khi Quantity<=ReorderPoint: MaximumStock−Quantity. Chỉ gợi ý, không tự tạo/hoàn tất phiếu. Giữ `0 < ReorderPoint < MaximumStock`.
- Log stock nghiệp vụ cùng transaction; notification/event sau commit, best-effort, không replay stock khi gửi lỗi. Retry chỉ SQL1205 rollback chắc chắn, tối đa hai lần; không retry unknown commit/timeout/lost connection.

## Kiểm thử đã thực hiện

| Kiểm tra | Kết quả / giới hạn |
|---|---|
| Solution build | PASS, 0 warning / 0 error |
| Unit suite | 152 PASS; SQL suite chạy riêng, không tính skipped là PASS |
| SQL suite | 4 PASS / 0 fail / 0 skip, fixture riêng và cleanup; concurrent transfer, competing exports, complete/cancel, rollback log failure, duplicate completion, SQL deadlock1205, Dashboard ADO.NET |
| Schema SQL | 18 bảng đối chiếu metadata thật và migration model đạt; 5 migrations đã apply TEST, không pending |
| Bootstrap SQL trắng | Actual Setup tương tác tạo3role/1ADMIN/PBKDF2, actual AuthService đăng nhập đạt, active ADMIN guard đạt; DB tạm được phép đã drop đúng target |
| Export | 3 PDF và3 Excel sinh thành công; dữ liệu stock/report/analytics và ABC thresholds đúng. PDF receipt60dòng/A4/5trang, đã xem trang đầu/cuối với audit, tiếng Việt và tổng tiền |
| Dependency | Pkcs9.0.21 được người dùng duyệt và pin tại Data; vulnerability audit Data không phát hiện package vulnerable. Harness local từng có NU1900 do không truy cập NuGet, không tính nó là vulnerability scan thành công |
| Native UI | ADMIN Dashboard/analytics/value/deadstock/reorder/ABC rỗng; category filter Office Supplies trả1sản phẩm; closed-alert empty và không cho đóng lại; audit Complete/Cancel giữ Note; history23dòng phân trang20+3; scrollbar menu/content/grid ẩn, wheel vẫn hoạt động; Chi tiết giữ nền hàng chọn; đổi ABC threshold chặn export dữ liệu cũ |

SQL test và UI audit fixtures đã được dọn đúng phạm vi sở hữu; không đổi tài khoản demo, không reset/drop DB TEST. DB bootstrap tạm `localhost/SmartInventoryManagerBootstrapTest_20261014` đã dọn. Source/harness local giữ lại để truy vết, không commit `.agent` hoặc local launchSettings.

## Những việc còn cần nghiệm thu hoặc quyết định

| Việc | Người/phạm vi xử lý |
|---|---|
| Bình quân gia quyền: tồn đầu kỳ, chi phí chuyển kho, định giá điều chỉnh | Chủ nghiệp vụ/team chốt trước giai đoạn2; chưa triển khai |
| Retention/archive lịch sử và yêu cầu bảo đảm giao event sau commit | Team quyết định nếu cần; hiện giữ toàn bộ history, notification best-effort, không Outbox |
| Production server/database, backup/restore, assignment và seed được duyệt | Admin; chưa có môi trường, không tự áp dụng TEST sang production |
| Peer review schema/migration và merge develop | Team review; chưa thay bằng việc agent test |
| Designer và các mức DPI/High Contrast, kích thước màn hình khác, máy in thật và SaveFileDialog | Nghiệm thu trực tiếp Windows/Visual Studio; native checks hiện ở1322×812 không xác nhận toàn bộ các tổ hợp |
| Dữ liệu rất lớn / export hiệu năng / snapshot consistency đa-query | Đo với dataset mục tiêu; chưa coi vài fixture là chứng minh mọi tải |

Không có thay đổi tự đoán cho các mục đang mở. Không rollback migration audit bằng Down trên DB đã có history nếu chưa backup và đánh giá mất dữ liệu; rollback index riêng là thay đổi schema không xóa business records. Xem [kiến trúc hiện hành](CurrentArchitecture.md) và [checklist DB](DatabaseRebuildChecklist.md).

Trạng thái Git: các tầng đã commit riêng trên Backend; chưa push đợt này. Theo chỉ đạo cuối, không dọn thêm dữ liệu; cleanup fixture nêu trên đã hoàn tất trước chỉ đạo đó.
