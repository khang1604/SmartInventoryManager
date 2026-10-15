# Báo cáo tổng hợp SmartInventoryManager

Cập nhật 15/10/2026. Đây là báo cáo tiến độ/nghiệm thu hiện hành duy nhất, hợp nhất báo cáo UI13/10, các bản tổng hợp và hai báo cáo hoàn thiện14/10. Quy tắc/contract/quyết định nghiệp vụ và checklist DB là tài liệu chuyên biệt, không phải các báo cáo tiến độ cạnh tranh. Dữ liệu lịch sử chưa được kiểm tra lại không tự trở thành kết quả hiện tại.

## 1. Phạm vi và trạng thái

- Chủ dự án cho phép hoàn thiện cả ba tầng, commit riêng từng tầng; SQL legacy giữ nguyên. Branch Backend, chưa push đợt này.
- TEST: localhost/SmartInventoryManagerDB. Production riêng chưa được cung cấp; không drop/reset DB TEST hoặc thay tài khoản demo.
- Quyết định hiện hành: SystemContract cùng các quyết định TeamChot mới hơn; không dùng snapshot SC_3/contract cũ để ghi đè lựa chọn mới. `.agent` local-only; quy tắc chia sẻ ở .github/copilot-instructions.md.
- Đợt15/10 xử lý nghiệp vụ trước; build/unit/schema được kiểm tra cuối đợt. SQL mutation/native UI/export của lifecycle mới chưa nghiệm thu lại. Không dọn thêm dữ liệu DB.

## 2. Đã triển khai theo ba tầng

| Tầng | Công việc | Commit chính |
|---|---|---|
| Data | Ngưỡng0<ReorderPoint<MaximumStock, UserWarehouse.IsActive/compositePK/Restrict; migrations là schema authority | 7282f26,455f838 |
| Data | Completion/cancel/reject audit; ReceiptRejections; movement before/after/difference; transfer sequence/uniqueness; bỏ open-alert index trùng | e756339,4513bf8 |
| Data15/10 | SubmittedBy/At, ApprovedBy/At, CancelledBy/At; ReceiptStatusChanges và CHECK/FK/index. Model hiện19entity, giữ3fileconfiguration | 4a164ca |
| Business | Authoritative inventory posting, atomic stock/log, bounded SQL1205 retry, best-effort events/notification; full-filter reports; Dashboard theo quy tắc đã chốt | 28df10a và các commit trong lịch sử Git |
| Business | Audit/rejections, transferreference, valuation estimate/ABC/deadstock/reorder; resolvedalerts actor/time; accountoperation logs không chứa credentials | 3ae79fa,78e9755 |
| Business15/10 | Ghi mọi chuyển trạng thái thực tế cùng transaction, bảo vệ history, latest actor/time, paged authorizedhistory | 7031268 |
| UI | Dashboard/report/analytics, PDFphiếu, user/assignment/switchwarehouse, movementmetadata, scrollbar ẩn/Chi tiết giữ màu, categoryfilter/closedalerts | d904a20,86ee516 |
| UI15/10 | Latest audit trên form/PDF và Lịch sử xử lý20/trang; giữ đường xem lịch sử từ chối | 686ca99 |
| Setup | Bootstrap3role/firstactiveADMIN/PBKDF2 riêng, không auto-seed khi mởapp/không ghi đèADMIN active | 381ba7a,8941447 |

## 3. Các quyết định nghiệp vụ đã chốt

| Chủ đề | Quy tắc hiện hành |
|---|---|
| Quyền | Đúng ADMIN,WAREHOUSE_MANAGER,STAFF. STAFF dựa activeassignment/currentwarehouse; manager dựa Warehouse.ManagerId; UI không thay authorization Business |
| Stock | Per product–warehouse; quantity không âm. Chỉ Complete post phiếu một lần. Transfer hai phía cùng transaction; adjustment có reason/actor/time/before/after/difference |
| Trạng thái | DRAFT,PENDING_APPROVAL,APPROVED,REJECTED,COMPLETED,CANCELLED; Resubmit hoặc sửa REJECTED đưa vềDRAFT rồi Submit lại |
| Lifecycle15/10 | Mỗi chuyển trạng thái có from/to/actor/time/reason nếu có; Submit/Approve/Cancel giữ mốc gần nhất. Initialdraft dùng creator/date hiện có, không bịa chuyển trạng thái trướcDRAFT |
| History | Giữ ReceiptRejections và ReceiptStatusChanges; không snapshot details, không tựpurge/archive. Có history thì không hard-delete; draft chưa xử lý vẫn xóa theo quyền |
| Legacy | Không tự backfill actor/time/history chưa biết. Giá trị nullable hiển thị chưa có dữ liệu |
| Transfer | TransactionId riêng, ReferenceId chung do SQLsequence cấp; gap do rollback chấp nhận, khôngMAX+1. UI OUT/IN/MOV/TRF từ IDhiện có |
| Retry/events | SQL1205 chỉ retry tối đa2lần khi rollback chắc chắn; không retrytimeout/lostconnection/unknowncommit. Log nghiệp vụstock cùngtransaction; publisher/notification saucommit best-effort, không replaystock |
| Notification15/10 | Người dùng chọn giữ best-effort, không thêm Outbox/hàng đợi bền vững trong đợt nghiệm thu này |
| Giá trị tồn | Ước tính=currentQty×currentPurchasePrice, không phải giá vốn kế toán. Người dùng15/10 xác nhận WAC để giai đoạn2 |
| ABC | CompletedexportdetailQty×UnitPrice, product–warehouse12tháng đến hết hôm nay; giảm dần value, tieProductCode/WarehouseCode; crossingrow ở nhóm đang tích lũy, defaultA80/B95 cấu hình; total0 không đủvalue |
| Luân chuyển | Tồn dương, khôngCompletedexport90ngày gồm hôm nay; firstmovement là trackingstart. Thiếuhistory/chưa đủ90ngày hiển thị chưa đủdữliệu; transfer/adjust không phải demand |
| Reorder | Qty<=RP: suggestion=Max−Qty, không tự tạo/postphiếu |
| Dashboard | CreatedAt, mặc định30ngày gồm hôm nay; top10productwarehouse theoqty/tiecodes;4stockstates;recent20. Report/export lấy toàn bộ authorizeddataset khớp filter |
| UI | Ẩn scrollbar chrome nhưng giữ truy cập wheel/keyboard/paging; Chi tiết giữ selectioncolor. Export sau đổifilter phải tải lại |

## 4. Kiểm tra: phân biệt đợt mới và bằng chứng lịch sử

| Kiểm tra | Bằng chứng mới nhất | Giới hạn |
|---|---|---|
| Buildsolution15/10 | PASS,0warning/0error | Không thay nativeUI/Designer |
| Unit15/10 | 157PASS,0fail/0skip; thêm5ca kiểm tra chain9transitions, latestactors/cancel, paging/scope/historydelete | EFInMemory không chứng minh SQL concurrency |
| Migration15/10 | 20261015004156_AddReceiptLifecycleAudit apply TEST thành công;6migrations, khôngpending/modeldiff | Chưa production |
| Schema15/10 | PASS19bảng: columns/type/length/precision/nullability,PK/FKmapping/deletebehavior,index/filter/unique,trustedCHECK và modelconsistency | Harness chuẩn hóa SQLServerIN→OR để so sánh; không sửa constraint để che lỗi |
| SQL14/10 | 4PASS/0fail/0skip: duplicatecompletion,transferconcurrent,competingexports,completecancelrace,logfailrollback,1205,DashboardADO.NET | Chưa chạy lại suite mutation sau lifecycle15/10; testsource đã cập nhật history assertions/fixtureFKorder |
| Bootstrap14/10 | ActualSQLtrắng interactiveSetup3role/firstADMIN/PBKDF2/AuthServicelogin/activeADMINguard đạt | DBtạm đúng target đã dọn trước yêu cầu hoãn dọn dữ liệu |
| Export14/10 | 3PDF/3Excel; numericdatasets vàABCthresholds đúng;receipt60dòng/A4/5trang đã xem đầu/cuối | PDF mới thêm latestaudit15/10 chưa render/nativeaccept lại |
| NativeUI14/10 | ADMINDashboard/analytics/emptyABC/categoryfilter/closedalert; completioncancelnote/history23=20+3; hiddenbars/wheel/selection; đổiABC chặnexportstale. STAFFA/B từ vòng trước đạt | Chưa nativeaccept lifecycle15/10; chưa fullDPI/Designer/HighContrast/máyin/SaveFileDialog/datasetlớn |
| Dependency14/10 | Pkcs9.0.21 pin được duyệt, Data vulnerabilityaudit không báo vulnerablepackages | NU1900 của harnesslocal khi không truy cậpNuGet không được tính làscanPASS |

SQL/UI fixture của14/10 và DBbootstrap tạm đã dọn trước khi người dùng yêu cầu hoãn. Đợt15/10 không dọn dữ liệuDB và không chạy suiteSQLmutation. Các file tạm/công cụ/harness được chuyển sang .temp, việc di chuyển file không phải cleanupDB.

## 5. Việc còn lại và thứ tự tiếp theo

| Thứ tự | Công việc | Trạng thái/phụ trách |
|---|---|---|
| 1 | SQLmutation/nghiệm thu nativeUI/PDF mới cho lifecycle15/10, repeatedrequest/rollback/historypaging/authorization | Để vòng testcuối, không đánh dấuPASS từ kết quả14/10; Khang+Khiêm |
| 2 | Designer,DPI100/125/150/200%,HighContrast,keyboard/modalfocus,screenwidth,SaveFileDialog/máyin thật | Chưa nghiệm thu đầy đủ; Khiêm/người dùng |
| 3 | Dataset lớn,exportperformance và snapshotconsistency khi đa-query gặp thay đổi đồng thời | Cần dữ liệu/tải mục tiêu; team |
| 4 | Peerreviewcode/schema/migration, pushBackend và mergedevelop | Teamreview; chưapush đợt này |
| 5 | Productiontarget,migrationowner,backup/restore,approvedseed/assignments | Admin/BảoAnh; chưa có môi trường |
| Giai đoạn2 | WAC: tồnđầukỳ,transfercost,adjustmentvaluation | Chưa chốt chi tiết; không triển khai |
| Khi phát sinh nhu cầu | Retention/archive và guaranteed eventdelivery; views/SP/trigger | Chưa policy/không bắt buộc tự thêm. Giữ history/best-effort theo quyết định hiện có |

Những gap từng nêu ở auditUI13/10 như rememberlogin/avatar/selfrecovery hay kiểm kê theo phiên độc lập không tự trở thành phạm vi đã triển khai hoặc đã bị loại bỏ. Cần xác nhận scope trước nếu team muốn thêm; không xem stockadjustment là phiên kiểm kê đầy đủ. Bốn lỗiUI13/10 về SubmitDetails/statuslabel/managername/locationwarehouse đã được xử lý ở các đợt trước; không dùng báo cáo cũ để kết luận vẫn lỗi trên HEAD hiện tại.

## 6. Tài liệu và file tạm

- Giữ `.agent/RULE.md`, `.agent/SystemContract.md`, `.agent/TeamChot.md` đang dùng; giữ SQL seed theo yêu cầu trước. README/quy tắc dùng chung trỏ báo cáo này.
- Các báo cáo cũ đã hợp nhất, script sửa một lần, reviewSQL, bằng chứng/harness và backups chuyển nguyên trạng sang `.temp/` trongrepo; không xóa, không ghi đè target đã có.
- `.temp/` bị loại khỏi Git bằng localexclude; đã chuyển44mục/835file, xác minh SHA256; .temp/AgentMoveManifest_20261015.json có source/destination/hash và ghi riêng việc chỉnh đường dẫn helper để đối chiếu/phục hồi. Không commit thư mục này hoặc local launchSettings.
- Kiến trúc hiện hành và checklistDB là tài liệu chuyên biệt còn dùng. SQLlegacy trongDatabase giữ nguyên; migrations vẫn là schema authority.
- Không chạy lại script/archive cũ như quyết định hiện hành. Harness di chuyển cần restore/build lại; EF10.0.12 hiện có có thể gọi qua DLL trong .temp/ef-tools/.store nếu shim còn giữ đường dẫn cũ.

## 7. Phụ lục lịch sử để không mất nội dung tổng hợp

Các snapshot sau chỉ phản ánh thời điểm nguồn, có thể có trạng thái “chưa có/chưa hoàn thành” đã được giải quyết ở phần hiện hành. Không dùng chúng thay bảng kết quả và backlog ở trên. Giữ đầy đủ nội dung nguồn trong các khối thu gọn để tra cứu ma trận94business/84technical/F01–F32 và bằng chứng; các file nguồn chuyển .temp để chỉ còn một báo cáo hiện hành.

<details>
<summary>Snapshot lịch sử: .agent/TONG_HOP_SC3_VA_KE_HOACH_HOAN_THIEN.md — không phải trạng thái hiện hành</summary>

## Tiến độ hiện hành sau triển khai TeamChot — 14/10/2026

Phần này thay các mục trì hoãn audit/transfer/analytics/UI tương ứng trong snapshot bên dưới. Đã hoàn thiện theo quyết định mới trên ba tầng; không suy rằng tất cả yêu cầu lịch sử đều đã nghiệm thu. Xem Documentation/Architecture/TeamDecisionCompletion_2026-10-14.md và CurrentArchitecture.md cho code, quyết định,152unit/4SQL PASS,18bảng schema và giới hạn nghiệm thu. SQL cũ giữ nguyên, fixture TEST đã dọn, production chưa cung cấp, chưa push đợt này.

Còn chờ: WAC và tồn đầu kỳ/định giá chuyển-điều chỉnh; retention/archive; production và peer review; Designer/DPI/máy in/dataset lớn. Không còn câu hỏi formula ABC/90ngày/giai đoạn đầu bị bỏ ngỏ: người dùng đã duyệt chi tiết trong TeamChot.

---
## Cập nhật hiện hành ngày 14/10/2026 — sau nghiệm thu

Phần này có ưu tiên so với các snapshot lịch sử phía dưới. Chuẩn là `.agent/SystemContract.md` hiện hành; người dùng đã cho phép hoàn thiện cả ba tầng, commit theo từng tầng và dùng `localhost / SmartInventoryManagerDB` làm DB TEST. Server production riêng chưa được cung cấp. SQL cũ được giữ nguyên; không drop/reset database và không push đợt này.

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

Các thay đổi `.github/copilot-instructions.md` và connection launchSettings có trước được giữ local ngoài commit code. README và checklist đã cập nhật nguồn schema/migration/bootstrap hiện hành. Dự án đã tiến thêm ở CORE, chưa chứng nhận hoàn thiện toàn bộ phạm vi hợp đồng.

---

## Snapshot lịch sử — đọc cùng cập nhật hiện hành ở trên

# Tổng hợp chuẩn SystemContract hiện hành và kế hoạch hoàn thiện SmartInventoryManager

## Cập nhật thực hiện của Khang sau bản đối chiếu ngày 13/10/2026

**Trạng thái cập nhật ngày 13/10/2026.** Các mục 1–12 và hai ma trận đã đồng bộ với các sửa Backend dưới đây; kết quả UI trước khi sửa và probe ban đầu được ghi rõ là bằng chứng lịch sử. Đã sửa code không đồng nghĩa đã nghiệm thu toàn bộ tính năng. Nhánh task hiện tại: `fix/backend-independent-workflows`, base `e53a254`. Chưa commit/push/PR. Chỉ sửa Business và Business tests; giữ nguyên thay đổi hướng dẫn và launchSettings có trước. Không sửa Data/Entity/migration/Common/UI/DTO/interface public và không thay dữ liệu development DB.

### Việc đã thực hiện theo độ khó tăng dần

