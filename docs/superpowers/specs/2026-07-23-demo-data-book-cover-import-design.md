# Dữ liệu demo và ảnh bìa cho QuanLyThuVien

## Mục tiêu

Tạo một bộ dữ liệu giả có thể nạp lặp lại cho bản demo thư viện nhỏ, đồng thời cung cấp ảnh bìa cục bộ đã cắt đúng tỉ lệ để giao diện Kho sách hiển thị đầy đủ và nhất quán.

## Phạm vi

- Tạo script SQL demo tại `QuanLyThuVien/Database/demo_seed_2026-07-23.sql`.
- Seed bổ sung, không xóa dữ liệu hiện có:
  - 20 đầu sách.
  - 10 độc giả.
  - 4 nhân viên demo.
  - 12 phiếu mượn/trả ở các trạng thái đang mượn, quá hạn, đã trả và đã trả một phần.
  - Một vài chi tiết có tiền phạt và đền sách mất để trình diễn luồng trả sách.
- Thêm 8 ảnh bìa demo đã cắt tỉ lệ 3:4 vào `QuanLyThuVien/Images/Sach`.
- Cập nhật `.csproj` để ảnh demo được copy sang thư mục runtime `Images/Sach`.
- Không thay đổi schema, API dữ liệu, nghiệp vụ mượn/trả hoặc các ảnh banner hiện có.

## Định dạng ảnh và quy trình cắt

- Ảnh bìa là JPEG RGB, kích thước cuối 768×1024 (tỉ lệ 3:4), tên `demo-cover-01.jpg` đến `demo-cover-08.jpg`.
- Ảnh được tạo theo phong cách minh họa bìa sách hiện đại, không dùng logo/thương hiệu thật, không chèn chữ nhỏ khó đọc hoặc watermark.
- Mỗi ảnh được crop từ ảnh nguồn portrait vào khung 3:4, ưu tiên giữ chủ thể ở vùng trung tâm và kiểm tra lại bằng preview trước khi đưa vào project.
- Database chỉ lưu khóa tương đối `Sach/demo-cover-0X.jpg`; tuyệt đối không lưu đường dẫn máy sinh ảnh.

## Thiết kế seed SQL

- Script dùng `SET XACT_ABORT ON` và transaction.
- Thể loại, tác giả, nhà xuất bản được tra theo tên và chỉ thêm nếu chưa tồn tại.
- Sách được nhận diện bằng ISBN demo ổn định; mỗi dòng chỉ thêm khi ISBN chưa tồn tại.
- Độc giả được nhận diện bằng email demo; nhân viên bằng tên đăng nhập demo.
- Phiếu mượn được nhận diện bằng người đọc, ngày mượn, hạn trả và trạng thái cố định của bộ demo; chi tiết chỉ thêm khi phiếu mới được tạo. Vì vậy chạy lại script không nhân bản phiếu hoặc trừ tồn kho lần thứ hai.
- Các phiếu active/lost mới trừ `Sach.SoLuong`; phiếu đã trả không trừ tồn kho. Chi tiết mất sách ghi `SoLuongMat=1` và `TienDenMatSach` bằng giá sách.
- Tài khoản demo dùng mật khẩu `Demo@123` và hash SHA-256 legacy để tương thích với cơ chế đăng nhập hiện tại; đây chỉ là dữ liệu phát triển và phải đổi mật khẩu nếu dùng ngoài demo.
- Script không xóa, reset identity hoặc cập nhật các bản ghi người dùng đã có.

## Luồng sử dụng

1. Chạy các migration/schema hiện tại nếu database chưa được tạo.
2. Chạy `demo_seed_2026-07-23.sql` bằng SSMS hoặc `sqlcmd` trên database `QuanLyThuVien`.
3. Build ứng dụng; các ảnh trong `QuanLyThuVien/Images/Sach` được copy vào `bin/<Configuration>/net10.0-windows/Images/Sach`.
4. Đăng nhập bằng tài khoản demo hoặc tài khoản hiện có, mở Kho sách và Mượn trả để kiểm tra dữ liệu.

## Kiểm thử và nghiệm thu

- Build Release không warning/error.
- Chạy script hai lần: số lượng sách, độc giả, phiếu và tồn kho không tăng lần thứ hai.
- Mọi giá trị `Sach.HinhAnh` của bộ demo bắt đầu bằng `Sach/` và resolve được tới file dưới `Images/Sach`.
- Preview ảnh không bị méo, không bị khóa file và giữ đúng tỉ lệ 3:4.
- Có thể xem được ít nhất một phiếu quá hạn, một phiếu đã trả, một phiếu trả một phần và một dòng đền tiền sách mất.
- Không có bản ghi hiện có bị xóa hoặc thay đổi ngoài các dòng demo có khóa nhận diện rõ ràng.
