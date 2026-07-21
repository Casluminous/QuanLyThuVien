# QuanLyThuVien Stability Hardening

## Mục tiêu

Sửa các lỗi đã xác minh trong schema/migration, luồng quản trị và mượn–trả, tài nguyên WinForms và trải nghiệm dialog; không tự động thay đổi database khi ứng dụng khởi động và không thay đổi nghiệp vụ ngoài các quy tắc được nêu dưới đây.

## Phạm vi

### 1. Database contract và migration thủ công

- Dùng `TacGia.QuocTich` làm tên cột chuẩn trong database, model, DataAccess và FormTacGia.
- Sửa migration để dùng hàm metadata hợp lệ của SQL Server.
- Migration có bước kiểm tra trước khi tạo unique index username; nếu phát hiện username trùng, dừng với thông báo rõ ràng thay vì tự chọn bản ghi.
- Bảo đảm migration không ghi đè trạng thái phiếu hợp lệ, đặc biệt `Đã trả một phần`; chỉ sửa các giá trị legacy đã xác định hoặc dừng để người vận hành xử lý.
- Thêm/đồng bộ check constraint cho tồn kho, số lượng mượn, giá sách và tiền phạt; preflight phát hiện dữ liệu vi phạm rồi dừng rõ ràng trước khi thêm constraint.
- Bỏ seed tài khoản `admin/admin` và `nhanvien1/admin`; tài khoản Admin đầu tiên chỉ được tạo qua FormFirstRun.
- Không gọi migration tự động từ `Program`; người vận hành chạy script thủ công.

### 2. Bảo vệ Admin

- Tạo thao tác DataAccess dạng transaction cho xóa, vô hiệu hóa hoặc hạ quyền nhân viên.
- Khóa các bản ghi Admin liên quan trong transaction, kiểm tra sau thay đổi vẫn còn ít nhất một Admin active.
- Áp dụng cùng quy tắc khi thao tác trên chính tài khoản hiện tại hoặc tài khoản khác, tránh race condition giữa hai phiên.

### 3. Tiền phạt trả sách

- Mức phạt được tính theo từng cuốn: `số ngày quá hạn × mức phạt/ngày/cuốn × số lượng`.
- Màn hình trả sách hiển thị mức phạt gợi ý/ngày/cuốn cho các dòng còn chưa trả; người dùng được chỉnh mức phạt/ngày/cuốn trước khi xác nhận và thấy tổng tiền dự kiến.
- DataAccess nhận và ghi phạt theo các dòng sách được trả, không chia lại một tổng tiền theo số dòng và không tính lại các dòng đã trả.
- Khi trả từng phần, chỉ các dòng được chọn mới được cập nhật `NgayTra`, tồn kho và `TienPhat`; tổng phạt không bị tính trùng ở lần trả sau.
- Admin vẫn được sửa tổng phạt của phiếu đã trả hết; khi phân bổ lại, phân bổ theo số lượng cuốn và bảo toàn tổng tiền.

### 4. Tài nguyên và UI ổn định

- Dispose các control/card cũ trước khi reload danh sách; dispose image thumbnail khi card bị thay thế hoặc hủy.
- Không tạo `Region`/`GraphicsPath` lâu sống ngoài ownership rõ ràng; cập nhật region ở thời điểm phù hợp và giải phóng region cũ.
- Clone ảnh banner đăng nhập trước khi đóng stream.
- Dùng `ClientSize` và layout neo đáy cho dialog cắt ảnh để nút luôn nhìn thấy.
- Dùng placeholder thật cho ô mật khẩu sửa nhân viên, không gán chuỗi hướng dẫn vào giá trị mật khẩu.
- Giới hạn năm xuất bản theo năm hiện tại thay vì mốc cố định 2030.
- Giữ thông báo người dùng ngắn gọn, log chi tiết nội bộ và không nuốt lỗi database im lặng ở các màn hình chính.

## Ngoài phạm vi

- Không tự động chạy migration hoặc tự ý sửa dữ liệu production.
- Không đổi schema ngoài các cột/index/constraint cần để bảo vệ các invariant nêu trên.
- Không thêm thư viện UI hoặc thay đổi theme Library Teal đã triển khai.
- Không thay đổi quyền nghiệp vụ ngoài invariant Admin và quy tắc tiền phạt.

## Kiểm thử và nghiệm thu

1. `dotnet build QuanLyThuVien.slnx --configuration Release -p:TreatWarningsAsErrors=true` đạt 0 warning, 0 error.
2. Kiểm tra SQL migration trên SQL Server local bằng truy vấn đọc/transaction an toàn; xác nhận không còn hàm không hợp lệ, không làm mất `Đã trả một phần`, và unique username được bảo vệ.
3. Smoke test dialog tại `1280×780`, `1000×600`, `900×600`, bao gồm cắt ảnh, sửa phiếu mượn, trả một phần và sửa tiền phạt.
4. Kiểm tra trả một phần nhiều lần: mỗi dòng/số lượng chỉ được ghi phạt và cộng tồn kho một lần.
5. Kiểm tra Admin: không thể làm hệ thống mất Admin active kể cả khi thao tác trên tài khoản khác hoặc có hai phiên đồng thời.
6. Kiểm tra reload danh sách nhiều lần không tăng bất thường số control/image/GDI handle.

## Rủi ro và xử lý

- Nếu database cũ có username trùng, migration sẽ dừng có chủ đích; cần người vận hành xử lý dữ liệu trùng rồi chạy lại.
- Database hiện tại có thể vẫn dùng `QuocTia`; phải chạy migration thủ công trước khi dùng build đã đồng bộ `QuocTich`.
- Các cảnh báo encoding BOM trong tài liệu không ảnh hưởng runtime, nhưng sẽ được ghi nhận riêng nếu không cần thay đổi nội dung.