| Thứ tự / độ khó | Mục | Thay đổi đã làm | Xác minh / phần còn lại |
|---|---|---|---|
| 1 / dễ | G23 — Category update | UpdateAsync chặn IsActive=false nếu còn product active, giống SetActiveAsync; validation xảy ra trước thay entity. | Regression giữ nguyên tên/trạng thái khi bị từ chối. SQL/UI save acceptance chưa chạy lại. |
| 2 / dễ | G12 — manager role | WarehouseService chỉ nhận active WAREHOUSE_MANAGER; ADMIN không được gán làm manager chỉ vì quyền hệ thống. | 3 ca: admin bị từ chối, manager active được nhận, manager inactive bị từ chối. Khiêm cần đồng bộ selector UI nếu đang liệt kê ADMIN. |
| 3 / dễ | G02 — Submit Details | Import/Export Submit nạp Details rõ ràng; tổng và validation không phụ thuộc tracking từ thao tác trước. | 2 ca sau ChangeTracker.Clear đều chuyển PENDING_APPROVAL, total20, không ghi tồn/movement. Chưa gửi duyệt lại trên UI/DB development. |
| 4 / trung bình | G01 — pending stock alerts | RefreshAsync dùng Inventory entry Added/Modified của workflow trước; chỉ query persisted quantity khi không có pending entry. Giữ same transaction và không tự SaveChanges/commit riêng. | 4 ca unit: về NORMAL, về OUT_OF_STOCK, lên OVERSTOCK, new Inventory trước first save. Probe SQL chỉ đọc: tracked7/persisted6/alert7, không SaveChanges. Chưa nghiệm thu toàn workflow SQL rollback/race. |
| 5 / trung bình | G03 — active actor | Thêm guard **internal** tái dùng query active actor/role hiện có; mọi mutation Product/Category/Supplier/Warehouse/Location/User/profile/password/notification revalidate trước ghi. GetList user và Get notifications cũng guard. Không đổi các CanManage* sync dành UI hoặc public signatures. | 18 ca disabled/role changed ở9 đường representative đều từ chối trước tracking mutation; giữ dữ liệu. Unit chưa thay proof race user-disable-vs-write trên SQL. Các lookup master-data không kho vẫn chưa đổi toàn bộ policy read. |
| 6 / trung bình | G20 — toàn bộ totals | ReportService aggregate totalQuantity/importTotal/exportTotal trên full authorized/date/status query trước Take5000. | 2 ca5001 records: totalQuantity5001, import10002/export15003, vẫn rows5000 và draft không cộng. Paging/truncation indicator/export-filter còn phụ thuộc DTO/UI. Các query report không cam kết snapshot consistency dưới concurrent writes; cần nhóm chốt nếu yêu cầu. |
| 7 / khó hơn | G04 — transitions/state tracked cũ | Submit/DeleteDraft/Approve/Reject/Resubmit/UpdateDraft/CancelDraft trong cả2 services mở Serializable transaction trên provider relational trước đọc receipt, save và commit cùng boundary. Tất cả8 mutation methods/service, gồm Complete hiện có, Reload scalar receipt state trước quyền/state guard để không dùng tracked status cũ. | 2 ca context khác hủy pending: cả nhập/xuất từ chối Approve, persisted status CANCELLED. 121 tests pass; SQL lock/deadlock/rollback/race/draft detail cache vẫn cần BA/Khang dedicated fixture. Không khẳng định G04 đã nghiệm thu concurrency hoàn toàn. |

### Kết quả kiểm tra mới nhất

- Build toàn `SmartInventoryManager.slnx --no-restore`: **0 error, 0 warning** sau thay đổi cuối.
- Tests `--no-build --no-restore`: **121 passed, 0 failed, 0 skipped**, tăng32 case so với baseline89. File regression mới `SmartInventoryManager.Tests/IndependentBackendFixTests.cs`; cập nhật2 fixture hiện có để actor authenticated có row DB hợp lệ.
- Probe EF read-only: CanConnect=True, pending migration rỗng, pending model=False,17 entity query PASS; alert quantity pending đã chuyển6→7 như expected. Dòng probe navigation0/detailSQL1 là raw query không Include để minh họa EF behavior, không phải Submit service đã sửa còn lỗi.
- Format style verify trên file đã chạm: PASS. Whitespace verify Business và file regression mới: PASS sau format riêng file mới. Lượt verify gồm2 file tests cũ báo whitespace ở code cũ ngoài thay đổi; giữ nguyên, không reformat toàn file/repo. Final logic build/test đã chạy sau sửa cuối; không coi formatter là test behavior.
- Tests mới dùng EF InMemory cho unit; SQL probe chỉ đọc, không chứng minh transaction/race/constraint. UI Debug đang chạy binary trước đó cần restart để nhận code mới; lượt này không điều khiển hoặc thay đổi UI.

### Các điểm cần nhóm thống nhất trước khi làm phần tiếp theo

| Điểm bàn giao | Việc cần chốt cụ thể | Ai xử lý | Phần có thể làm sau khi chốt |
|---|---|---|---|
| B01 — Cancel/reject/completion audit, G06 | Existing Note không lưu riêng lý do rejection/cancellation hoặc completion actor/time; xác định equivalent field hay các cột minimal được duyệt. Chốt giữ lịch sử khi sửa/resubmit và semantics CompletedBy/At. | Bảo Anh + Khang; Khiêm nhập reason | Data/migration → DTO/interface → Business ghi cùng transaction → UI; không add field/đổi Cancel signature tự ý. |
| B02 — Adjustment/transfer reference, G08 | Chốt lưu before/difference/after/direction và shared operation reference cho2 movement. ReferenceId đang nullable int, chưa có allocator/reference uniqueness policy; không dùng timestamp/int random hoặc Note ad hoc để né quyết định. | Bảo Anh + Khang | Sau approval, persistence + service/history/tests; không thêm TransferReceipt/outbox. |
| B03 — Stock authority và official events, G07/G09 | Thống nhất internal posting boundary InventoryService trong same context/transaction, shared publisher/UiServices ownership và consumer contracts; quyết định ReceiptCompleted compatibility, subscriber failure và payload/reference. | Khang + Khiêm; BA transaction infrastructure | Refactor Import/Export delegation và publish2 official event types sau commit, wire consumers; không commit giữa stock và receipt/alert. |
| B04 — Stable error contract, G05 | Chọn typed errors/result giữ compatibility public methods, stable codes gồm ALREADY_COMPLETED/permission/state/stock/concurrency và SQL error mapping. Khiêm xác nhận UI handling/reload/no replay. | Khang + Khiêm + Bảo Anh SQL mapping | Thay expected exceptions/Result theo agreed contract; không tự thêm API framework hoặc nuốt lỗi. |
| B05 — Assignment lifecycle/API, G13 | Removal hay deactivate? active STAFF/kho semantics, role đổi và assignment bị thu hồi, management command/list contract, main-session context switch. Composite PK/Restrict đã có; auditfields mới chưa được phép. | BA + Khang + Khiêm | ADMIN assignment service/UI và current context refresh; không lặp việc tạo entity đã xong. |
| B06 — SQL integration fixture, G27/G04 | Chỉ định dedicated SQL Server test database/connection, setup/migration/seed/cleanup recovery, allowed reset targets và harness chạy lại được. Cần cả conflict transitions, cached detail update, lock/deadlock, rollback/new inventory/alert race, actor revocation. | Bảo Anh + Khang | Integration tests trên DB test; không chạy mutation/reset fixtures trên SmartInventoryManagerDB demo. |
| B07 — Report paging/export, phần còn lại G20/G21 | Trả pagination/total/truncation metadata thế nào, summary full scope vs screen filter, export scope/order/date/type/search và paging policy. Totals full query đã fix; không coi5000rows là complete dataset. | Khang + Khiêm; BA query | DTO/interface & query/UI/export phù hợp; không thay DTO public đơn phương. |
| B08 — Dashboard/analytics, G18/G19/G24 | Bốn components đã approved nhưng date field/range/timezone/aggregation source/topN/ties và valuation/ABC/deadstock/reorder formula chưa chốt. | Khiêm + Khang + BA | Query contracts/Data/Business/UI; BONUS sau core, không dùng công thức draft Excel/SQL như approval. |
| B09 — Notification/SystemLog production, G10 | Event nào mandatory persist cùng transaction, event nào best-effort, active recipients role/kho/context, delivery/retry/audit policy và query contract. | Khang + BA; Khiêm consumers/log UI | Producer và safe logging với model hiện hữu; no outbox/newtable nếu chưa approved. |
| B10 — Data/UI work đã biết | BA tạo migration ReorderPoint >0, giữ MaximumStock>ReorderPoint; Khang sửa validation/tests từ chối ReorderPoint=0, index duplicate/migration setup; Khiêm stockstatus/state labels/manager-location binding/useradmin edit-reset. | Owner tương ứng theo G11/G14–G17/G22/G25/G26 | Không sửa các file ngoài Backend scope trong nhánh này. |

**Điểm dừng:** các sửa độc lập của Khang ở bảng trên đã được làm và unit/probe xác minh. Phần tiếp theo cần B01–B09/owner contracts hoặc dedicated SQL acceptance để tránh schema/API/publisher/query decisions đơn phương. Không bỏ task chỉ vì khó; giữ bằng chứng và câu hỏi cụ thể để người dùng/nhóm xử lý. Không gửi tin nhắn cho thành viên khác, không commit/push.

Ngày đối chiếu: **13/10/2026**, múi giờ Asia/Bangkok. Đây là bản tổng hợp phục vụ cả ba thành viên, gồm yêu cầu chuẩn, trạng thái đã kiểm chứng, lỗi, công việc, phụ thuộc và điều kiện nghiệm thu. **Dự án build được, đăng nhập và đọc UI/DB được, nhưng chưa hoàn thiện theo SystemContract hiện hành.** Đã sửa cảnh báo đọc tồn cũ, Submit thiếu Details và guard actor ở các thao tác đã liệt kê; bổ sung transaction/state reload và sửa totals. Ưu tiên tiếp theo là thống nhất B01–B10, nghiệm thu SQL/UI, rồi hoàn thiện audit/reference, stock authority/events và Dashboard v1.

Bản này thay thế việc sử dụng `ViecCanLam.md`, audit 08/10 và báo cáo UI 13/10 như danh sách việc hiện tại. Các nguồn đó vẫn là bằng chứng lịch sử; bản này không sửa nội dung hoặc tự phê duyệt thay đổi của `SystemContract.md`.

## 1. Nguồn, phạm vi và độ tin cậy

Repository đã xác minh: `C:\Project\SmartInventoryManager`. Nhánh `Backend`, HEAD `e53a254` tại thời điểm đối chiếu. Có thay đổi sẵn trong `.github/copilot-instructions.md` và `SmartInventoryManager.UI/Properties/launchSettings.json`; được bảo toàn. Không fetch/pull thêm trong lượt tổng hợp; không khẳng định remote sau thời điểm này không có thay đổi.

| Nguồn trong .agent | Vai trò và cách sử dụng |
|---|---|
| `SystemContract.md` | Chuẩn quyết định nghiệp vụ/data/service/UI/test; ưu tiên nội dung cập nhật §12–15 khi phần đầu còn baseline cũ. |
| `RULE.md` | Quy trình làm việc, ownership, giới hạn công nghệ/scope/Git/an toàn file; giữ nguyên. |
| `Phan cong.docx` | Đã trích văn bản và bảng; dùng yêu cầu/phân công khi không trái SystemContract hiện hành. Có nội dung 16 entity, trạng thái cũ và ownership cũ phải loại khỏi quy định hiện hành. Không kiểm tra layout/ảnh nhúng trong lượt này. |
| `phan_quyen.docx` | Đã trích văn bản/bảng phân quyền; STAFF một kho và mô tả reject giản lược được thay bằng SystemContract hiện hành. |
| `BẢNG PHÂN CÔNG.xlsx` | Hai sheet: 94 business feature và 84 technical task; giữ STT, cấp độ và phạm vi; không coi cột P/S là quyền sửa mọi layer. |
| `AUDIT_2026-10-08.md` | Snapshot cũ; dùng mã F01–F32 để đối chiếu, không giữ nguyên điểm/đánh giá % hoặc lỗi đã hết. |
| `ViecCanLam.md` | Snapshot banh_db/a50b557, 82 tests, trước cập nhật quyền và apply DB; chỉ dùng tham khảo backlog, không dùng làm trạng thái hiện tại. |
| `UI_TEST_REPORT_2026-10-13.md` | Bằng chứng UI ADMIN trong phiên trước; ma trận “Đã có” không có nghĩa đạt toàn bộ SystemContract hiện hành. Dashboard v1 phải đánh giá lại theo §13.7. |
| `UI_TEST_EVIDENCE_2026-10-13.json` | Bằng chứng accessibility các ca UI; giữ để tái hiện/đối chiếu, không chứa ảnh nghiệm thu DPI. |
| `SeedDemoEnglish.sql` | Fixture demo đã chạy theo yêu cầu người dùng, 3 roles + 10 dòng/bảng khác; không thay schema, không chứng minh notification/log producer có thật. |
| `copilot-instructions.before-pull-20261013.md` | Bản sao an toàn trước pull; không phải instruction authority mới; không tự xóa backup. |
| `DbConnectionProbe/` | Probe local hỗ trợ kiểm chứng EF và các vấn đề tracking/navigation; không phải test SQL integration đầy đủ. |

Đã kiểm kê 5 project, **183 file C# ngoài bin/obj**: Business 49, Common 8, Data 35, Tests 12, UI 79; con số gồm Designer và một file migration phụ không biên dịch. Đã đọc đường chạy trọng yếu authorization, receipt, inventory, alerts, master data, dashboard/report, DTO/event, UI wiring, mapping/migration, SQL và tests; không khẳng định mỗi nhánh trong mọi file đã được chạy.

Quy ước bằng chứng: **R** = runtime UI/SQL đã quan sát trong phiên này hoặc phiên UI trước, phân biệt rõ nguồn; **P** = probe chỉ đọc, không SaveChanges; **S** = source review; **O** = quyết định mở cần nhóm phê duyệt. Trạng thái “Có triển khai” luôn tách khỏi “Đã nghiệm thu”; thiếu bằng chứng không tự động có nghĩa tính năng không tồn tại.

## 2. Chuẩn thống nhất lấy SystemContract hiện hành làm gốc

### 2.1 Công nghệ và kiến trúc

- C#, .NET 10, WinForms + AntdUI, SQL Server, EF Core 10.x, LINQ, ADO.NET/Microsoft.Data.SqlClient; theo version project. Hiện EF packages 10.0.12, SqlClient 7.1.1, MSTest 3.8.3, SDK test 17.14.1; ClosedXML/QuestPDF là dependency đã có. Không đổi stack/add package ngoài quy trình phê duyệt.
- UI gọi Business DTO/service; Business giữ quyền/validation/workflow; Data giữ persistence/query/EF/ADO.NET; Common không phụ thuộc layer khác. UI có reference Data để composition trong Program không tự động là vi phạm; Forms không được dùng trực tiếp SQL/DbContext để bypass Business.
- EF Migration là nguồn duy nhất cho bảng/cột/PK/FK/check/default/index/relationship EF-managed. SQL riêng chỉ cho view/SP/trigger được duyệt/seed tương thích; không tạo lại schema bằng script legacy.
- Chỉ 3 file trong Configurations: ProductConfiguration, InventoryConfiguration, ImportConfiguration; mapping còn lại trong DbContext. Repository/UnitOfWork là tùy chọn, không có nhiệm vụ tạo framework chỉ vì file skeleton.
- Git regular work base/target develop, task branch, PR và một thành viên khác review. Không commit/push develop trực tiếp, không force/reset/clean để giấu divergence. .agent là local-only.

### 2.2 Model và khóa

**17 entity:** Role, User, Category, Product, Supplier, Warehouse, Location, Inventory, ImportReceipt, ImportDetail, ExportReceipt, ExportDetail, InventoryTransaction, StockAlert, Notification, SystemLog, UserWarehouse.

- Inventory key logic `(ProductId, WarehouseId)`, không có Location trong key. Location thuộc Warehouse; không suy quyền STAFF từ location.
- InventoryTransaction có ProductId/WarehouseId, không có InventoryId; ghi movement độc lập với row tồn.
- UserWarehouse đang có composite PK `(UserId, WarehouseId)` và hai FK Restrict/NO_ACTION. Đã chứng minh code + DB, không còn task “tạo bảng assignment” từ đầu. Lifecycle removal/reactivation/backfill vẫn cần chốt khi triển khai quản trị assignment.
- Không thêm Users.WarehouseId, MinimumStock, InventoryLocation, StockAlertHistory, TransferReceipt, Customer hoặc Outbox ngoài contract. Lịch sử resolved alerts có thể dùng StockAlerts hiện có; không cần entity StockAlertHistory.
- Quantity nguyên theo model, money decimal/VND; import/export quantity >0, unit price ≥0, total do Business tính. ReceiptCode unique, không dùng MAX+1 không bảo vệ. Product threshold `0 < ReorderPoint < MaximumStock`; ngưỡng bằng nhau và 0/0 không hợp lệ theo quyết định người dùng ngày 13/10/2026. Code đã từ chối equality/0/0 theo đợt trước (128 tests PASS); chưa đồng bộ yêu cầu mới ReorderPoint > 0.

