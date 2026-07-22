# Admin thiết lập trạng thái phiếu mượn và kịch bản demo

## Mục tiêu

Cho phép Admin sửa các phiếu có trạng thái header bị lệch so với chi tiết sách, đồng thời cung cấp một cách an toàn để tạo dữ liệu demo cho các trạng thái mượn–trả. Trạng thái không được sửa tự do theo kiểu đổi một chuỗi, vì nó liên quan đến ngày trả, tồn kho, tiền phạt và tiền đền sách.

Phạm vi chỉ áp dụng cho `PhieuMuon`, `ChiTietPhieuMuon`, tồn kho và giao diện WinForms hiện có. Không thêm chatbot, không thay đổi nghiệp vụ người dùng thường và không thay đổi schema database trong phiên bản này.

## Trạng thái hợp lệ

- `Đang mượn`: có ít nhất một chi tiết `NgayTra IS NULL` và chưa có chi tiết đã trả.
- `Đã trả một phần`: có cả chi tiết đã trả và chi tiết chưa trả.
- `Đã trả`: mọi chi tiết đều có `NgayTra IS NOT NULL`.
- `Quá hạn`: trạng thái hiển thị suy ra từ `HanTra < ngày hiện tại` khi phiếu còn sách chưa trả; không ghi `Quá hạn` vào database.

Nếu phiếu không có chi tiết sách, không được đồng bộ trạng thái và phải báo lỗi dữ liệu.

## Quyền và giao diện

Trong menu thao tác của `FormPhieuMuon`, thêm mục `Thiết lập trạng thái`. Mục này chỉ bật cho Admin đang hoạt động; nhân viên thường chỉ thấy mục bị vô hiệu hóa hoặc không thấy mục tùy theo quy tắc menu hiện tại.

Dialog hiển thị:

- Mã phiếu, độc giả, ngày mượn, hạn trả.
- Trạng thái đang lưu và trạng thái suy ra từ chi tiết.
- Số dòng chưa trả, số dòng đã trả, tổng phạt/đền và số đã thu.
- Nút `Đồng bộ trạng thái`.
- Nút `Tạo kịch bản demo`.
- `Đóng`/`Esc` để thoát.

Dialog không cho nhập trực tiếp một giá trị trạng thái tùy ý. Các nút phải có `AccessibleName`, `AcceptButton`/`CancelButton`, thứ tự Tab và focus rõ ràng.

## Đồng bộ phiếu thực tế

`Đồng bộ trạng thái` chỉ cập nhật `PhieuMuon.TrangThai` theo dữ liệu chi tiết trong một transaction:

1. Khóa header và các dòng chi tiết bằng `UPDLOCK, HOLDLOCK`.
2. Đọc lại số dòng đã trả/chưa trả từ database, không dùng số hiển thị trên UI.
3. Tính trạng thái hợp lệ theo quy tắc ở trên.
4. Nếu trạng thái hiện tại đã đúng, trả kết quả không thay đổi.
5. Nếu trạng thái lệch, cập nhật header và commit.

Thao tác này không sửa `NgayTra`, `SoLuongMat`, `TienPhat`, `TienDenMatSach` hoặc `Sach.SoLuong`. Vì vậy nó chỉ sửa metadata bị lệch và không làm phát sinh giao dịch kho hay tiền.

Nếu phát hiện dữ liệu mâu thuẫn nguy hiểm (chi tiết đã trả nhưng trạng thái header không thuộc tập hợp hợp lệ, thiếu sách, hoặc có giá trị null bất thường), transaction rollback và hiển thị lý do để Admin xử lý bằng luồng trả sách; không tự động đoán ngày trả hay cộng kho.

## Kịch bản demo

`Tạo kịch bản demo` không đổi trạng thái tùy ý của một phiếu đang sử dụng. Nó mở một luồng tạo phiếu mới dùng cùng các kiểm tra nghiệp vụ hiện có, sau đó cho chọn kịch bản:

- `Đang mượn`: tạo phiếu hợp lệ với hạn trả hôm nay hoặc tương lai.
- `Quá hạn`: tạo phiếu hợp lệ với hạn trả trước hôm nay; trạng thái quá hạn được UI suy ra.
- `Đã trả một phần`: tạo phiếu có ít nhất hai dòng sách, sau đó xử lý trả một dòng bằng transaction trả sách hiện có.
- `Đã trả`: tạo phiếu rồi xử lý toàn bộ dòng bằng transaction trả sách hiện có.
- `Có sách mất`: trong bước trả, nhập số lượng mất; tiền đền lấy từ `Sach.GiaTien` và chỉ số lượng không mất được cộng lại kho.

Kịch bản demo phải chọn độc giả hợp lệ và sách đủ tồn kho. Nếu bất kỳ bước nào thất bại, toàn bộ kịch bản rollback. UI hiển thị mã phiếu mới sau khi thành công để dễ tìm và xóa thủ công theo quy trình hiện có nếu cần.

Không thêm cột `IsDemo` hoặc `TrangThaiOverride`; dữ liệu demo vẫn tuân theo schema và các invariant thực tế, tránh trường hợp bản demo làm sai số tồn kho hoặc tổng tiền.

## DataAccess và an toàn đồng thời

Thêm hai operation ở `DataAccess`:

- `SyncPhieuMuonStatus(int maPM, int actorMaNV, out string? reason)`: kiểm tra Admin, khóa dữ liệu, tính và cập nhật trạng thái header.
- `CreateDemoLoanScenario(DemoLoanScenarioRequest request, int actorMaNV, out int maPM, out string? reason)`: kiểm tra Admin, tạo phiếu và xử lý kịch bản trong một transaction.

Không nhận SQL hoặc trạng thái từ input tự do. Enum kịch bản được whitelist trong code. Các operation dùng tham số hóa, transaction và khóa dòng giống các operation mượn/trả hiện có.

## Kiểm thử nghiệm thu

### Đồng bộ thực tế

1. Header `Đang mượn`, tất cả chi tiết chưa trả → không thay đổi dữ liệu.
2. Header sai, có cả dòng đã trả và chưa trả → sửa thành `Đã trả một phần`.
3. Header sai, mọi dòng đã trả → sửa thành `Đã trả`.
4. Hạn trả trước hôm nay và còn sách chưa trả → UI hiển thị `Quá hạn`, database vẫn lưu `Đang mượn` hoặc `Đã trả một phần`.
5. Nhân viên thường gọi operation → bị từ chối, không thay đổi dữ liệu.
6. Hai máy đồng bộ cùng phiếu → chỉ một transaction cập nhật; kết quả cuối nhất quán.
7. Dữ liệu thiếu chi tiết hoặc lỗi SQL → rollback, không đổi header.

### Kịch bản demo

1. Tạo demo `Đang mượn` → stock giảm đúng, có chi tiết chưa trả.
2. Tạo demo `Quá hạn` → hạn trả trước hôm nay, UI lọc được trong mục quá hạn.
3. Tạo demo `Đã trả một phần` → một dòng có ngày trả, dòng khác chưa trả, stock chỉ hoàn lại dòng đã trả.
4. Tạo demo `Đã trả` → tất cả dòng có ngày trả, stock hoàn lại đúng số lượng không mất.
5. Tạo demo có sách mất → `TienDenMatSach = GiaTien × SoLuongMat`, không hoàn kho phần mất.
6. Thiếu tồn kho hoặc độc giả hết hạn → không tạo phiếu và không để lại dữ liệu dở dang.
7. Bấm `Esc`, `Đóng`, `Enter` và điều hướng bằng Tab → dialog hoạt động đúng, không cắt footer.

## Tiêu chí hoàn thành

- Chỉ Admin đang hoạt động thấy và dùng được chức năng.
- Không có đường dẫn nào cho phép đổi trạng thái mà làm lệch chi tiết, tồn kho hoặc tiền.
- Trạng thái hiển thị, bộ lọc và dashboard vẫn dùng cùng quy tắc hiện tại.
- Kịch bản demo tạo dữ liệu hợp lệ bằng các transaction nghiệp vụ thật.
- Build Release không có lỗi; các test hồi quy mượn/trả, thu tiền và xuất PDF vẫn chạy được.
