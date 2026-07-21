# Header dùng chung và luồng trả sách có sách mất

## Bối cảnh

Các màn quản lý dạng danh sách đang đặt tiêu đề và nút thêm trên hai hàng riêng, làm tốn chiều cao và thiếu nhất quán. Luồng trả sách hiện cho phép chọn nguyên một dòng chi tiết để trả, tính phạt theo số ngày quá hạn và cộng toàn bộ số lượng của dòng trở lại kho. Dữ liệu đã có `Sach.GiaTien`, nhưng chưa lưu được số lượng sách mất hoặc khoản tiền đền.

Thiết kế này chuẩn hóa header cho các màn quản lý và mở rộng luồng trả sách để một dòng mượn nhiều bản sao có thể vừa trả lại một phần vừa báo mất một phần. Nghiệp vụ mượn sách, xác thực người dùng và các thay đổi theme hiện có được giữ nguyên.

## Phạm vi

- Đưa tiêu đề và nhóm nút thao tác lên cùng một header tại các màn quản lý dạng danh sách.
- Chuẩn hóa kiểu nút chính, nút phụ, hover, pressed, focus, khoảng cách và hành vi responsive.
- Cho phép đánh dấu sách mất và nhập số lượng mất trong hộp thoại trả sách.
- Tự tính tiền đền từ giá sách đang lưu trong database.
- Tiếp tục cho phép chỉnh mức phạt quá hạn trước khi xác nhận trả.
- Lưu riêng số lượng mất, tiền phạt và tiền đền.
- Bảo toàn transaction, trạng thái phiếu, tồn kho và quyền sửa phạt của Admin.

Không thêm thư viện UI ngoài, không đổi framework WinForms, không tạo bảng lịch sử mới và không hỗ trợ để lại một phần số lượng của cùng dòng ở trạng thái chưa trả. Khi một dòng được chọn xử lý, toàn bộ `SoLuong` của dòng được giải quyết trong một lần: một phần trả lại kho và phần còn lại có thể được ghi nhận là mất.

## Header dùng chung

### Cấu trúc

Tạo một control hoặc helper header dùng chung với hai vùng:

- Bên trái là tiêu đề trang.
- Bên phải là `FlowLayoutPanel` chứa các nút thao tác theo thứ tự chính trước, phụ sau.

Ở chiều rộng đủ lớn, tiêu đề và nút nằm trên cùng một hàng. Khi không đủ chỗ, vùng nút tự chuyển xuống hàng dưới và chiều cao header tăng; bảng hoặc nội dung bên dưới phải lấy vị trí thực tế của header thay vì dùng một tọa độ cố định.

Header được áp dụng cho các màn quản lý dạng danh sách gồm Kho sách, Độc giả, Nhân viên, Tác giả, Nhà xuất bản, Thể loại, Phiếu mượn và Phiếu trả. Các màn Danh mục và Báo cáo dùng cùng kiểu tiêu đề nhưng chỉ hiển thị nhóm nút nếu thực sự có hành động. Dashboard, Đăng nhập và các dialog không bị ép vào cấu trúc header này.

### Kiểu nút

- Nút chính dùng `ModernButton`, màu teal semantic từ `AppColors`, chữ đậm vừa, radius 12px và chiều cao 40px.
- Nút thêm dùng nhãn rõ nghĩa, ví dụ `+ Tạo phiếu mượn`, `+ Thêm sách`; không dùng nút chỉ có icon.
- Nút phụ hoặc chuyển chế độ dùng màu surface/trung tính và trạng thái selected rõ bằng màu, viền hoặc chữ; không thay đổi kích thước khi hover/pressed.
- Khoảng cách giữa các nút là 8px; vùng bấm tối thiểu 40px chiều cao và có `AccessibleName`.
- Focus bằng bàn phím phải nhìn thấy được; thứ tự Tab đi từ tiêu đề bỏ qua sang nút chính, nút phụ rồi nội dung trang.

## Hộp thoại trả sách

### Bảng chi tiết

Hộp thoại hiển thị các cột:

1. `Chọn trả`: chọn dòng chưa trả để xử lý.
2. `Mất sách`: bật chế độ báo mất cho dòng.
3. `SL mất`: số lượng mất, chỉ chỉnh được khi `Mất sách` được chọn.
4. `Mã sách`.
5. `Tên sách`.
6. `SL mượn`.
7. `Giá sách`.
8. `Phạt quá hạn`.
9. `Tiền đền`.
10. `Tổng thu`.