### 2.3 Phân quyền

| Thao tác | ADMIN | WAREHOUSE_MANAGER | STAFF |
|---|---|---|---|
| Phạm vi kho | Toàn hệ thống theo chính sách kho active | Warehouses.ManagerId = actor | UserWarehouse active user/kho + current context hợp lệ |
| User/role/assignment | Được | Không | Không |
| Product/Category/Supplier | Được | Được | Chỉ xem được phép |
| Warehouse configuration | Được | Không theo rule hiện hành | Không |
| Draft/create/submit | Được trong scope | Được trong scope | Phiếu của mình, kho hiện tại được phân công |
| Edit/delete DRAFT | Được trong scope | Được trong scope | Chỉ của mình |
| Edit REJECTED/resubmit | Theo state/scope | Theo state/scope | Owner trong scope |
| Approve/reject/complete | Được | Trong scope | Không |
| Cancel DRAFT | Được | Trong scope | Chỉ của mình |
| Cancel PENDING_APPROVAL/APPROVED | Được | Trong scope | Không |
| Adjust/transfer/resolve alert | Được | Trong scope cả hai kho khi transfer | Không |
| Edit/delete COMPLETED | Không | Không | Không |

Actor/role/kho do authenticated context cung cấp và Business tái xác minh; UI-supplied IDs không phải permission. User disabled không authenticate hoặc mutate; role đổi/assignment mất/kho inactive phải mất quyền an toàn. Manager assignment vào Warehouse phải là active WAREHOUSE_MANAGER theo §13.8, không nhận ADMIN chỉ để tiện chọn.

### 2.4 Phiếu, transaction và concurrency

`DRAFT → PENDING_APPROVAL → APPROVED → COMPLETED`; reject tạo `REJECTED`, sửa/resubmit về DRAFT trước gửi lại. Cancel chỉ DRAFT/PENDING_APPROVAL/APPROVED → CANCELLED, phải có nonblank CancelReason lưu cùng transition. Completed bất biến; sửa sai bằng reversal/adjustment được duyệt, không sửa/xóa lịch sử.

Chỉ COMPLETED post stock đúng một lần. InventoryService là authority application-level, helper kỹ thuật không thay thế authority. Một transaction bao trùm state + validate + tồn + movement + alert + mandatory audit. Transition phải kiểm tra persisted state tại mutation, request stale thất bại; Serializable hiện có ở Complete không chứng minh các transition khác đã an toàn. Rowversion/idempotency framework không bắt buộc, khóa/index dedup phải xét valid multiplicity trước chốt.

Adjustment phải truy vấn được before/difference/after, reason, actor, time. Transaction.Quantity dương; ADJUSTMENT đơn độc với abs(delta) không mô tả tăng/giảm đủ. Transfer có OUT/IN, source khác destination, kiểm scope/tồn, cùng reference và transaction; không cần entity phiếu chuyển riêng.

### 2.5 Alerts, events, notification và logs

| Quantity | Kết quả |
|---|---|
| 0 | OUT_OF_STOCK |
| >0 và <= ReorderPoint | LOW_STOCK |
| > ReorderPoint và <= MaximumStock | NORMAL |
| > MaximumStock | OVERSTOCK |

Ngưỡng theo Product, áp dụng từng product/kho; tồn âm/ngưỡng sai bị từ chối. NORMAL không có abnormal alert mở; trở về NORMAL hoặc đổi loại phải resolve stale alert. Tối đa một open alert mỗi `(ProductId, WarehouseId, AlertType)`; manual resolve lưu ResolvedBy/ResolvedAt.

Official events: InventoryChangedEvent và StockAlertEvent, payload product/kho/quantity/status/actor/time/reference khi có. Publish sau commit; rollback không success event; subscriber lỗi không replay stock. ReceiptCompletedEvent hiện còn code: đánh giá compatibility/consumer trước thay đổi, không coi nó thay thế hai official events.

Notification producer do Business/application orchestration quyết định, Data persist, UI display; recipient active và role/kho đúng scope. SystemLog ghi action/actor/entity/reference/time/description an toàn. Không log password/hash/token/full connection string. Mandatory vs best-effort và delivery guarantee còn mở; không tự thêm Outbox.

### 2.6 Dashboard, reports và nghiệm thu

Dashboard v1 SystemContract hiện hành §13.7 bắt buộc **4 thành phần**: nhập-vs-xuất quantity theo ngày; horizontal bar các sản phẩm tồn thấp; phân bố 4 trạng thái tồn; recent movement có reference code/type/kho/actor/time. Chart 10 giao dịch nhóm theo loại và recent receipt table không đáp ứng 4 thành phần này, dù tải số liệu thành công.

Chốt date field/range/timezone/aggregation/source/top-N/tie trước query. Metric một nguồn authoritative, không double-count details và movements. Giá trị tồn, ABC, dead-stock/reorder formula còn mở; công thức Excel/SQL draft không phải phê duyệt. ABC/Dead Stock/Reorder là BONUS; không đánh đồng với CORE Dashboard v1.

Nghiệm thu phải có build, unit, SQL Server integration trên DB test riêng và UI acceptance Windows/roles/DPI/keyboard/export. Dev DB có demo không phải test database cho test reset/rollback/concurrency.

## 3. Kết quả xác minh hiện tại

| Kiểm tra | Kết quả | Giới hạn |
|---|---|---|
| Build toàn solution --no-restore | PASS, 0 warning, 0 error | Không chứng minh nghiệp vụ đúng |
| Test --no-build --no-restore | 121 PASS, 0 fail/skip; thêm 32 ca so với baseline 89 | Tests dùng InMemory ở nhiều nhóm, không chứng minh SQL concurrency/constraint |
| EF CanConnect/GetPendingMigrations/HasPendingModelChanges | True / rỗng / False | Kết quả probe trước bản mới; MaximumStock strict > đúng, ReorderPoint >=0 cần đổi >0 G11 |
| 17 entity Take(1) qua EF | PASS | Chỉ đọc; không post stock |
| DB local | 17 bảng nghiệp vụ + history 1 row; Roles 3, bảng khác 10; total stock 105 | Views/SP/trigger chưa có trong DB theo sys.objects |
| Migration | 20261012084010_InitialCreate, EF 10.0.12 | Chưa có dedicated SQL integration suite |
| UserWarehouse | Composite PK, FK NO_ACTION, 10 assignments | Chưa có service/UI assignment quản trị |
| StockAlerts open uniqueness | Có **hai** unique filtered indexes cùng key/filter | F05 cũ “không có index” đã hết; index trùng cần Bảo Anh review |
| Threshold SQL | MaximumStock > ReorderPoint | Đúng quy tắc mới MaximumStock > ReorderPoint; MaximumStock strict > đúng; ReorderPoint >=0 còn lệch bản mới G11 |
| Runtime UI phiên trước | Visual Studio Debug, ADMIN tự đăng nhập, tải main pages; tìm SP/Category, Dashboard kho08, STAFF user filter, notification read | Chưa test MANAGER/STAFF runtime, CRUD save toàn bộ, export, stock operations commit/DPI |
| Probe Details (query thô, bằng chứng ban đầu) | IsLoaded=False, navigation Count=0, SQL detail Count=1 | Query không Include không tự tải navigation; Submit hiện đã Include Details và có regression fresh context. Chưa chạy lại Submit trên UI |
| Probe alert quantity sau sửa | Tồn tracked 7, persisted 6, alert quantity 7 | Trước sửa alert=6; sau sửa dùng pending quantity đúng. Probe không SaveChanges, chưa chứng minh SQL commit/rollback |

Các thao tác UI trước chỉ đổi notification admin sang IsRead=True; submit DEMO-EXP-01 thất bại ở phiên UI trước sửa, vẫn DRAFT/detail=1; chưa chạy lại UI sau sửa G02. Lượt tổng hợp này chỉ đọc DB/probe; không chạy seed, migration, hoặc sửa dữ liệu nghiệp vụ.

## 4. Lỗi và gap cần xử lý

P0 = ảnh hưởng quyền/tính toàn vẹn tồn; P1 = chặn workflow hoặc thiếu contract bắt buộc; P2 = hiển thị, tài liệu, hoàn thiện vận hành. Mức ưu tiên dưới đây là đề xuất review, không tự cấp quyền sửa liên tầng.

