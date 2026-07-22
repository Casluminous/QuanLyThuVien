# Toolbar danh sách thống nhất

## Bối cảnh

Các trang Kho sách, Độc giả, Mượn trả và Trả sách hiện dùng `PageHeader` cho tiêu đề và nút hành động, nhưng đặt `FilterBar` ở một hàng riêng phía dưới. Cấu trúc này tạo khoảng trống dọc lớn và khiến bộ lọc trông tách rời khỏi thao tác chính của trang.

Thiết kế này gộp tiêu đề, bộ lọc và nút hành động vào một toolbar responsive dùng chung. Nghiệp vụ lọc, tải dữ liệu, xuất PDF và các thao tác thêm hoặc sửa dữ liệu được giữ nguyên.

## Phạm vi

- Áp dụng cho Kho sách, Độc giả, Mượn trả và Trả sách.
- Giữ nguyên `FilterBar`, các sự kiện `FilterChanged`, trạng thái bộ lọc và thứ tự dữ liệu.
- Không thay đổi database, API dữ liệu, xuất PDF hoặc nghiệp vụ mượn trả.
- Không áp dụng cho Dashboard, Báo cáo, Danh mục, dialog hoặc các trang không có bộ lọc.

## Cấu trúc toolbar

`PageHeader` được mở rộng để nhận một vùng bộ lọc tùy chọn bên cạnh tiêu đề và vùng hành động hiện có.

Ở chiều rộng đủ lớn, thứ tự hiển thị từ trái sang phải là:

1. Tiêu đề trang.
2. Bộ lọc tìm kiếm.
3. Khoảng co giãn.
4. Các nút hành động ở bên phải.

Tiêu đề, bộ lọc và nút được căn giữa theo chiều dọc trong cùng một hàng. Toolbar dùng lề trái và phải 8px; khoảng cách giữa tiêu đề, bộ lọc và từng nút là 8px. Không dùng margin âm hoặc tọa độ chồng lên nhau.

`FilterBar` khi nằm trong toolbar không tạo thêm khoảng đệm trên hoặc dưới. Các control bên trong giữ chiều cao 36px, thứ tự Tab hiện có và `AccessibleName` hiện có.

## Hành vi responsive

`PageHeader` đo chiều rộng ưa dùng của tiêu đề, toàn bộ bộ lọc và nhóm nút mỗi khi resize.

- Nếu đủ chỗ, tất cả nằm trên một hàng cao 56px.
- Nếu không đủ chỗ, toàn bộ `FilterBar` chuyển xuống hàng thứ hai; không tách riêng ô tìm kiếm, nút xóa lọc hoặc combo box.
- Hàng thứ hai có chiều cao 52px và giữ lề ngang 8px.
- Nhóm nút hành động tiếp tục nằm bên phải hàng đầu nếu tiêu đề và nút còn vừa; nếu bản thân nhóm nút không vừa, nó dùng hành vi wrap hiện có của `PageHeader`.
- Chiều cao `PageHeader` được cập nhật theo số hàng thực tế để bảng bên dưới luôn bắt đầu ngay sau toolbar và không bị che.
- Không thu nhỏ control dưới kích thước thiết kế, không cắt nhãn và không tạo cuộn ngang.

Ngưỡng xuống hàng được tính từ chiều rộng thực tế thay vì một breakpoint cố định, nên hoạt động ổn định với nội dung tiếng Việt và mức DPI khác nhau.

## Thay đổi component

### `PageHeader`

- Thêm API gắn một `FilterBar` tùy chọn.
- Quản lý ba vùng: tiêu đề, bộ lọc và hành động.
- Tính layout một hàng hoặc hai hàng trong `ArrangeChildren`.
- Giữ `AccessibleRole`, thứ tự Tab và trạng thái focus của các control con.

### `FilterBar`

- Hỗ trợ chế độ nhúng trong header với padding dọc bằng 0.
- Cung cấp kích thước ưa dùng dựa trên tổng chiều rộng các control và margin.
- Giữ margin 8px giữa ô tìm kiếm, nút xóa lọc và combo box.

### `ResponsiveUi`

- `AddFilterBar` gắn bộ lọc vào `PageHeader` thay vì tạo một hàng riêng trong vùng chứa bảng.
- Vùng chứa bảng chỉ còn một control dock-fill và tự động bắt đầu sau chiều cao thực tế của header.

### `FormSach`

- Chuyển cấu trúc tùy biến hiện tại sang API toolbar chung.
- Giữ nguyên chuyển đổi dạng Bảng/Thư viện, bộ lọc thể loại, bộ lọc tồn kho và nút Xuất PDF.

## Khả năng truy cập

- Thứ tự Tab: ô tìm kiếm, Xóa lọc, các combo box, nút hành động, rồi bảng hoặc nội dung trang.
- Không thay đổi `AccessibleName` của control hiện tại.
- Focus ring và trạng thái hover/pressed không làm thay đổi kích thước hoặc vị trí layout.
- Không dùng màu sắc làm dấu hiệu duy nhất cho trạng thái được chọn.

## Kiểm thử và nghiệm thu

- Build Release bằng `dotnet build QuanLyThuVien.slnx --configuration Release --no-restore` với 0 warning và 0 error.
- Chạy `git diff --check`.
- Kiểm tra Kho sách, Độc giả, Mượn trả và Trả sách tại 1280×780, 1000×600 và 900×600.
- Ở chiều rộng đủ lớn, tiêu đề, bộ lọc và nút hành động nằm cùng một hàng với margin 8px.
- Ở chiều rộng không đủ, bộ lọc xuống nguyên khối ở hàng thứ hai, không cắt chữ và không che bảng.
- Kiểm tra chuyển trang và resize nhiều lần không làm mất nội dung tìm kiếm hoặc lựa chọn bộ lọc.
- Kiểm tra Tab, Shift+Tab, focus ring và thao tác lọc bằng bàn phím.
- Đối chiếu lọc dữ liệu, chuyển dạng Kho sách và xuất PDF để bảo đảm hành vi không đổi.
- Bảo toàn toàn bộ thay đổi chưa commit ngoài phạm vi toolbar.