Các cột định danh, số lượng mượn, giá sách, tiền đền và tổng thu là chỉ đọc. Dòng đã trả trước đó bị vô hiệu hóa và dùng màu chữ phụ. Dòng chưa trả có thể chọn bằng chuột hoặc bàn phím.

### Tương tác

- Chọn `Mất sách` tự chọn `Chọn trả` và đặt `SL mất` mặc định bằng 1 nếu giá trị hiện tại bằng 0.
- `SL mất` là số nguyên từ 1 đến `SL mượn`. Bỏ chọn `Mất sách` đặt `SL mất` về 0.
- Nếu mượn 3, nhập mất 1 thì 2 cuốn được trả lại kho và 1 cuốn được ghi nhận là mất.
- Mức phạt/ngày/cuốn là số nguyên VND không âm, mặc định 10.000 VND và vẫn cho phép nhân viên chỉnh trước khi xác nhận.
- `Phạt quá hạn = số ngày quá hạn × mức phạt/ngày/cuốn × SL mượn` cho mỗi dòng được chọn. Sách mất vẫn chịu phạt quá hạn nếu phiếu quá hạn.
- `Tiền đền = GiaTien × SL mất` và không cho sửa thủ công.
- `Tổng thu = Phạt quá hạn + Tiền đền`.
- Footer hiển thị tổng tiền phạt, tổng tiền đền và tổng cần thu của toàn bộ dòng được chọn.
- Enter xác nhận, Esc hủy; xác nhận không đóng dialog khi validation hoặc transaction thất bại.

Giá và số lượng hiển thị chỉ là preview. Khi xác nhận, tầng dữ liệu phải đọc lại `Sach.GiaTien` và `ChiTietPhieuMuon.SoLuong` trong transaction để tính khoản tiền chính thức.

## Thay đổi schema

Thêm hai cột vào `ChiTietPhieuMuon`:

- `SoLuongMat INT NOT NULL DEFAULT 0`.
- `TienDenMatSach DECIMAL(18,2) NOT NULL DEFAULT 0`.

Không thêm `MatSach BIT` vì một cờ Boolean không thể biểu diễn trường hợp mất 1 trong 3 cuốn. Trạng thái mất được suy ra từ `SoLuongMat > 0`.

Thêm constraint:

- `SoLuongMat >= 0`.
- `SoLuongMat <= SoLuong`.
- `TienDenMatSach >= 0`.

`database.sql` dành cho cài đặt mới và một migration idempotent dành cho database hiện có phải chứa cùng định nghĩa. Dữ liệu cũ nhận giá trị 0 nên không đổi hành vi.

## Tầng dữ liệu và transaction

### Dữ liệu hiển thị

`GetChiTietPhieuMuon` trả thêm `Sach.GiaTien`, `SoLuongMat` và `TienDenMatSach`. Chi tiết phiếu hiển thị một trong các trạng thái:

- `Đã trả` khi `SoLuongMat = 0`.
- `Mất X cuốn` khi `SoLuongMat = SoLuong`.
- `Trả N / Mất X` khi `0 < SoLuongMat < SoLuong`.

Tiền phạt, tiền đền và tổng thu được hiển thị thành các cột riêng.

### Xác nhận trả

Thay danh sách ID đơn thuần bằng danh sách yêu cầu gồm `MaSach` và `SoLuongMat`; mức phạt/ngày/cuốn vẫn là tham số riêng. Transaction thực hiện:

1. Kiểm tra danh sách không rỗng, ID duy nhất, số lượng mất không âm và mức phạt là số nguyên VND không âm.
2. Khóa phiếu mượn, các chi tiết được chọn và các dòng sách bằng `UPDLOCK, HOLDLOCK`.
3. Xác nhận mỗi sách thuộc phiếu, chưa trả và số lượng mất không vượt `SoLuong` thật trong database.
4. Đọc `GiaTien` thật từ dòng sách đang khóa.
5. Tính `soLuongTra = SoLuong - SoLuongMat`.
6. Tính tiền phạt quá hạn từ hạn trả, ngày hiện tại, mức phạt và toàn bộ số lượng dòng.
7. Tính `TienDenMatSach = GiaTien × SoLuongMat`.
8. Ghi `NgayTra`, `TienPhat`, `SoLuongMat`, `TienDenMatSach` cho chi tiết.
9. Chỉ cộng `soLuongTra` trở lại `Sach.SoLuong`; sách mất không được cộng vào kho.
10. Đặt trạng thái phiếu thành `Đã trả một phần` nếu còn dòng chưa trả, ngược lại thành `Đã trả`.
11. Commit một lần; bất kỳ sai lệch hoặc lỗi nào đều rollback toàn bộ.

### Sửa tiền phạt sau khi trả

`UpdateReturnedLoanPenalty` chỉ phân bổ lại `TienPhat`. Hàm này không được sửa `SoLuongMat`, `TienDenMatSach`, ngày trả hoặc tồn kho. Chỉ Admin đang hoạt động được thực hiện như quy tắc hiện tại.

Các số liệu gọi là “tiền phạt” tiếp tục chỉ tổng hợp `TienPhat`. Nếu giao diện cần “tổng thu”, nó phải cộng `TienPhat + TienDenMatSach` và dùng nhãn rõ ràng, tránh nhập tiền đền vào thống kê phạt.

## Validation và lỗi

- Không cho xác nhận khi chưa chọn dòng.
- Không cho nhập số lượng mất âm, bằng 0 khi checkbox mất đang bật hoặc lớn hơn số lượng mượn.
- Không tin giá, số lượng hoặc tổng tiền do UI gửi; tầng dữ liệu luôn tính lại từ dữ liệu đã khóa.
- Nếu giá sách thay đổi trong lúc dialog đang mở, transaction dùng giá hiện tại từ dòng đang khóa. Nếu trạng thái phiếu hoặc chi tiết không còn hợp lệ, transaction trả về thông báo xung đột và giữ dialog mở.
- Lỗi SQL được ghi Debug, hiển thị thông báo thân thiện và không làm thay đổi tồn kho một phần.
- Migration phải có kiểm tra tồn tại trước khi thêm cột/default/constraint để chạy lặp lại an toàn.

## Kiểm thử và nghiệm thu

### Header và nút

- Kiểm tra các màn quản lý tại 1280×780, 1000×600 và 900×600.
- Tiêu đề và nút cùng hàng khi đủ chỗ; khi hẹp nút xuống hàng nhưng không che tiêu đề hoặc bảng.
- Nút có hover, pressed, disabled và focus rõ; Tab order đúng; không có nút bị cắt.

### Trả sách

- Trả toàn bộ bình thường: toàn bộ số lượng được cộng lại kho, số lượng mất và tiền đền bằng 0.
- Mượn 3, mất 1: kho tăng 2, `SoLuongMat = 1`, tiền đền bằng một lần giá sách.
- Mất toàn bộ: kho không tăng, tiền đền bằng giá sách nhân toàn bộ số lượng.
- Phiếu quá hạn và có sách mất: tổng thu bằng tiền phạt quá hạn cộng tiền đền.
- Đổi mức phạt về 0 hoặc một số nguyên khác: tiền phạt thay đổi, tiền đền không đổi.
- Giá hiển thị bị cũ trước khi xác nhận: tầng dữ liệu dùng giá đang lưu trong transaction.
- Thử số lượng mất vượt số lượng mượn, ID trùng, dòng đã trả, phiếu đã đóng và xác nhận lặp: tất cả bị từ chối và không làm tăng kho hai lần.
- Trả một số đầu sách rồi trả phần còn lại: trạng thái lần lượt là `Đã trả một phần` và `Đã trả`.
- Admin sửa phạt sau khi trả: chỉ `TienPhat` đổi; tiền đền và tồn kho giữ nguyên.
- Chi tiết phiếu hiển thị đúng trạng thái và ba số tiền `Phạt`, `Đền`, `Tổng`.

### Kỹ thuật

- Build Release bằng `dotnet build --configuration Release` với 0 warning và 0 error.
- Chạy `git diff --check`.
- Bảo toàn mọi thay đổi chưa commit ngoài phạm vi tính năng, bao gồm theme, responsive UI và `CODE_REVIEW.md`.