| ID | Ưu tiên / bằng chứng / trạng thái | Vấn đề và ảnh hưởng | Nguồn code | Chủ trì / phối hợp |
|---|---|---|---|---|
| G01 | Đã sửa code + 4 regression + probe | Refresh ưu tiên Inventory Added/Modified cùng context; bỏ đọc tồn persisted cũ khi có mutation pending. Probe tracked7/persisted6/alert7. Còn nghiệm thu atomic commit/rollback và race trên SQL. | StockAlertService.RefreshAsync; IndependentBackendFixTests | Khang; BA phối hợp SQL |
| G02 | Đã sửa code + regression | Submit Import/Export đã Include Details; fresh context/clear tracking có detail, total20, không post stock. UI FAIL trước sửa là lịch sử; còn chạy lại SQL/UI Submit và nhiều dòng. | ImportService.SubmitAsync; ExportService.SubmitAsync; IndependentBackendFixTests | Khang; Khiêm UI acceptance |
| G03 | Đã sửa các đường đã liệt kê + 18 regression | Guard async đọc active user/role DB trước mutation master data/user/profile/password/notification; user list và notification read cũng guard. CanManage* đồng bộ vẫn dùng DTO cho UI visibility; chưa thay mọi generic read và chưa chứng minh race thu hồi quyền trong SQL write. | AuthorizationService.EnsureActiveActorAsync; các Business services; IndependentBackendFixTests | Khang; BA SQL; Khiêm UI |
| G04 | Một phần: code + 2 regression | Bảy mutation phiếu mỗi service mở Serializable trước đọc khi provider relational; cả tám mutation gồm Complete reload scalar state trước guard. Test tracked Pending rồi persisted Cancelled không Approve. Còn SQL race/deadlock/rollback và cache draft details; chưa đóng concurrency. | ImportService/ExportService; IndependentBackendFixTests | Khang + BA SQL acceptance |
| G05 | P1 / S | Expected business errors chưa có mã stable/Result; ALREADY_COMPLETED là InvalidOperationException text. LiveUi gộp nhiều lỗi thành thông báo chung và DbUpdateException hướng dẫn restart áp dụng migration dù Program không auto-migrate. | Interfaces/DTOs; CompleteAsync; LiveUi.Error; Program | Khang + Khiêm; BA phân loại SQL |
| G06 | P1 / S | CancelDraftAsync không nhận/lưu reason; reject ghi Note rồi UpdateDraft ghi đè. Thiếu persisted completion actor/time; CreatedBy/ReceiptDate không tương đương hoàn tất. | Import/Export service/interfaces/entities | Khang + BA contract; Khiêm form |
| G07 | P1 / S | Import/Export gọi StockMutationService trực tiếp và tự ghi stock/movement, không gọi InventoryService authority theo SystemContract hiện hành. Helper public không kiểm actor/transaction là boundary kỹ thuật cần giới hạn. | StockMutationService; Import/Export Complete | Khang |
| G08 | P1 / S | Adjustment chỉ ghi abs(delta), ADJUSTMENT, reason/actor/time; thiếu audit before/difference/after đáng tin cậy. Transfer OUT/IN không gán shared ReferenceId. | InventoryService:167, TransferAsync; InventoryTransaction | Khang + BA |
| G09 | P1 / S | Official events không phát đủ các đường: import/export chỉ ReceiptCompletedEvent; adjust/transfer chỉ InventoryChanged; alert refresh publishEvent=false không publish lại sau commit; product threshold/active cũng suppress. UI subscribe Alerts instance khác các instance tạm. | Services; NotificationPanel; FrmDashboard | Khang + Khiêm |
| G10 | P1 / S | Không có notification producer/recipient routing hoặc SystemLog producer. Dữ liệu demo và UI read không chứng minh tự sinh khi có nghiệp vụ. Guarantees mandatory/best-effort chưa chốt. | NotificationService chỉ read/mark; không có SystemLog service/ghi trong workflows | Khang + BA; Khiêm log UI |
| G11 | P1 / contract mới 14/10/2026 | SystemContract.md yêu cầu 0 < ReorderPoint < MaximumStock. Business và CHECK hiện cho phép ReorderPoint=0; cần Khang sửa validation/tests và Bảo Anh tạo migration mới CK_Products_ReorderPoint > 0. Không sửa baseline đã apply; chưa nghiệm thu SQL/UI. | StockStatusCalculator; ProductConfiguration; migrations | Khang + Bảo Anh; Khiêm UI validation |
| G12 | Backend đã sửa + regression; UI còn | ValidateManagerAsync chỉ nhận active WAREHOUSE_MANAGER, từ chối ADMIN. Còn đồng bộ selector UI và nghiệm thu lưu kho trên SQL/UI. | WarehouseService.ValidateManagerAsync; FrmWarehouseDetail selector | Khang backend; Khiêm selector |
| G13 | P1 / S | UserWarehouse đã có model/schema/auth, nhưng chưa có ADMIN service/UI assignment và chưa có luồng chuyển current context STAFF trong main session; hiện chọn tại login. | AuthorizationService; FrmWarehouseContext; Program; search SetCurrentWarehouse | Khang + Khiêm; BA lifecycle |
| G14 | P1 / R+S | Enum stock status được đổi sang tiếng Việt rồi StatusText đổi lần hai thành Chưa xác định; report string path lại đúng. | DataListPage:71–73; InventoryDto | Khiêm |
| G15 | P2 / R+DB+S | WarehouseUi bind ManagerName không tồn tại trong WarehouseDto; manager DB có nhưng cột trống. | WarehouseUi:227; MasterDataDtos:25 | Khiêm + Khang contract |
| G16 | P2 / R+DB+S | Location UI bind WarehouseName không có trong LocationDto; tab 10 dòng cùng mã không phân biệt kho, thiếu lọc kho. | WarehouseUi:216; MasterDataDtos | Khiêm + Khang contract |
| G17 | P1 / S | LiveUi.StatusText thiếu PENDING_APPROVAL/APPROVED/REJECTED; receipt/report sẽ hiển thị Chưa xác định cho ba trạng thái này. Chưa runtime các state này; Submit backend đã sửa G02, UI chưa kiểm thử lại. | LiveUi.StatusText; ReceiptListView.CellFormatting | Khiêm |
| G18 | P1 / R+S | Dashboard mới có KPI/alert/recent receipts; chưa đạt 4 thành phần §13.7. Chart ghi 20 nhưng service Take(10), không chart daily import/export, lowest-stock horizontal hoặc distribution. Recent table thiếu movement actor/reference. | FrmDashboard:126–137; DashboardService:61; DashboardDto | Khiêm + Khang DTO + BA query |
| G19 | P1 / S+O | Giá trị tồn/advanced analytics chưa có công thức chuẩn. Draft SQL InventoryCostValue/PotentialSellingValue và Excel ABC/90-day/reorder không tự được coi phê duyệt. | SystemContract hiện hành §13.9; SQL05/06; XLSX | Khiêm + Khang + BA chốt |
| G20 | Một phần: totals đã sửa + 2 regression | Report totals aggregate full authorized/date/status query trước Take(5000); 5001-row cases PASS. Rows vẫn cap5000, list cap500/lookup200; chưa paging/truncation metadata và chưa chốt export-filter/snapshot semantics. | ReportService; list/lookup DTO/UI | Khang + BA query; Khiêm paging |
| G21 | P1 / S | Excel/PDF export lấy lại report scope/date không áp search/type đang hiển thị; chưa GUI file test. In/xuất phiếu chi tiết riêng chưa có. | ConnectedPages.ExportInventory/ExportImportExport; ReportExportService | Khiêm + Khang contract |
| G22 | P1 / S | UI user chỉ tạo/detail read-only/deactivate; Business UpdateUser/ResetPassword có nhưng chưa UI admin. Không cần tạo thêm backend trùng. | FrmUserManagement; FrmUserDetail; UiDetails; UserService | Khiêm + Khang |
| G23 | Backend đã sửa + regression | Category.UpdateAsync chặn IsActive=false khi còn active products trước gán entity, nhất quán SetActiveAsync. Còn nghiệm thu save/toggle end-to-end SQL/UI. | CategoryService.UpdateAsync/SetActiveAsync; IndependentBackendFixTests | Khang; Khiêm UI acceptance |
| G24 | P1 / S+DB | ADO.NET executor/provider/query rỗng, SQL views/SP có nội dung draft nhưng chưa deploy/consume; DB không có object user V/P/TR. Cần use case thật/parameterized/scoped, không chỉ bỏ header skeleton. | Data/AdoNet; Data/Queries; Database05/06 | BA + Khang + Khiêm |
| G25 | P1 / S+DB | Bootstrap/seed/rebuild docs chưa cập nhật việc DB local đã apply; runtime localhost nhưng design factory SQLEXPRESS; seed08 placeholder, seed11 có thể overwrite admin. Seed local mới chỉ dùng DB trống/admin, chạy lại dừng có kiểm soát, không phải idempotent upsert/general seed. | Factory; App.config/launchSettings; DatabaseRebuildChecklist; SQL08/11; local seed | BA; Khang/Khiêm phối hợp |
| G26 | P2 / S+DB | Hai filtered unique indexes trùng key/filter open alerts; `_BASE_678` snapshot phụ đã excluded compilation nhưng vẫn tồn tại. Review có chủ đích, không xóa/tạo index lại tự động. | DbContext:569/608; Data csproj; Migrations | BA |
| G27 | 121 unit PASS; nghiệm thu còn thiếu | Thêm 32 regression trong IndependentBackendFixTests; chưa dedicated SQL Server integration fixture và GUI acceptance all roles sau sửa. InMemory không chứng minh SQL concurrency/constraint. | 12 test files; Tests InMemory; local read-only probe | Khang + BA + Khiêm |
| G28 | P2 / S | Official SRS/UseCase/ProjectReport chỉ Skeleton; ERD/Class/Architecture mỗi file 2 root cells, chưa mô hình. Assignment Word/Excel có rule cũ, SystemContract hiện hành tự có đoạn baseline16/SHOULD/open mapping chưa đồng nhất đoạn mới. | Documentation/*; .agent sources; SystemContract hiện hành §2.7/7.4/13–15 | BA lead docs; Khang Business/Class; Khiêm UI/report |
| G29 | P2 / S | Avatar disabled, Remember không persistence, Forgot chỉ liên hệ admin; không tự áp yêu cầu public signup/MFA/session expiry khi contract chưa chốt. | Login/AccountSettings | Khiêm + Khang khi approved |
| G30 | P2 / S | Common enums/session/helpers, repository/interface và MainMenu skeleton chưa dùng không tự là thiếu feature; cần kiểm dependency/Designer trước giữ/xóa. | Common/Data repositories/UI MainMenu | Owner tương ứng |

### Phần đã sửa và bước xác minh còn lại

- G01 đã dùng quantity Added/Modified cùng context, không thêm commit riêng. Tiếp theo test SQL tăng/giảm/new inventory/transfer và rollback toàn transaction.
- G02 đã Include Details, giữ validation count và có regression fresh context/clear tracking. Tiếp theo nghiệm thu SQL/UI với nhiều dòng.
- G03/G04 đã bổ sung guard active actor, Serializable và reload scalar state. Tiếp theo SQL race thu hồi quyền/state, rollback/deadlock và cache detail; stable error contract G05 còn chờ nhóm.
- G06/G08: audit trường tương đương trước; chốt schema/reference minimal với BA, cập nhật contract rồi DTO/Business/UI. Không nhét JSON/string vào Note mà không contract queryable.
- G09: tập hợp official payload rồi dispatch từ shared publisher sau commit; xử lý subscriber failure không làm replay. Không tự thay Outbox.
- G14–G17: một lần format/DTO binding chuẩn. G15/G16 phải thống nhất contract display names, không đổi Entity/schema chỉ để thêm tên hiển thị.

## 5. Công việc theo thành viên và điều kiện đóng

### Bảo Anh — Database, Architecture, Data, EF, ADO.NET

| Mã mới | Việc cần bàn giao | Phụ thuộc / điều kiện hoàn thành |
|---|---|---|
| BA-01 | Đồng bộ ReorderPoint > 0 bằng migration tiến lên | G11; cập nhật model/snapshot/migration mới, rà dữ liệu ReorderPoint=0 trước apply; giữ MaximumStock > ReorderPoint và không regenerate baseline/drop data |
| BA-02 | Audit CancelReason/completion actor-time/adjustment metadata/shared reference, đề xuất schema minimal | G06/G08; Khang xác nhận semantics, approve trước add field |
| BA-03 | Dedicated SQL test database/harness với Khang | G01/G04/G11/G27; setup/seed/cleanup/recovery rõ, không reset development DB |
| BA-04 | UserWarehouse lifecycle/backfill/seed/docs, validate key/FK hiện có | G13; giữ composite/Restrict trừ approved change; không làm lại entity đã có |
| BA-05 | Approved Data queries/ADO.NET cho Dashboard/report/analytics | G18/G20/G24; parameterized, authorized scope, một nguồn metric, shared EF transaction nếu mutation |
| BA-06 | Chuẩn hóa SQL-managed object deploy và seed sau migration | G24/G25; không view/SP stock posting, không giả định formula, không overwrite admin ngoài chủ đích |
| BA-07 | Thống nhất runtime/design target, cập nhật rebuild/README và seed chính thức | G25; target explicit, test apply + histories + runnable seed |
| BA-08 | Review index trùng/snapshot phụ/repository skeleton | G26/G30; dependency/diff review, exact delete approval, không coi skeleton bắt buộc |
| BA-09 | ERD/Architecture/SRS/UseCase/Data report và source model audit | G28; 17 entity, independent stock branches, migration authority; phối hợp toàn nhóm |

### Khang — Business, Backend, Warehouse Operations

| Mã mới | Việc cần bàn giao | Phụ thuộc / điều kiện hoàn thành |
|---|---|---|
| K-01 | Tracked quantity/alert và Submit Details | G01/G02 đã sửa, unit/probe đạt; còn SQL atomicity/fresh context/multiple lines và UI Submit. |
| K-02 | Active actor/role trên quản trị/master/profile/notification | G03 đã guard các đường đã liệt kê, 18 regression; còn SQL revocation race và UI acceptance. |
| K-03 | State transitions/concurrency/error codes stable | G04 đã Serializable + scalar reload, 2 stale-state regression; SQL concurrency chưa nghiệm thu. G05 chờ B04, BA phân loại SQL/Khiêm UI. |
| K-04 | InventoryService authority và official event delivery | G07/G09; same context/transaction, publish sau commit, subscriber failure không replay |
| K-05 | Cancel/reject/completion audit, adjustment before/after và transfer reference | G06/G08; BA-02 approved; UI reason/contract handoff |
| K-06 | ADMIN assignment service và current-context validation | G13; user STAFF active/kho active, scope revocation, không tự add assignment audit fields |
| K-07 | Manager role + category activation consistency | G12/G23 Backend đã sửa và regression đạt; còn UI selector manager và SQL/UI save/toggle. |
| K-08 | Notification orchestration và SystemLog production | G10; chốt recipient/transaction guarantees, safe logs; BA persistence/Khiêm UI |
| K-09 | Dashboard/report query contracts và aggregate/paging | G20 totals đã sửa trước cap, 2 ca5001 rows đạt; paging/export B07, Dashboard/metrics B08 còn chờ nhóm. |
| K-10 | Business tests + SQL integration với BA | 121 unit PASS, tăng32; dedicated SQL fixture B06 chưa có. Còn rollback/race/multi-product/reject equal thresholds/revoked actor và all-role UI. |
| K-11 | DTO/interfaces/display/error handoff cho UI | G15/G16/G21/G22; shared contract review, không create duplicate services |
| K-12 | Class/use-case/business-flow/documentation contributions | G28; đúng owner, actual implementation/acceptance, không khẳng định % lịch sử |

### Khiêm — UI, Dashboard, Analytics, Reports, Notification UI

| Mã mới | Việc cần bàn giao | Phụ thuộc / điều kiện hoàn thành |
|---|---|---|
| KH-01 | Fix StockStatus format và receipt state labels | G14/G17; test tất cả 4 stock + 6 receipt states |
| KH-02 | ManagerName/WarehouseName binding và location warehouse filter | G15/G16; K-11, row có tên kho/manager thật; không change schema tự ý |
| KH-03 | Assignment UI ADMIN và đổi context STAFF trong main | G13; K-06/BA-04; refresh/clear cached data toàn scope, deny revoked assignments |
| KH-04 | Cancellation reason + stable errors/stale-state/busy/retry-safe | G05/G06; K-03/K-05; giữ input khi fail, thành công chỉ sau commit |
| KH-05 | Dashboard v1 đầy đủ 4 thành phần | G18; K-09/BA-05 + approved metric; không thay daily chart bằng recent-group chart |
| KH-06 | Filter category/product, resolved-alert history/actor nếu contract có | G15/G16/feature gaps; backend DTO cần bàn giao, không thêm StockAlertHistory |
| KH-07 | Report filters/export/paging/receipt print | G20/G21; file Excel/PDF mở/render đúng, totals/scope/filter theo agreed semantics |
| KH-08 | UI admin update user/role/reset bằng existing Business, log view khi producer/query sẵn | G22/G10; không tạo backend trùng, không lộ hash |
| KH-09 | Notification recipient/context display/event integration | G09/G10; K-08, refresh đúng scope, unsubscribe khi dispose |
| KH-10 | Analytics ENHANCED và BONUS sau core | G19/G24; approved formulas/DTO/query, không gắn nhãn AI cho formula đơn giản |
| KH-11 | Acceptance all roles/stock workflows/export/keyboard/1366x768/DPI100–200% | G27/G29; expected/actual data/evidence; build alone không pass UI |
| KH-12 | UI docs/screenshots/demo, settings/Login UX nếu được duyệt | G28/G29; không tự thiết kế authentication/reset policy mới |

Các task code chỉ được bắt đầu trong scope tính năng được giao; bản phân công này là kế hoạch và review, không ủy quyền tự sửa layer của thành viên khác. Shared DTO/interface/auth/schema thay đổi cần agreed process.

## 6. Quyết định mở phải chốt trước việc phụ thuộc

| Quyết định | Đã biết | Còn phải chốt / chủ trì |
|---|---|---|
| Assignment lifecycle | 17th entity + composite/Restrict đã hiện hữu và live | Removal/deactivation policy, revoked role/account, backfill/source assignments; BA+Khang |
| Receipt audit | Creator/date và movement actor/time có, completion fields thiếu | Fields equivalent + minimal persistence CancelReason/RejectReason/completion audit; BA+Khang |
| Adjustment audit | Positive quantity + reason/actor/time có | Reliable before/difference/after/direction và query contract; BA+Khang |
| Transfer/dedup | 2 movements cùng transaction, ReferenceId nullable int | Shared operation reference allocation + valid multiplicity/key strategy; BA+Khang |
| Events/notifications/audit | Official two events, no approved outbox | Mandatory writes/recipient/delivery/failure handling; Khang+BA, Khiêm consumers |
| Dashboard v1 metrics | 4 components approved | Date field/default range/timezone/topN/tie/source/aggregation; Khiêm+Khang+BA |
| Valuation/advanced analytics | Không dùng MinimumStock, BONUS làm sau | Inventory valuation basis, ranking, ABC cuts, dead stock window/reorder rules; Khiêm+Khang+BA |
| Nội bộ SystemContract hiện hành | Phần mới 12–15 là cập nhật confirmed | Sửa wording baseline16 và SHOULD/open-key cũ cho nhất quán; không thay business meaning hoặc tự add fields |

## 7. Thứ tự thực hiện và ma trận nghiệm thu

1. G01/G02/G03/G12/G23 đã sửa Backend, G04/G20 đã sửa một phần. BA/Khang thống nhất B06 và dựng SQL fixture riêng; Khiêm kiểm thử lại Submit và thực hiện display/selector fixes trong phạm vi UI.
2. Nghiệm thu SQL actor/state đã sửa; thống nhất B03/B04 trước authority/event/error contracts. BA xác minh CHECK strict > và chuẩn bị audit/reference proposal; Khang đồng bộ validation/tests G11; Khiêm sửa binding/state labels.
3. Chốt metadata/assignment lifecycle → BA migration/persistence → Khang service contracts → Khiêm UI reason/assignment/user admin.
4. Chạy toàn vòng receipt + stock/transfer/alerts SQL, including race/rollback/subscriber failure; chỉ đóng CORE khi actual results đạt.
5. Chốt Dashboard metric → BA query/Khang DTO → Khiêm đủ4 charts/movement table; reports/export/notification/log production.
6. All-role UI/DPI/keyboard/export và official docs; ENHANCED/BONUS theo agreed priority sau core.

| Nhóm | Ca bắt buộc | Owner / bằng chứng |
|---|---|---|
| Actor/scope | ADMIN/MANAGER/STAFF; zero/multi assignments; current context missing; user/role/kho disabled; scope forged/revoked | Khang unit, BA SQL, Khiêm UI |
| Receipt | Fresh context detail loading; empty draft save; submit/approve/reject/edit/resubmit/cancel reason; invalid/stale transitions; completed immutable/repeat conflict | Khang + BA SQL; Khiêm buttons/messages |
| Stock | Positive/zero price rules, insufficient export, multiple products, new inventory row, atomic rollback at each stage | Khang + BA dedicated SQL |
| Race | Concurrent complete vs cancel/update/delete/approve; two exports/adjust/transfer; duplicate alert insert; deadlock/timeout/unknown commit outcomes | Khang + BA; no InMemory substitution |
| Adjustment/transfer | Before/diff/after queryable; positive movement quantity; shared reference; failure destination rolls back source | Khang + BA |
| Alerts/events | quantity 0/equal/below/above thresholds; reject equal configuration thresholds và 0/0, NORMAL close, type switch, quantity after tracked changes, resolve actor/time; rollback/no event; after-commit subscriber failure | Khang + BA; Khiêm display/event |
| Migration/seed | 17 schema, key/FK/restrict, live strict > constraint, histories, open unique, safe rerun, hashes valid/no output secrets | BA |
| Dashboard/reports | Approved aggregation/source/date bounds, lowest-stock/distribution, >5000 rows exact totals/limits, identical scope and agreed screen-export filters | BA query/Khang/Khiêm |
| UI | CRUD/save error preservation, all roles, switch context, disabled action+backend denial, repeated clicks, reconnect, Vietnamese long text, resize/DPI/keyboard, exported file QA | Khiêm |
| Docs | SRS/use-case/ERD/Class/Architecture/report match SystemContract hiện hành and shipped code/test status | All by ownership |

Mỗi task đóng phải kèm branch/head/files, SystemContract hiện hành clause, DTO/schema change, migration/target nếu có, commands/results, expected/actual runtime, chưa chạy gì và recovery. Không quy phần trăm hoàn thành từ số file/121 unit tests/điểm audit cũ.

## 8. Đối chiếu F01–F32 lịch sử

| F cũ | Trạng thái hiện hành, không đóng toàn bộ theo snapshot |
|---|---|
| F01/F06 | UserWarehouse + current-scope auth đã code/live, 121 tests có case authorization và active actor; guard quản trị đã bổ sung G03; switch/revoke runtime vẫn G13/G27. |
| F02 | Receipt buttons state/owner đã nối và quan sát draft/completed; G17 labels và Submit G02 vẫn mở. |
| F03/F04 | G04 đã mở Serializable cho mutations và reload scalar state; G01 tracked alerts đã sửa + regression/probe. SQL race/rollback/deadlock và cache draft details G27 chưa nghiệm thu. |
| F05 | Thiếu unique index đã hết; live có2 index; G26 duplicate-index review và alert race/error mapping còn mở. |
| F07 | Helper mutation chung đã có, authority SystemContract hiện hành chưa đạt G07. |
| F08/F09 | DeleteDraft/state guard/generated unique code/total có; CancelReason/completion audit G06, Submit G02 đã sửa; G04 đã transaction/reload nhưng SQL acceptance còn, stable errors G05 chưa đạt. |
| F10 | Adjustment/transfer có code/UI; audit direction/reference G08 chưa đạt. |
| F11 | MaximumStock > ReorderPoint đã đồng bộ; bản mới bổ sung ReorderPoint >0, cần validation/tests và migration mới G11. |
| F12 | Product activation/threshold refresh có nhưng tracked alert/event G01/G09 và SQL verification chưa đạt. |
| F13 | Supplier CRUD wiring có, list runtime10; save/active SQL acceptance chưa đủ. |
| F14 | User backend update/reset/create/deactivate có; runtime list/filter; admin update/reset UI G22 thiếu. |
| F15 | Notification read+mark runtime đạt; persistence producer/recipient G10/event G09 chưa đạt. |
| F16 | SystemLog chỉ entity/seed, producer và UI tra cứu chưa có G10. |
| F17/F18 | Location/kho CRUD wiring/manager selector có; binding G15/G16 còn; manager-role Backend G12 đã sửa, selector UI cần đồng bộ. |
| F19 | Category search/refresh runtime đạt; Update-vs-SetActive G23 đã sửa + regression, SQL/UI save chưa nghiệm thu. |
| F20 | Product category selector và Include Category đã có, search runtime đạt; category filter danh sách/save/SQL acceptance còn. |
| F21 | KPI labels/filters/alerts/recent receipts có runtime; chưa Dashboard v1 G18/valuation G19. |
| F22 | Analytics chưa nối; SQL draft không phải implementation accepted; BONUS vẫn riêng G19/G24. |
| F23 | Report type filter đã có và quan sát draft exclusion; totals trước cap G20 đã sửa; paging/filter export/print G20/G21 còn. |
| F24 | Payload events đã mở rộng; full official event path/consumer publisher G09 chưa đạt. |
| F25 | ADO.NET/query skeleton chưa có use-case real G24. |
| F26 | Migration local đã apply và seeded; bootstrap chính thức/target/docs/views/SP G25/G24 còn. |
| F27 | 121 unit tests pass; dedicated SQL + UI all-role/DPI/export chưa có G27. |
| F28 | Warehouse authorization revalidate active role đã có; other operations actor G03 và optional login UX G29 còn. |
| F29 | Unit/threshold/status fields có; status display G14, resolved history/filter/stocktake chưa hoàn thiện/chưa có. |
| F30 | Entity defaults DRAFT đã đúng; không còn lỗi COMPLETED default. |
| F31 | Official artifacts vẫn skeleton G28. |
| F32 | Visual Studio Debug ADMIN UI đã chạy; full DPI/keyboard/export roles chưa có. Build hiện 0 warning; không giữ claim NU1701 như lỗi hiện tại. |

## 9. Tài liệu giữ lại và kế hoạch dọn

**Giữ bắt buộc:** SystemContract hiện hành, RULE, DOCX/XLSX nguồn, UI evidence JSON, seed demo, probe và backup trước pull. Không xóa SystemContract hiện hành hoặc RULE vì bản tổng hợp không thay instruction authority; không xóa binary nguồn chỉ vì đã trích text, còn ảnh/layout và lịch sử cần giữ. Backup trước pull không tự xóa.

Hai file có thể loại khỏi danh sách tài liệu làm việc sau khi người dùng duyệt **đúng đường dẫn**:

| Đường dẫn tuyệt đối | Nội dung / lý do | Khôi phục |
|---|---|---|
| `C:\Project\SmartInventoryManager\.agent\ViecCanLam.md` | Backlog cũ banh_db/a50b557, 82 tests/migration chưa apply; đã hợp nhất và sửa trạng thái trong bản này. | ZIP nguồn ở cùng .agent, kiểm tra hashes trước xóa. |
| `C:\Project\SmartInventoryManager\.agent\AUDIT_2026-10-08.md` | Snapshot lịch sử 94 features/F01–F32/điểm% cũ; mapping và danh mục đã tích hợp; có thể lưu bản archive thay file sống để tránh đọc nhầm. | ZIP nguồn ở cùng .agent, kiểm tra hashes trước xóa. |

Sau khi người dùng duyệt rõ hai đường dẫn trên ngày 13/10/2026, đã kiểm tra lại ZIP khớp byte-for-byte, xác minh target nằm trong repository rồi xóa đúng `ViecCanLam.md` và `AUDIT_2026-10-08.md`. Không xóa tài liệu khác, không recursive cleanup và giữ nguyên ZIP khôi phục. RULE.md / “Deletion requires specific approval” yêu cầu: “Obtain explicit approval for those exact targets.”

## 10. Ma trận 94 business features

Ma trận dưới giữ STT/tên/cấp độ của Excel; trạng thái cập nhật theo SystemContract hiện hành/source/runtime, không sao chép điểm% cũ. Owner biểu diễn từng layer: Khang Business, BA Data, Khiêm UI; ô “Chờ quyết định” không coi là thiếu implement phải tự làm ngay.

| STT | Feature gốc | Cấp | Chủ phần chính theo layer | Trạng thái cập nhật | Bằng chứng / việc còn lại |
|---|---|---|---|---|---|
| 1 | Đăng nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có, runtime ADMIN | AuthService; người dùng đăng nhập Debug; role khác chưa UI acceptance. |
| 2 | Đăng xuất | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa runtime ca logout | Program SignOut/login loop, không tự suy session expiry. |
| 3 | Kiểm tra quyền | CORE | Khang Business; Khiêm UI; BA Data support | Có guard; nghiệm thu còn | Warehouse/context auth và active actor mutations đã có G03; SQL revocation race, assignment/switch G13 còn. |
| 4 | Hash/verify mật khẩu | CORE | Khang Business; Khiêm UI; BA Data support | Có | PBKDF2 SHA256 salted 600000; hash verification và AuthService login đã đạt. |
| 5 | Thêm người dùng | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | UserService.CreateAsync/FrmUserDetail; G03 đã guard; còn SQL/UI validation. |
| 6 | Sửa người dùng | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | UpdateUserAsync có; admin edit UI chưa nối G22. |
| 7 | Xóa/khóa người dùng | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI write | Deactivate giữ lịch sử, UI action có; actor G03 đã guard; SQL/UI acceptance còn. |
| 8 | Tìm kiếm/lọc người dùng | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime | List10, lọc STAFF7; cap500 G20. |
| 9 | Phân quyền Role | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Role/active actor guard G03 đã bổ sung; admin edit/reset UI G22 và assignment/switch G13 còn. |
| 10 | Quản lý Session | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Active actor DB guard đã bổ sung G03; STAFF context switch/lifecycle G13 và all-role UI còn. |
| 11 | Thêm sản phẩm | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | ProductManagementService/form thật; giá/ngưỡng tải đúng; G03 đã guard; G09/G11 + SQL/UI save còn. |
| 12 | Sửa sản phẩm | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | ProductManagementService/form thật; giá/ngưỡng tải đúng; G03 đã guard; G09/G11 + SQL/UI save còn. |
| 13 | Ngừng hoạt động sản phẩm | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | ProductManagementService/form thật; giá/ngưỡng tải đúng; G03 đã guard; G09/G11 + SQL/UI save còn. |
| 14 | Tìm kiếm sản phẩm | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime | DEMO-PRD-03 trả1; paging code có, chưa multi-page UI test. |
| 15 | Lọc sản phẩm | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Status filter có, category filter danh sách chưa có. |
| 16 | Quản lý giá nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | ProductManagementService/form thật; giá/ngưỡng tải đúng; G03 đã guard; G09/G11 + SQL/UI save còn. |
| 17 | Quản lý giá bán | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | ProductManagementService/form thật; giá/ngưỡng tải đúng; G03 đã guard; G09/G11 + SQL/UI save còn. |
| 18 | Thiết lập Minimum Stock | CORE | Khang Business; Khiêm UI; BA Data support | Ngoài scope, không triển khai | MinimumStock bị cấm; dùng ReorderPoint/MaximumStock theo SystemContract hiện hành. |
| 19 | Thiết lập Maximum Stock | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | MaximumStock > ReorderPoint đã đúng; Business/Data còn cho ReorderPoint=0, cần sửa theo bản mới G11 và SQL/UI acceptance. |
| 20 | Thiết lập Reorder Point | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | MaximumStock > ReorderPoint đã đúng; Business/Data còn cho ReorderPoint=0, cần sửa theo bản mới G11 và SQL/UI acceptance. |
| 21 | Thêm/sửa/xóa Category | CORE | Khang Business; Khiêm UI; BA Data support | Backend đã sửa guard; chưa UI save | G23 Update/SetActive nhất quán, G03 active actor đã guard; regression đạt, SQL/UI CRUD còn. |
| 22 | Tìm kiếm/lọc Category | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime | List10; search Packaging trả1; trạng thái/refresh đã nối. |
| 23 | Thêm/sửa/xóa Supplier | CORE | Khang Business; Khiêm UI; BA Data support | Có code; list runtime | Supplier list10/search + CRUD service; G03 đã guard; chưa save/toggle SQL/UI acceptance. |
| 24 | Tìm kiếm/lọc Supplier | CORE | Khang Business; Khiêm UI; BA Data support | Có code; list runtime | Supplier list10/search + CRUD service; G03 đã guard; chưa save/toggle SQL/UI acceptance. |
| 25 | Thêm/sửa/xóa Warehouse | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | G12 Backend chỉ active WAREHOUSE_MANAGER; còn selector UI, ManagerName binding G15 và SQL/UI save. |
| 26 | Quản lý Location | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Location CRUD có, list10; WarehouseName trống và thiếu filter G16. |
| 27 | Tìm kiếm/lọc Warehouse | CORE | Khang Business; Khiêm UI; BA Data support | Có code; list runtime | Scoped list10/search/status; actor administration G03 đã guard; binding G15/SQL acceptance còn. |
| 28 | Tạo phiếu nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa full SQL/UI acceptance | Draft editor/detail/Business total có; runtime trước sửa chỉ list completed; G02 đã sửa, G04 SQL/UI acceptance còn. |
| 29 | Thêm sản phẩm vào phiếu nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa full SQL/UI acceptance | Draft editor/detail/Business total có; runtime trước sửa chỉ list completed; G02 đã sửa, G04 SQL/UI acceptance còn. |
| 30 | Tính tổng tiền phiếu nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa full SQL/UI acceptance | Draft editor/detail/Business total có; runtime trước sửa chỉ list completed; G02 đã sửa, G04 SQL/UI acceptance còn. |
| 31 | Xác nhận phiếu nhập | CORE | Khang Business; Khiêm UI; BA Data support | Backend Submit đã sửa; nghiệm thu còn | G02 fresh context regression PASS; G04 Serializable/reload đã có nhưng SQL race/UI chưa nghiệm thu; G05/G06/G07/G09 còn. |
| 32 | Cập nhật tồn kho khi nhập | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Complete transaction/movement có; tracked alert G01 đã sửa; SQL atomicity còn, authority G07/events G09 chưa đạt. |
| 33 | Lịch sử phiếu nhập | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime list | Nhập10 completed/xuất10 draft; creator/scope/totals có; cap500 G20. |
| 34 | In/Xuất phiếu nhập | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có riêng | Report export không phải in/xuất một phiếu đủ dòng chi tiết G21. |
| 35 | Tạo phiếu xuất | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | Draft create/edit/details có; fresh context Submit G02 regression đạt; còn SQL/UI lifecycle. |
| 36 | Thêm sản phẩm vào phiếu xuất | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI save | Draft create/edit/details có; fresh context Submit G02 regression đạt; còn SQL/UI lifecycle. |
| 37 | Kiểm tra tồn kho trước khi xuất | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa SQL race acceptance | Complete kiểm stock/Serializable; tests InMemory không chứng minh concurrency, G01 đã sửa, G04/G27 SQL acceptance còn. |
| 38 | Không cho xuất vượt tồn | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa SQL race acceptance | Complete kiểm stock/Serializable; tests InMemory không chứng minh concurrency, G01 đã sửa, G04/G27 SQL acceptance còn. |
| 39 | Xác nhận phiếu xuất | CORE | Khang Business; Khiêm UI; BA Data support | Backend Submit đã sửa; nghiệm thu còn | G02 fresh context regression PASS; lần UI FAIL là trước sửa. G04 SQL race và G05/G06/G07/G09 còn. |
| 40 | Cập nhật tồn kho khi xuất | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Complete transaction/movement có; tracked alert G01 đã sửa; SQL atomicity còn, authority G07/events G09 chưa đạt. |
| 41 | Lịch sử phiếu xuất | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime list | Nhập10 completed/xuất10 draft; creator/scope/totals có; cap500 G20. |
| 42 | In/Xuất phiếu xuất | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có riêng | Report export không phải in/xuất một phiếu đủ dòng chi tiết G21. |
| 43 | Xem tồn kho | CORE | Khang Business; Khiêm UI; BA Data support | Một phần, lỗi UI | Tồn quantity/unit/ngưỡng đúng; status Chưa xác định G14. |
| 44 | Tìm kiếm tồn kho | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa runtime search riêng | ConnectedPages.Inventory search mã/tên. |
| 45 | Lọc tồn kho | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Warehouse selector có; InventoryDto page thiếu filter status trực tiếp; G14. |
| 46 | Điều chỉnh tồn kho | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Adjust/movement có; before/difference/after G08 và events G09 còn; alerts G01 đã sửa, SQL acceptance còn. |
| 47 | Lịch sử biến động tồn kho | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime read, metadata còn thiếu | IMPORT6/ref1/actor James; adjustment/transfer audit G08. |
| 48 | Xem tồn theo Warehouse | CORE | Khang Business; Khiêm UI; BA Data support | Có runtime ADMIN read | Warehouse01 tồn6, scope validation code; MANAGER/STAFF acceptance G27. |
| 49 | Ghi nhận Inventory Transaction | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Adjust/movement có; before/difference/after G08 và events G09 còn; alerts G01 đã sửa, SQL acceptance còn. |
| 50 | Phát hiện hết hàng | CORE | Khang Business; Khiêm UI; BA Data support | Có Business formula/tests, SQL/UI chưa đủ | Boundary formula đúng; pending quantity G01 đã sửa; SQL persistence acceptance, threshold G11/display G14 còn. |
| 51 | Phát hiện sắp hết hàng | CORE | Khang Business; Khiêm UI; BA Data support | Có Business formula/tests, SQL/UI chưa đủ | Boundary formula đúng; pending quantity G01 đã sửa; SQL persistence acceptance, threshold G11/display G14 còn. |
| 52 | Phát hiện tồn vượt mức | CORE | Khang Business; Khiêm UI; BA Data support | Có Business formula/tests, SQL/UI chưa đủ | Boundary formula đúng; pending quantity G01 đã sửa; SQL persistence acceptance, threshold G11/display G14 còn. |
| 53 | Tạo Stock Alert | CORE | Khang Business; Khiêm UI; BA Data support | Backend quantity đã sửa; còn gap | G01 tracked/new quantity regression và read-only probe PASS; SQL lifecycle/race G27 và events G09 còn. |
| 54 | Resolve Alert | CORE | Khang Business; Khiêm UI; BA Data support | Có code, chưa UI resolve | Permission/ResolvedBy/At có; alert type lifecycle SQL race chưa nghiệm thu. |
| 55 | Hiển thị cảnh báo | CORE | Khiêm UI + Khang Business + BA query | Có runtime open list | Warehouse01 Sắp hết6/ngưỡng20; resolved history/ResolvedBy DTO thiếu. |
| 56 | Notification cảnh báo | CORE | Khiêm UI + Khang Business + BA query | Một phần | Notification read/mark/runtime có; alert notification producer/recipient G10 chưa có. |
| 57 | Tổng số sản phẩm | CORE | Khiêm UI + Khang Business + BA query | Một phần | UI đã gọi Dòng tồn kho đúng semantics InventoryLineCount; distinct product KPI theo scope chưa có. |
| 58 | Tổng số tồn kho | CORE | Khiêm UI + Khang Business + BA query | Có runtime | All105; warehouse08 13 sau refresh; không coi là historical stock. |
| 59 | Tổng giá trị tồn kho | CORE | Khiêm UI + Khang Business + BA query | Chờ quyết định, chưa triển khai | UI dấu —; valuation basis chưa approve G19. |
| 60 | Sản phẩm sắp hết | CORE | Khiêm UI + Khang Business + BA query | Một phần | Open alerts được gắn nhãn đúng; dedicated LOW_STOCK KPI chưa có, Dashboard v1 G18. |
| 61 | Sản phẩm hết hàng | CORE | Khiêm UI + Khang Business + BA query | Có aggregate code/runtime0 | OutOfStockCount đếm inventory rows qty0; semantics distinct product cần metric quyết định. |
| 62 | Thống kê nhập | CORE | Khiêm UI + Khang Business + BA query | Một phần | Completed receipt count/amount và report có; daily quantity Dashboard v1 chưa có G18. |
| 63 | Thống kê xuất | CORE | Khiêm UI + Khang Business + BA query | Một phần | Completed receipt count/amount và report có; daily quantity Dashboard v1 chưa có G18. |
| 64 | Biểu đồ Dashboard | CORE | Khiêm UI + Khang Business + BA query | Một phần | Chart group recent10, title20; chưa đủ4 thành phần Dashboard v1 G18. |
| 65 | Top sản phẩm xuất | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có đường ứng dụng đầy đủ | Draft SQL views/SP có, chưa deploy/query/analytics service/UI G24; metric definition G19. |
| 66 | Phân tích xu hướng nhập/xuất | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có đường ứng dụng đầy đủ | Draft SQL views/SP có, chưa deploy/query/analytics service/UI G24; metric definition G19. |
| 67 | Phân tích tồn kho | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có đường ứng dụng đầy đủ | Draft SQL views/SP có, chưa deploy/query/analytics service/UI G24; metric definition G19. |
| 68 | Phân tích sản phẩm bán chạy | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có đường ứng dụng đầy đủ | Draft SQL views/SP có, chưa deploy/query/analytics service/UI G24; metric definition G19. |
| 69 | ABC Analysis | BONUS | Khiêm UI + Khang Business + BA query | BONUS, chưa có/chờ định nghĩa | Không áp ABC/90days/reorder formula Excel như approved SystemContract hiện hành; làm sau CORE G19. |
| 70 | Dead Stock Analysis | BONUS | Khiêm UI + Khang Business + BA query | BONUS, chưa có/chờ định nghĩa | Không áp ABC/90days/reorder formula Excel như approved SystemContract hiện hành; làm sau CORE G19. |
| 71 | Reorder Recommendation | BONUS | Khiêm UI + Khang Business + BA query | BONUS, chưa có/chờ định nghĩa | Không áp ABC/90days/reorder formula Excel như approved SystemContract hiện hành; làm sau CORE G19. |
| 72 | Báo cáo tồn kho | CORE | Khiêm UI + Khang Business + BA query | Có runtime trước sửa; totals đã sửa | Warehouse01 total6/alert1; G20 full-query totals regression PASS, paging còn; historical/valuation chưa có. |
| 73 | Báo cáo nhập kho | CORE | Khiêm UI + Khang Business + BA query | Có runtime read; còn gap | Completed-only report: import72/export0; Export filter empty đúng; full-query totals G20 đã sửa; paging/date semantics/export G20/G21 còn. |
| 74 | Báo cáo xuất kho | CORE | Khiêm UI + Khang Business + BA query | Có runtime read; còn gap | Completed-only report: import72/export0; Export filter empty đúng; full-query totals G20 đã sửa; paging/date semantics/export G20/G21 còn. |
| 75 | Báo cáo nhập/xuất theo thời gian | CORE | Khiêm UI + Khang Business + BA query | Có runtime read; còn gap | Completed-only report: import72/export0; Export filter empty đúng; full-query totals G20 đã sửa; paging/date semantics/export G20/G21 còn. |
| 76 | Export Excel/PDF | ENHANCED | Khiêm UI + Khang Business + BA query | Có code, chưa GUI file acceptance | ClosedXML/QuestPDF; search/type export không đồng nhất G21. |
| 77 | Ghi System Log | CORE | Khang Business; Khiêm UI; BA Data support | Chưa có workflow/UI thực | SystemLog entity/seed không phải log producer/query screen G10. |
| 78 | Xem System Log | ENHANCED | Khiêm UI + Khang Business + BA query | Chưa có workflow/UI thực | SystemLog entity/seed không phải log producer/query screen G10. |
| 79 | Database Schema | MINIMUM | BA Data | Có code và local DB/probe | 17 model+baseline history/FK/schema load đã chứng minh; G11/G25 + dedicated test acceptance còn. |
| 80 | PK/FK/Relationship | MINIMUM | BA Data | Có code và local DB/probe | 17 model+baseline history/FK/schema load đã chứng minh; G11/G25 + dedicated test acceptance còn. |
| 81 | Constraints | CORE | BA Data | Một phần, contract mismatch | MaximumStock CHECK strict > đúng; ReorderPoint CHECK cần >0 theo bản mới; còn SQL integration acceptance và Business alignment G11. |
| 82 | Indexes | CORE | BA Data | Có live indexes, review trùng | Open alert uniqueness đã có2 index G26; dedup key mới còn review. |
| 83 | Views | ENHANCED | BA Data | Draft SQL, chưa deploy/consume | Live DB không có user Views/SP; G24. |
| 84 | Stored Procedures | ENHANCED | BA Data | Draft SQL, chưa deploy/consume | Live DB không có user Views/SP; G24. |
| 85 | Seed Data | CORE | BA Data | Có demo local; bootstrap chưa hoàn thiện | Fixture English đã chạy, safe-stop rerun; official08 placeholder/11 overwrite G25. |
| 86 | EF DbContext | MINIMUM | BA Data | Có code và local DB/probe | 17 model+baseline history/FK/schema load đã chứng minh; G11/G25 + dedicated test acceptance còn. |
| 87 | EF Configurations | CORE | BA Data | Có3 configurations; cần CHECK ReorderPoint >0 | MaximumStock CHECK strict > đúng, ReorderPoint CHECK >=0 còn lệch bản mới; mappings còn lại trong DbContext; phối hợp Business validation G11. |
| 88 | ADO.NET infrastructure | ENHANCED | BA Data | Chưa có chức năng thật | ADO.NET executor/provider empty G24. |
| 89 | Database migration/setup script | MINIMUM | BA Data | Có code và local DB/probe | 17 model+baseline history/FK/schema load đã chứng minh; G11/G25 + dedicated test acceptance còn. |
| 90 | Layered Architecture | MINIMUM | BA Data | Có layer structure, còn boundary gaps | 5 projects/dependencies; Business Data queries G24, InventoryService authority G07. |
| 91 | Entity Model | MINIMUM | BA Data | Có code và local DB/probe | 17 model+baseline history/FK/schema load đã chứng minh; G11/G25 + dedicated test acceptance còn. |
| 92 | Service Architecture | CORE | Khang Business; Khiêm UI; BA Data support | Một phần | Services có; active actor guard G03 đã bổ sung; stable results/stock authority/events G05/G07/G09 còn. |
| 93 | Event/Delegate | ENHANCED | Khang Business; Khiêm UI; BA Data support | Một phần | Official event payload có, thiếu phát đủ path/shared publisher G09. |
| 94 | Testing Architecture | CORE | Khang unit; BA+Khang SQL; Khiêm UI | 121 unit PASS; nghiệm thu chưa đủ | Build0 warning/error; tăng32 regression. Dedicated SQL rollback/race và UI all-role/export/DPI G27 còn. |

## 11. Ma trận 84 technical tasks

Technical filenames trong Excel là tham chiếu lịch sử. T02–05 nay thực hiện qua EF Migration; T11–15 repositories optional; T51 navigation FrmMain đã thay MainMenu skeleton. Không tạo chức năng trùng để lấp tên file.

| Mã | Task gốc | Thành phần Excel | Owner theo chuẩn hiện hành | Trạng thái / xử lý |
|---|---|---|---|---|
| T01 | Tạo Solution/Project structure | .sln, 4 projects | BA | Có5 projects/solution.slnx, build PASS; Excel4 projects đã lỗi thời. |
| T02 | SQL Database | 01_CreateDatabase.sql | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T03 | Create Tables | 02_CreateTables.sql | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T04 | PK/FK/Constraints | 03_CreateConstraints.sql | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T05 | Indexes | 04_CreateIndexes.sql | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T06 | Views | 05_CreateViews.sql | BA | Có draft SQL; chưa objects live/ADO consumption G24. |
| T07 | Stored Procedures | 06_CreateStoredProcedures.sql | BA | Có draft SQL; chưa objects live/ADO consumption G24. |
| T08 | Seed Data | 08_SeedData.sql | BA | Local seed đã chạy; official bootstrap/rerun policy G25 còn. |
| T09 | EF DbContext | InventoryDbContext.cs | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T10 | EF Entity Mapping | Configurations/*Configuration.cs | BA | Có EF baseline/schema/mapping/live DB; dùng Migration thay SQL01–04; G11/G25/G26 còn. |
| T11 | Repository infrastructure | IRepository.cs, Repository.cs | BA | Skeleton; repository optional, không bắt buộc làm wrapper EF; owner review G30. |
| T12 | Product Repository | ProductRepository.cs | BA | Skeleton; repository optional, không bắt buộc làm wrapper EF; owner review G30. |
| T13 | Inventory Repository | InventoryRepository.cs | BA | Skeleton; repository optional, không bắt buộc làm wrapper EF; owner review G30. |
| T14 | Import Repository | ImportRepository.cs | BA | Skeleton; repository optional, không bắt buộc làm wrapper EF; owner review G30. |
| T15 | Export Repository | ExportRepository.cs | BA | Skeleton; repository optional, không bắt buộc làm wrapper EF; owner review G30. |
| T16 | ADO.NET Executor | StoredProcedureExecutor.cs | BA | Empty executor/provider/query; cần approved use case thật G24. |
| T17 | Report Data Provider | ReportDataProvider.cs | BA | Empty executor/provider/query; cần approved use case thật G24. |
| T18 | Dashboard Queries | DashboardQueries.cs | BA | Empty executor/provider/query; cần approved use case thật G24. |
| T19 | Password Hashing | PasswordHasher.cs | Khang | PasswordHasher thật PBKDF2 salted; hash/Auth verification PASS. |
| T20 | User Session | UserSession.cs | Khang | Business.Authorization.UserSession thật; active actor mutations G03 đã guard; Common.Session skeleton chưa dùng, context switch G13 còn. |
| T21 | Authentication Service | AuthService.cs | Khang | AuthService thật login DB/active/hash; UI ADMIN Debug đã đăng nhập. |
| T22 | Product Service | ProductService.cs | Khang | ProductService là lookup; CRUD ProductManagementService, không làm CRUD trùng; actor G03 đã guard; SQL/UI acceptance còn. |
| T23 | Category Service | CategoryService.cs | Khang | Category CRUD có; runtime search PASS; activation consistency G23 đã sửa + regression; SQL/UI save còn. |
| T24 | Supplier Service | SupplierService.cs | Khang | Supplier service+UI list có, runtime10; actor G03 đã guard; SQL/UI save acceptance còn. |
| T25 | Warehouse Service | WarehouseService.cs | Khang | Warehouse service/scope có; manager role G12 và actor G03 đã sửa; binding G15/selector UI và SQL acceptance còn. |
| T26 | Import Service | ImportService.cs | Khang | Submit G02 đã sửa; G04 Serializable/reload đã bổ sung, SQL acceptance còn; errors/cancel/audit/authority G05–G07/events G09 còn. |
| T27 | Export Service | ExportService.cs | Khang | Submit G02 đã sửa; G04 Serializable/reload đã bổ sung, SQL acceptance còn; errors/cancel/audit/authority G05–G07/events G09 còn. |
| T28 | Inventory Service | InventoryService.cs | Khang | Inventory read/history/adjust/transfer có; G01 đã sửa, SQL acceptance còn; G07/G08/G09 chưa đạt. |
| T29 | Stock Alert Service | StockAlertService.cs | Khang | Formula/lifecycle/resolve có; G01 quantity đã sửa + regression/probe; SQL unique race/events/threshold G09/G11/G27 còn. |
| T30 | Notification Service | NotificationService.cs | Khang | Read/mark/read-all có, runtime mark PASS; producer G10 chưa có. |
| T31 | Dashboard Service | DashboardService.cs | Khang | KPI/recent query có; SystemContract hiện hành v1 query/metric G18/G19/G24 thiếu. |
| T32 | Report Service | ReportService.cs | Khang | Full-query totals trước cap G20 đã sửa + 5001-row regression; paging và screen-export G21 còn. |
| T33 | Analytics Service | AnalyticsService.cs | Khang | Chưa AnalyticsService hoặc equivalent application boundary; metrics G19/G24. |
| T34 | Product DTO | ProductDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T35 | Inventory DTO | InventoryDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T36 | Import DTO | ImportReceiptDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T37 | Export DTO | ExportReceiptDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T38 | Stock Alert DTO | StockAlertDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T39 | Dashboard DTO | DashboardDto.cs | Khang | DTO hiện hữu; cancel/assignment/display/alert actor/dashboard v1 contract cần cập nhật G06/G13/G15/G16/G18. |
| T40 | Product Validation | ProductValidator.cs | Khang | Validators có unit tests; SQL persisted validation/threshold G01/G02/G11 chưa đủ. |
| T41 | Import Validation | ImportValidator.cs | Khang | Validators có unit tests; SQL persisted validation/threshold G01/G02/G11 chưa đủ. |
| T42 | Export Validation | ExportValidator.cs | Khang | Validators có unit tests; SQL persisted validation/threshold G01/G02/G11 chưa đủ. |
| T43 | Inventory Changed Event | InventoryChangedEvent.cs | Khang | Official payload có; các workflow publish sau commit chưa đủ G09. |
| T44 | Stock Alert Event | StockAlertEvent.cs | Khang | Official payload có; các workflow publish sau commit chưa đủ G09. |
| T45 | Main Form | FrmMain.cs | Khiêm | Runtime ADMIN login/main Debug đạt; full roles/switch/DPI G13/G27. |
| T46 | Login UI | FrmLogin.cs | Khiêm | Runtime ADMIN login/main Debug đạt; full roles/switch/DPI G13/G27. |
| T47 | Dashboard UI | FrmDashboard.cs | Khiêm | UI/cards/chart có; không đạt4 Dashboard v1, chart title20/service10 G18. |
| T48 | Statistic Cards | StatisticCard.cs | Khiêm | UI/cards/chart có; không đạt4 Dashboard v1, chart title20/service10 G18. |
| T49 | Chart Control | ChartControl.cs | Khiêm | UI/cards/chart có; không đạt4 Dashboard v1, chart title20/service10 G18. |
| T50 | Alert Card | AlertCard.cs | Khiêm | AlertCard runtime bind10 alerts có; không còn finding skeleton/no bind cũ. |
| T51 | Main Menu | MainMenu.cs | Khiêm | FrmMain navigation hoạt động thay MainMenu; không phải thiếu menu chỉ vì class skeleton. |
| T52 | Notification Panel | NotificationPanel.cs | Khiêm | NotificationPanel runtime list/mark PASS; producer/event paths G09/G10. |
| T53 | Product List UI | FrmProductList.cs | Khiêm | Product list/search/edit fields runtime có; save/category filter/SQL acceptance còn. |
| T54 | Product Detail UI | FrmProductDetail.cs | Khiêm | Product list/search/edit fields runtime có; save/category filter/SQL acceptance còn. |
| T55 | Category UI | FrmCategory.cs | Khiêm | Category list/search runtime PASS; G23 Backend guard đã sửa; CRUD SQL/UI save chưa nghiệm thu. |
| T56 | Supplier UI | FrmSupplier.cs | Khiêm | Supplier list10 runtime; CRUD code, chưa save end-to-end. |
| T57 | Warehouse UI | FrmWarehouse.cs | Khiêm | Kho/vị trí list10 runtime; manager/location binding lỗi G15/G16; G12 Backend đã sửa; selector UI cần đồng bộ. |
| T58 | Location UI | FrmLocation.cs | Khiêm | Kho/vị trí list10 runtime; manager/location binding lỗi G15/G16; G12 Backend đã sửa; selector UI cần đồng bộ. |
| T59 | Import List UI | FrmImportList.cs | Khiêm | Receipt UI/editor có; runtime list/state buttons; Submit Backend G02 đã sửa, UI chưa test lại; labels G17 + reason G06 còn. |
| T60 | Import Detail UI | FrmImportDetail.cs | Khiêm | Receipt UI/editor có; runtime list/state buttons; Submit Backend G02 đã sửa, UI chưa test lại; labels G17 + reason G06 còn. |
| T61 | Export List UI | FrmExportList.cs | Khiêm | Receipt UI/editor có; runtime list/state buttons; Submit Backend G02 đã sửa, UI chưa test lại; labels G17 + reason G06 còn. |
| T62 | Export Detail UI | FrmExportDetail.cs | Khiêm | Receipt UI/editor có; runtime list/state buttons; Submit Backend G02 đã sửa, UI chưa test lại; labels G17 + reason G06 còn. |
| T63 | Inventory UI | FrmInventory.cs | Khiêm | Inventory quantity/unit/ngưỡng runtime, status formatting lỗi G14. |
| T64 | Inventory History UI | FrmInventoryHistory.cs | Khiêm | History runtime IMPORT/ref/actor đúng; adjustment/transfer metadata G08. |
| T65 | Stock Alert UI | FrmStockAlert.cs | Khiêm | Open alerts runtime đúng; resolve code, chưa UI write/history acceptance. |
| T66 | Inventory Report UI | FrmInventoryReport.cs | Khiêm | Report tabs runtime; cap/filter export G20/G21 và file acceptance còn. |
| T67 | Import/Export Report UI | FrmImportExportReport.cs | Khiêm | Report tabs runtime; cap/filter export G20/G21 và file acceptance còn. |
| T68 | User Management UI | FrmUserManagement.cs | Khiêm | List/filter STAFF7 runtime; create/deactivate code, edit/reset admin UI G22 thiếu. |
| T69 | Theme | Themes/AppColors.cs | Khiêm | Theme/validation/grid có source; visual ADMIN một size; full DPI/keyboard/error fields chưa đủ G27. |
| T70 | UI Validation/ErrorProvider | Forms | Khiêm | Theme/validation/grid có source; visual ADMIN một size; full DPI/keyboard/error fields chưa đủ G27. |
| T71 | DataGridView configuration | Forms | Khiêm | Theme/validation/grid có source; visual ADMIN một size; full DPI/keyboard/error fields chưa đủ G27. |
| T72 | Dashboard Charts | ChartControl.cs | Khiêm | UI/cards/chart có; không đạt4 Dashboard v1, chart title20/service10 G18. |
| T73 | Report Export Excel/PDF | Report UI/Service | Khiêm | Export code ClosedXML/QuestPDF có, chưa GUI file verification/filter consistency G21. |
| T74 | Unit Tests – Inventory | InventoryServiceTests.cs | Khang | 12 test files, suite121 PASS; thêm32 regression (G01/G02/G03/G04/G12/G20/G23). Tên file Excel là lịch sử; SQL rollback/race/constraint chưa chứng minh. |
| T75 | Unit Tests – Alert | StockAlertServiceTests.cs | Khang | 12 test files, suite121 PASS; thêm32 regression (G01/G02/G03/G04/G12/G20/G23). Tên file Excel là lịch sử; SQL rollback/race/constraint chưa chứng minh. |
| T76 | Unit Tests – Validator | ProductValidatorTests.cs | Khang | 12 test files, suite121 PASS; thêm32 regression (G01/G02/G03/G04/G12/G20/G23). Tên file Excel là lịch sử; SQL rollback/race/constraint chưa chứng minh. |
| T77 | Database Integration Test | Tests + SQL Server | BA + Khang | Chưa dedicated SQL Server integration harness; probe read-only không thay thế G27. |
| T78 | README technical setup | README.md | BA | README hiện có, connection/migration/seed authority updates G25 còn. |
| T79 | Architecture Documentation | Architecture.drawio | BA | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |
| T80 | ERD Documentation | ERD.drawio | BA | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |
| T81 | Class Diagram | ClassDiagram.drawio | Khang | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |
| T82 | SRS | SRS.docx | Cả nhóm theo layer | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |
| T83 | Use Case | UseCase.docx | Cả nhóm theo layer | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |
| T84 | Project Report | ProjectReport.docx | Cả nhóm theo layer | Official artifacts skeleton: DOCX2paragraph, drawio2rootcells; hoàn thiện đúng ownership G28. |

## 12. Manifest nguồn và trạng thái bàn giao

Hash dưới đây định danh nguồn tại lần tổng hợp ban đầu, không phải hash của code/report sau sửa và không xác nhận tài liệu đúng contract. Hai nguồn đã xóa được giữ nguyên trong ZIP được phê duyệt ở mục 9.

| Nguồn | Bytes | SHA256 |
|---|---|---|
| SystemContract_3.md (nguồn lịch sử trong ZIP) | 46218 | dee857fa07e213d6cb9f81aa0809498875101000dcea80dae4cbc5ef5e1f3698 |
| RULE.md | 37645 | 04b0ef49e84fc1ba22dd4fa4ebd766434487ed169d14e14ed85d89acf5c30c64 |
| Phan cong.docx | 782535 | 079a023519929c7f476ae16dcba54c9a2628b2c99b4eced8c7e64c5a16c6e555 |
| phan_quyen.docx | 59340 | 08000599938288c537bf225392c6fe6ef302c4d9e936ef26d185ce60c366d782 |
| BẢNG PHÂN CÔNG.xlsx | 16848 | 4a8622f6d17a1fb417de6f9446681af1819e7307cd141e0297d979f8f6550a9f |
| AUDIT_2026-10-08.md | 45050 | a390e23e4b2781c4897f8a82899c978d5997c57b0d6e8493b91d60c447796569 |
| ViecCanLam.md | 24224 | 74608a8d2c91aa8301ebccb9e28dfe43fc527020d228a44d0c9ebda07c85d9bc |
| UI_TEST_REPORT_2026-10-13.md | 17218 | a46269aa26fb20a2050b69b5533eef6dff378b51e977bd3c7ace93e43960a9b4 |
| UI_TEST_EVIDENCE_2026-10-13.json | 220473 | a84e80f304f9b78edfb5f027fa9ff7416ee88b26ad108ceb95fb5b8cc35e1254 |
| SeedDemoEnglish.sql | 10133 | 635d6bcc4eba30cd853634987b04b9fa5b6802c8232c13a145cfe055f4f4cfc6 |
| copilot-instructions.before-pull-20261013.md | 35495 | 3c06cb4e7b8b56982273080dab53e8442e3ecce3bb2d5f7602800ece38b4c4b9 |

Backup đã tạo và xác minh byte-for-byte trước khi xóa: `TaiLieuCu_TruocTongHop_20261013.zip`; chứa đúng2 file ViecCanLam.md và AUDIT_2026-10-08.md. Hai bản gốc đã được xóa theo phê duyệt cụ thể của người dùng; khôi phục bằng giải nén vào .agent khi cần.

Lượt tổng hợp ban đầu chỉ tạo tài liệu/probe và xóa đúng hai nguồn đã được phê duyệt, có ZIP khôi phục. Sau đó đã sửa 12 file Business, thêm IndependentBackendFixTests và cập nhật 2 fixture tests theo phạm vi Khang. Lượt cập nhật tài liệu này đồng bộ các mục và ma trận với kết quả 121 unit PASS, giữ nguyên code và dữ liệu DB; không apply migration/seed, không commit/push. Bản trước cập nhật đã được sao lưu ZIP trong .agent. Dedicated SQL acceptance, UI sau sửa và các quyết định B01–B10 vẫn còn; chưa chứng nhận dự án hoàn thiện.

## Đồng bộ ngưỡng sau quyết định ngày 13/10/2026

StockStatusCalculator và ProductValidator hiện dùng MaximumStock > ReorderPoint. Ngưỡng bằng nhau và 0/0 bị từ chối. Build 0 warning/error; 128 unit tests PASS (thêm 7 ca). Data CHECK giữ nguyên vì đã đúng. SQL/UI chưa kiểm thử lại. Các số 121 ở các bảng phía trên là kết quả của đợt sửa Backend trước; kết quả mới nhất là 128.

## Tiếp nhận SystemContract.md — 14/10/2026

Người dùng chọn SystemContract.md mới làm chuẩn. Quy tắc cấu hình: `0 < ReorderPoint < MaximumStock`. Khóa kép UserWarehouse và hai FK Restrict được chốt; baseline gồm 17 entity. Bản cũ SystemContract_3.md được người dùng yêu cầu xóa và đã lưu trong ContractDocs_TruocChuyenBanMoi_20261014.zip. Các số 121/128 test và probe phía trên là bằng chứng của những đợt code trước, không chứng minh rule ReorderPoint >0 mới. Việc tiếp theo: Khang sửa validation/tests; Bảo Anh cập nhật mapping và migration mới, rà dữ liệu zero trước apply; Khiêm kiểm tra input/error UI. Lượt này chỉ đồng bộ tài liệu, không thay code/DB.

## Đợt hoàn thiện theo ba tầng — 14/10/2026

Báo cáo mới nhất: Documentation/Architecture/CompletionByLayer_2026-10-14.md. Data 7282f26 thêm migration strict-positive và factory env; Business fcbf152 validation/tests dương + display DTO/lookup manager; UI ea86338 trạng thái/bindings/manager/location filter/user edit-reset. Build0 warning/error; 136 tests PASS; EF model/script và UI control smoke PASS. Các G14/G15/G16/G17/G22 và selector G12 đã sửa code, UI runtime acceptance còn; G11 model/migration/Business đã đồng bộ nhưng migration chưa apply. Các số 121/128 và nhận xét lỗi chưa sửa phía trên là snapshot của đợt trước; báo cáo theo ba tầng là nguồn trạng thái hiện tại. SQL cũ giữ nguyên; nghiệp vụ chưa chốt ghi trong báo cáo, chưa push.


</details>

<details>
<summary>Snapshot lịch sử: .agent/UI_TEST_REPORT_2026-10-13.md — không phải trạng thái hiện hành</summary>

# Báo cáo kiểm thử UI — SmartInventoryManager

Ngày kiểm tra: 13/10/2026, giờ Việt Nam. Phạm vi: đánh giá UI toàn ứng dụng; không sửa code UI/Data/Business. Chủ trì phiên làm việc: Khang — Backend + Warehouse Operations. Các lỗi liên tầng được phân công bên dưới, không tự sửa ngoài phạm vi.

## 1. Môi trường và mức độ xác minh

- Visual Studio đã mở `SmartInventoryManager.slnx`, startup project `SmartInventoryManager.UI`, Debug / Any CPU. F5 khởi chạy thành công; cửa sổ Visual Studio có trạng thái Running và nút Stop Debugging. Người dùng tự đăng nhập ADMIN.
- SQL Server: `localhost`, database `SmartInventoryManagerDB`, Windows Authentication. Migration hiện hành `20261012084010_InitialCreate`, EF Core 10.0.12. Probe trước kiểm thử xác nhận 17 entity truy vấn thành công, không có pending migration hoặc pending model changes.
- Snapshot Git khi rà soát: nhánh Backend, HEAD `e53a254`. Working tree có hai thay đổi sẵn có ở copilot-instructions.md và launchSettings.json; được giữ nguyên.
- Dữ liệu: Roles có 3 dòng; 16 bảng nghiệp vụ khác có 10 dòng/bảng. Tổng tồn đầu phiên 105, 10 dòng tồn, 10 lịch sử nhập, 10 cảnh báo mở; 10 phiếu nhập COMPLETED và 10 phiếu xuất DRAFT.
- Kiểm tra trực tiếp các trang chính, các tab Kho/Vị trí, Tồn kho/Nhập xuất trong Báo cáo và Thông báo; có thử tìm kiếm, lọc, mở form sản phẩm, gửi duyệt xuất, đánh dấu thông báo đã đọc.
- `dotnet test SmartInventoryManager.Tests/SmartInventoryManager.Tests.csproj --no-build --no-restore`: 89 đạt, 0 lỗi, 0 bỏ qua. Đây là test đã có, không phải 89 ca nghiệm thu GUI hoặc SQL concurrency.
- Công cụ không điều khiển ổn định cửa sổ modal: form sản phẩm đã được đọc và người dùng hỗ trợ đóng. Các thao tác lưu CRUD, dialog xuất file, điều chỉnh/chuyển kho và hoàn tất phiếu chưa được chạy end-to-end. Không biến giới hạn này thành kết luận tính năng không tồn tại.
- Các trạng thái: **Đã có** = đã có đường triển khai, kèm ghi rõ phần runtime đã thử; **Chưa hoàn thành** = có một phần nhưng còn lỗi/gap cụ thể; **Chưa có** = chưa tìm thấy triển khai trong đường chạy hiện tại. “Chưa kiểm thử” là mức bằng chứng riêng.

## 2. Ma trận tính năng

| Nhóm / tính năng | Trạng thái | Bằng chứng và giới hạn |
|---|---|---|
| Đăng nhập và mở phiên ADMIN | Đã có | Người dùng đăng nhập trong phiên Visual Studio Debug; ứng dụng tải dữ liệu DB. |
| Dashboard: tồn hiện tại, cảnh báo, hoạt động, biểu đồ | Đã có | Tất cả kho: 10 dòng tồn, 105 đơn vị, 10 cảnh báo, 0 hết hàng. Có biểu đồ biến động và danh sách nhập hoàn tất. |
| Dashboard: lọc kho và làm mới | Đã có | Chọn Warehouse 08 và Làm mới: 1 dòng, 13 đơn vị, 1 cảnh báo. Bộ chọn chưa tự áp dụng cho tới khi Làm mới. |
| Dashboard: giá trị tồn | Chưa hoàn thành | UI hiện dấu — và “Chưa thống nhất định giá”; chưa có công thức/giá trị được triển khai. |
| Sản phẩm: danh sách, tìm kiếm, xem/sửa form | Đã có | 10 sản phẩm; tìm DEMO-PRD-03 trả đúng 1 dòng. Form sửa tải mã, tên, danh mục Packaging, giá 16/20 và ngưỡng 20/100. Chưa lưu thay đổi. |
| Sản phẩm: tạo, cập nhật, đổi trạng thái, phân trang | Đã có, chưa nghiệm thu lưu | Form và ProductManagementService có đường gọi thật; chưa thử ghi hoặc dữ liệu lớn hơn một trang. Checkbox hoạt động trên form bị khóa; đổi trạng thái đi qua thao tác riêng trên danh sách. |
| Sản phẩm: lọc theo danh mục | Chưa hoàn thành | Contract tìm kiếm có CategoryId nhưng danh sách hiện tại không có bộ chọn danh mục. |
| Danh mục: tải, tìm kiếm, trạng thái | Đã có | 10 dòng; nhập Packaging và Làm mới trả 1 dòng. Các kết luận audit cũ nói tìm kiếm/refresh chưa nối không còn đúng. |
| Danh mục: thêm/sửa/ngừng hoạt động | Đã có, chưa nghiệm thu lưu | Có nút, form và service; chưa ghi dữ liệu hoặc thử chặn ngừng danh mục chứa sản phẩm active. |
| Nhà cung cấp: danh sách và CRUD | Đã có | Danh sách 10 dòng, mã/tên/contact/trạng thái tải được. Nút thêm/sửa/đổi trạng thái đã nối service theo source; chưa thử lưu. |
| Kho: danh sách, cấu hình, manager | Chưa hoàn thành | 10 kho tải được; cột Người quản lý trống dù DB có ManagerId và user. Thêm/sửa/đổi trạng thái có code nhưng chưa thử lưu. |
| Vị trí kho: danh sách và CRUD | Chưa hoàn thành | 10 vị trí tải được; cột Kho trống, không có bộ lọc chọn kho trong tab hiện tại. Thêm/sửa/đổi trạng thái có code nhưng chưa thử lưu. |
| Phiếu nhập: danh sách và khóa thao tác phiếu hoàn tất | Đã có | 10 phiếu hoàn tất; creator, supplier, warehouse, tổng tiền đúng. Sửa/gửi duyệt/duyệt/hoàn tất/hủy/xóa bị khóa với phiếu COMPLETED. |
| Phiếu nhập: tạo/sửa/gửi/duyệt/từ chối/gửi lại/hoàn tất | Chưa hoàn thành | Có Business và UI wiring. Chưa chạy nguyên vòng đời; source Submit thiếu Include Details giống lỗi xuất đã tái hiện. Không khẳng định nhập đã lỗi runtime vì chưa thử. |
| Phiếu xuất: danh sách và nút theo trạng thái | Đã có | 10 phiếu nháp; sửa/gửi/hủy/xóa nháp mở; duyệt/từ chối/hoàn tất khóa ở DRAFT. |
| Phiếu xuất: gửi duyệt | Chưa hoàn thành — lỗi đã tái hiện | Nhấn Gửi duyệt DEMO-EXP-01 báo lỗi chung. DB vẫn DRAFT, có đúng 1 ExportDetail. Luồng không tiến tới duyệt/hoàn tất được trong ca này. |
| Hủy phiếu và lý do/audit vòng đời | Chưa hoàn thành | Có hủy phiếu trước hoàn tất, nhưng chưa có cột CancelReason và actor/time riêng cho từng bước. Chưa kiểm thử hủy hoặc xóa trong phiên. |
| Tồn kho: dữ liệu per-product/per-warehouse | Đã có | Warehouse 01: Copy Paper A4, tồn 6, reorder 20, maximum 100, thời gian cập nhật đúng. |
| Tồn kho: hiển thị trạng thái | Chưa hoàn thành — lỗi đã tái hiện | Cùng sản phẩm có trạng thái “Chưa xác định” ở Tồn kho, trong khi Cảnh báo/Báo cáo là “Sắp hết”. |
| Điều chỉnh và chuyển kho | Chưa hoàn thành ở mức truy vết | Có UI action và Business transaction/validation. Lịch sử adjustment chỉ có abs(delta), thiếu before/after/direction; transfer thiếu reference chung. Chưa kiểm thử ghi hoặc concurrency. |
| Lịch sử tồn kho | Đã có | Warehouse 01 có 1 IMPORT, số lượng 6, reference IMPORT_RECEIPT/1, actor James Anderson, thời gian và ghi chú. Có filter kho/ngày/loại theo source. |
| Cảnh báo tồn: danh sách, lọc, đóng | Đã có | Warehouse 01 hiển thị LOW_STOCK/Sắp hết, tồn 6, ngưỡng 20; nút đóng có thật. Chưa đóng cảnh báo hay kiểm tra toàn bộ lifecycle/biên threshold qua UI. |
| Lịch sử cảnh báo đã giải quyết | Chưa có trên UI hiện tại | Đường trang chính dùng GetOpenAsync, không có trang/filter tra resolved history. |
| Báo cáo tồn hiện tại | Đã có | Warehouse 01: 1 dòng, tổng tồn 6, cảnh báo mở 1, trạng thái Sắp hết. |
| Báo cáo nhập/xuất hoàn tất | Đã có | Warehouse 01: nhập 72, xuất 0, 1 phiếu nhập; lọc Xuất kho trả rỗng đúng vì tất cả phiếu xuất đang nháp. Tổng summary vẫn ghi rõ số liệu kho/ngày, không phải tổng riêng sau filter loại. |
| Xuất Excel/PDF | Đã có code, chưa hoàn tất nghiệm thu | Có nút, SaveFileDialog và ReportExportService. Chưa tạo/mở file qua UI. Source export lấy lại toàn bộ report theo kho/ngày, không áp dụng search/type đang hiển thị; cần chốt hành vi và test. |
| Báo cáo tồn theo thời điểm, giá trị tồn, in phiếu chi tiết | Chưa có trong đường UI đã rà | Báo cáo tồn lấy dữ liệu hiện tại; chưa có định giá/theo thời điểm hoặc luồng in phiếu riêng đầy đủ các dòng hàng. |
| Người dùng: danh sách, tìm/lọc, tạo, vô hiệu hóa | Đã có | 10 user; lọc STAFF trả 7. Code nối tạo và Deactivate; tự vô hiệu hóa admin đang đăng nhập bị loại khỏi action. Chưa tạo/vô hiệu hóa trong phiên. |
| Quản trị user: sửa tài khoản/role/reset password | Chưa hoàn thành UI | Business có UpdateUserAsync và ResetPasswordAsync; trang quản trị hiện có tạo, chi tiết chỉ đọc, vô hiệu hóa, chưa có đường gọi các chức năng trên. |
| Phân công STAFF vào kho trên UI | Chưa có | Có bảng UserWarehouses và authorization backend; chưa tìm thấy form/service quản trị assignments trong luồng UI hiện tại. |
| Thông báo: danh sách cá nhân, đọc, đọc tất cả | Đã có | Admin thấy 1 thông báo. Nhấn Đánh dấu đã đọc đổi trạng thái đúng; SQL xác nhận IsRead=True. Có nút đọc tất cả và timer/event refresh theo source. |
| Tự tạo notification lưu DB khi có stock alert | Chưa hoàn thành | Có cảnh báo và event/UI refresh; chưa tìm thấy đường orchestration ghi Notification cho alert mới. Dữ liệu thông báo demo không chứng minh tính năng tự tạo. |
| Cài đặt tài khoản, đổi mật khẩu, cỡ chữ/menu/page size | Đã có code, chưa kiểm thử UI lưu | Form và service wiring có thật; phiên này không đổi mật khẩu/cấu hình cá nhân. |
| Ảnh đại diện, remember login, tự phục hồi mật khẩu | Chưa có hoàn chỉnh | Nút ảnh bị disable; Remember chưa có persistence; quên mật khẩu chỉ hướng dẫn liên hệ quản trị. Reset backend có nhưng chưa có UI quản trị tương ứng. |
| Nhật ký hệ thống tự động và UI tra cứu | Chưa có | SystemLogs hiện có entity/seed nhưng không có service ghi log nghiệp vụ hoặc menu tra cứu trong đường chạy hiện tại. |
| Analytics chuyên sâu, Top Products, ABC, Dead Stock, reorder recommendation | Chưa có trong source/đường UI hiện tại | Không có implementation analytics chuyên sâu; ABC/Dead Stock/Reorder là bonus theo tài liệu hiện hành, không đánh đồng với thiếu chức năng chính. |
| Kiểm kê theo phiên độc lập | Chưa có | Điều chỉnh delta không phải phiên kiểm kê có danh sách đếm, đối chiếu, duyệt và lịch sử phiên. |
| Minh chứng ADO.NET cho tính năng thực | Chưa có | StoredProcedureExecutor, ReportDataProvider, DashboardQueries còn lớp rỗng. Không cần tạo repository rỗng thành framework chỉ để đủ danh sách. |

## 3. Bốn lỗi đã tái hiện và hướng xử lý

### UI-01 — Gửi duyệt phiếu xuất thất bại (ưu tiên P1, Khang)

1. Đăng nhập ADMIN, mở Xuất kho.
2. Chọn DEMO-EXP-01, nhấn Gửi duyệt.
3. UI báo “Chưa thực hiện được thao tác…”, phiếu không chuyển trạng thái.

SQL xác nhận phiếu DRAFT và có 1 dòng chi tiết; tổng tồn vẫn 105. Nguyên nhân có bằng chứng trong source: ExportService.SubmitAsync lấy receipt bằng SingleOrDefaultAsync không Include Details, sau đó ValidateStoredReceiptAsync kiểm tra receipt.Details.Count. EF không bật lazy loading và UiServices clear tracking sau từng operation, nên danh sách navigation trống khi đọc lại. ImportService.SubmitAsync có mẫu tương tự, cần ca kiểm thử riêng. Lỗi runtime đã xác nhận; chưa thu được stack trace nguyên nhân từ Output nên chẩn đoán navigation là kết luận từ source, không trích stack trace.

Nguồn: `SmartInventoryManager.Business/Services/ExportService.cs:175`, `:182`, `:436`; `SmartInventoryManager.Business/Services/ImportService.cs:200`; `SmartInventoryManager.UI/Components/UiServices.cs`.

### UI-02 — Tồn kho hiển thị “Chưa xác định” (P1, Khiêm; phối hợp Khang)

Warehouse 01/Product 01: quantity 6, reorder 20. Tồn kho hiển thị Chưa xác định; Báo cáo tồn và Cảnh báo hiển thị Sắp hết. DataListPage.CellFormatting đổi enum thành chuỗi tiếng Việt rồi tiếp tục đưa chuỗi đó vào LiveUi.StatusText, hàm chỉ hiểu mã trạng thái; kết quả bị thay lần hai.

Nguồn: `SmartInventoryManager.UI/Components/DataListPage.cs:71–73`; `SmartInventoryManager.Business/DTOs/InventoryDto.cs`. Cần chỉ thực hiện một đường chuyển đổi trạng thái và test cả enum/string DTO.

### UI-03 — Cột Người quản lý kho trống (P2, Khiêm + Khang cho contract)

Danh sách 10 kho đều trống cột manager. SQL xác nhận DEMO-WH-01 có ManagerId=2, FullName=James Anderson. UI bind ManagerName nhưng WarehouseDto chỉ có ManagerId. Cần dùng contract hiển thị manager đã thống nhất hoặc mapping tên phù hợp; không sửa schema chỉ để giải quyết binding.

Nguồn: `SmartInventoryManager.UI/Forms/Warehouses/WarehouseUi.cs:227`; `SmartInventoryManager.Business/DTOs/MasterDataDtos.cs:25`.

### UI-04 — Cột Kho của vị trí trống (P2, Khiêm + Khang cho contract)

Tab Vị trí tải 10 dòng A-01 nhưng không thể nhìn ra dòng thuộc kho nào. SQL xác nhận vị trí đã liên kết Warehouse 01. UI bind WarehouseName nhưng LocationDto chỉ có WarehouseId. Tab cũng không có bộ chọn kho, khiến nhiều dòng cùng mã/tên khó phân biệt.

Nguồn: `SmartInventoryManager.UI/Forms/Warehouses/WarehouseUi.cs:216`; `SmartInventoryManager.Business/DTOs/MasterDataDtos.cs`.

## 4. Các gap cần theo dõi, không phải lỗi GUI đã nghiệm thu

- Cập nhật quy tắc ngày 13/10/2026 theo người dùng: MaximumStock > ReorderPoint là chuẩn, equality/0/0 bị từ chối. CHECK database hiện đúng; Khang cần đồng bộ Business validation/tests. Kết quả UI trong báo cáo là lịch sử, chưa test lại quy tắc mới.
- ReportService Take(5000) và các danh sách/lookup giới hạn 500/200 còn cần phân trang/cảnh báo giới hạn và tổng toàn scope. Chưa stress test khối lượng lớn.
- Nhật ký adjustment/transfer, CancelReason/audit vòng đời và SQL concurrency/rollback cần phối hợp Business/Data; không kết luận đúng hoàn toàn từ phiên đọc UI này.
- Tests hiện có dùng EF InMemory ở nhiều workflow; thiếu nghiệm thu SQL Server cho tranh chấp đồng thời, idempotency và rollback trên tất cả đường stock mutation.
- Khởi chạy Debug ở Folder View đã từng lỗi CoreCLR; mở solution chuẩn khắc phục được trong phiên này. Cấu hình design-time factory vẫn .\SQLEXPRESS, trong khi runtime localhost; migration cần connection explicit hoặc chỉnh đồng bộ theo scope được duyệt.
- Không lấy audit ngày 08/10 làm trạng thái hiện tại: category search/refresh, product category selector, dashboard binding, UserWarehouse và filtered open-alert index đã có cập nhật.

## 5. Dữ liệu sau test và kiểm tra chưa chạy

Thay đổi thành công duy nhất do kiểm thử UI trong phiên: thông báo “Demo data is ready” của admin chuyển sang đã đọc. Gửi duyệt thất bại không đổi phiếu; DEMO-EXP-01 vẫn DRAFT và có 1 dòng detail. Tổng tồn 105, 10 Inventory rows, 10 InventoryTransactions; không tạo/xóa master data, không post stock, không đổi mật khẩu hoặc assignments.

Chưa chạy: tạo/sửa/xóa end-to-end tất cả master data; toàn bộ receipt lifecycle; insufficient-stock UI; adjustment/transfer commit; đóng/resolved/reopen alerts; export Excel/PDF và kiểm tra file; tài khoản MANAGER/STAFF và chọn kho; reconnect/mất mạng; concurrent operations; 100/125/150/200% DPI, Narrator, toàn bộ keyboard navigation. Không đánh dấu các phần này “đã hoàn thành nghiệm thu”.

Ưu tiên lượt sửa/kiểm tra tiếp: (1) Submit Details ở Backend; (2) hiển thị stock status; (3) manager/warehouse binding; (4) UI quản trị tài khoản/assignment và export filters; (5) kiểm thử SQL toàn vòng đời phiếu rồi UI role/DPI/export. Không có sửa code nghiệp vụ, commit hoặc push trong phiên kiểm thử này.

Bằng chứng accessibility theo từng ca được lưu ở `UI_TEST_EVIDENCE_2026-10-13.json` cùng thư mục. Báo cáo và bằng chứng là local-only trong .agent.

Cập nhật tài liệu 14/10/2026: SystemContract.md mới yêu cầu ReorderPoint >0 (ngoài MaximumStock > ReorderPoint). Code/Data vẫn cho phép ReorderPoint=0; cần Backend validation/tests và migration mới. Báo cáo UI 13/10 là lịch sử, chưa nghiệm thu quy tắc mới.


</details>

<details>
<summary>Snapshot lịch sử: .agent/FinalProgress_20261014.md — không phải trạng thái hiện hành</summary>

## Cập nhật hiện hành ngày 14/10/2026 — sau nghiệm thu

Phần này có ưu tiên so với các snapshot lịch sử phía dưới. Chuẩn là `.agent/SystemContract.md` hiện hành; người dùng đã cho phép hoàn thiện cả ba tầng, commit theo từng tầng và dùng `localhost / SmartInventoryManagerDB` làm DB TEST. Server production riêng chưa được cung cấp. SQL cũ được giữ nguyên; không drop/reset database và không push đợt này.

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

Các thay đổi `.github/copilot-instructions.md` và connection launchSettings có trước được giữ local ngoài commit code. README và checklist đã cập nhật nguồn schema/migration/bootstrap hiện hành. Dự án đã tiến thêm ở CORE, chưa chứng nhận hoàn thiện toàn bộ phạm vi hợp đồng.

---

## Snapshot lịch sử — đọc cùng cập nhật hiện hành ở trên


</details>

<details>
<summary>Snapshot lịch sử: Documentation/Architecture/CompletionByLayer_2026-10-14.md — không phải trạng thái hiện hành</summary>

## Tiến độ hiện hành sau triển khai TeamChot — 14/10/2026

Phần này thay các mục trì hoãn audit/transfer/analytics/UI tương ứng trong snapshot bên dưới. Đã hoàn thiện theo quyết định mới trên ba tầng; không suy rằng tất cả yêu cầu lịch sử đều đã nghiệm thu. Xem Documentation/Architecture/TeamDecisionCompletion_2026-10-14.md và CurrentArchitecture.md cho code, quyết định,152unit/4SQL PASS,18bảng schema và giới hạn nghiệm thu. SQL cũ giữ nguyên, fixture TEST đã dọn, production chưa cung cấp, chưa push đợt này.

Còn chờ: WAC và tồn đầu kỳ/định giá chuyển-điều chỉnh; retention/archive; production và peer review; Designer/DPI/máy in/dataset lớn. Không còn câu hỏi formula ABC/90ngày/giai đoạn đầu bị bỏ ngỏ: người dùng đã duyệt chi tiết trong TeamChot.

---
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


</details>

<details>
<summary>Snapshot lịch sử: Documentation/Architecture/TeamDecisionCompletion_2026-10-14.md — không phải trạng thái hiện hành</summary>

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


</details>
