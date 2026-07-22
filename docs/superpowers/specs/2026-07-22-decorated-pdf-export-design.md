# Spec: Xuất PDF trang trí cho toàn bộ QuanLyThuVien

## Mục tiêu

Thay toàn bộ đầu ra CSV và Print Preview hiện có bằng file PDF được thiết kế thống nhất theo Library Teal. PDF phải dùng được như tài liệu chính thức khi demo: tiếng Việt hiển thị đúng, bảng tự chia trang, số tiền rõ ràng, có header/footer và không phụ thuộc máy in ảo Windows.

Phạm vi gồm phiếu mượn, hóa đơn trả sách, lịch sử độc giả, danh sách sách/độc giả/phiếu mượn/phiếu trả và các bảng báo cáo. Không thay đổi nghiệp vụ, schema database hoặc dữ liệu đã lưu.

## Công nghệ

- Thêm NuGet package ổn định `PDFsharp-MigraDoc-GDI` phiên bản `6.2.4` cho project WinForms.
- Dùng MigraDoc để dựng tài liệu, bảng, phân trang, header/footer và render PDF.
- Dùng biến thể GDI vì ứng dụng chỉ chạy Windows/WinForms và cần dùng font Windows ổn định.
- Không dùng Microsoft Print to PDF, không yêu cầu cài máy in ảo và không dùng QuestPDF.

## Kiến trúc code

### Thành phần dùng chung

- `PdfTheme`: màu Library Teal, font Segoe UI, cỡ chữ, lề, khoảng cách, style bảng và format tiền/ngày.
- `PdfExportService`: mở `SaveFileDialog`, dựng tên file, gọi renderer, xử lý lỗi ghi file và thông báo thành công.
- `PdfDocumentBase`: tạo A4, header `QLTV - HỆ THỐNG QUẢN LÝ THƯ VIỆN`, footer người xuất/thời gian/trang hiện tại và các helper section/table.
- `GridPdfDocument`: xuất một snapshot của các cột/dòng dữ liệu đang hiển thị; bỏ cột thao tác, giữ thứ tự sort/filter hiện tại.
- `LoanReceiptPdfDocument`, `ReturnInvoicePdfDocument`, `ReaderHistoryPdfDocument`: các document chuyên biệt lấy model đã chuẩn hóa, không đọc trực tiếp control UI trong lúc render.

### Dữ liệu

- Danh sách/báo cáo được snapshot từ `DataGridView` sau khi đã lọc và sort.
- Phiếu mượn, hóa đơn trả và lịch sử độc giả tải lại dữ liệu chuẩn từ `DataAccess` theo ID trước khi xuất.
- Hóa đơn trả sách chỉ được tạo từ giao dịch đã xác nhận thành công trong database. Không tạo hóa đơn chính thức từ preview trước khi transaction trả sách hoàn tất.
- Tiền phải thu/đã thu/còn lại dùng cùng truy vấn hiện tại với dialog thanh toán; không tính lại bằng chuỗi hiển thị.

## Nhận diện và bố cục chung

- Khổ A4; tài liệu nghiệp vụ dùng portrait, danh sách nhiều cột dùng landscape.
- Lề trái/phải 18 mm, trên/dưới 18-20 mm.
- Font Segoe UI; tiêu đề 18-20 pt, section 11-12 pt, nội dung 9-10 pt, metadata 8 pt.
- Header teal đậm gồm `QLTV`, dòng `HỆ THỐNG QUẢN LÝ THƯ VIỆN` và tên tài liệu.
- Góc phải header hiển thị mã phiếu nếu có và ngày xuất.
- Bảng có header teal, chữ trắng, border mảnh, zebra row nhẹ, nội dung dài tự xuống dòng.
- Ngày căn giữa; số lượng và tiền căn phải; tiền theo dạng `85.000 đ` hoặc format tương đương theo culture Việt Nam.
- Footer hiển thị nhân viên xuất, thời gian xuất và `Trang X / Y`.
- Không dùng emoji, gradient nặng, watermark hoặc hình nền gây khó đọc.

## Từng loại tài liệu

### Phiếu mượn

- Mã phiếu, độc giả, nhân viên, ngày mượn, hạn trả và trạng thái.
- Bảng sách gồm mã/tên sách, số lượng và giá tham khảo.
- Ghi chú trách nhiệm bảo quản và trả sách đúng hạn.
- Hai vùng ký tên `Độc giả` và `Nhân viên`.

### Hóa đơn trả sách

- Mã phiếu, độc giả, nhân viên xử lý và ngày xuất.
- Bảng từng sách gồm số lượng mượn, kết quả trả/mất, phạt quá hạn và tiền đền.
- Ba ô tổng kết `Phải thu`, `Đã thu`, `Còn lại` và nhãn trạng thái `Chưa thu`, `Thu một phần` hoặc `Đã thu đủ`.
- Hai vùng ký tên `Người nộp` và `Nhân viên`.
- Sau khi transaction trả sách thành công, hỏi người dùng vị trí lưu hóa đơn PDF. Có thể xuất lại từ chi tiết phiếu mượn nếu phiếu đã có dòng được trả.

### Lịch sử độc giả

- Thẻ tóm tắt tên độc giả, trạng thái/hạn thẻ.
- Bảng phiếu mượn, hạn trả, ngày trả cuối, trạng thái, số sách, tiền phạt và tiền đền.
- Tổng số phiếu, tổng sách và tổng tiền phát sinh.

### Danh sách và báo cáo

- Xuất dữ liệu đang nhìn thấy sau khi lọc/sort, không xuất cột action ẩn/hiện.
- Hiển thị tên báo cáo, thời điểm xuất, điều kiện lọc dạng mô tả ngắn nếu có và tổng số bản ghi.
- A4 landscape cho bảng rộng; header bảng lặp lại trên mọi trang.
- Thay nút `Xuất CSV` bằng `Xuất PDF` ở Kho sách, Độc giả, Mượn trả, Trả sách và Báo cáo.

## Tên file và luồng lưu

- Tên mặc định không dấu, ổn định và có timestamp:
  - `phieu-muon-15_20260722_143000.pdf`
  - `hoa-don-tra-15_20260722_143000.pdf`
  - `lich-su-doc-gia-8_20260722_143000.pdf`
  - `danh-sach-sach_20260722_143000.pdf`
- `SaveFileDialog` chỉ nhận `.pdf`, tự thêm phần mở rộng và nhớ thư mục gần nhất theo hành vi mặc định của Windows.
- Không tự ghi đè file; Windows hiển thị xác nhận khi tên file đã tồn tại.
- Nếu không có dữ liệu thì không tạo file và hiển thị empty-state message.
- Nếu file đang mở, đường dẫn không hợp lệ hoặc thiếu quyền ghi, hiển thị lỗi thân thiện và giữ nguyên màn hình để người dùng thử lại.

## Thay đổi giao diện

- `Xuất CSV` đổi thành `Xuất PDF`.
- `In phiếu`/`In lịch sử` đổi thành `Lưu PDF`/`Lưu lịch sử PDF`.
- Trong chi tiết phiếu mượn có `PDF phiếu mượn`; nếu đã có sách trả, thêm `PDF hóa đơn trả`.
- Nút trong dialog trả sách trước khi xác nhận không tạo hóa đơn chính thức; hóa đơn được đề nghị lưu sau khi transaction thành công.
- Giữ Library Teal, keyboard focus, `AccessibleName`, và không làm footer dialog bị cắt tại `900x600`.

## Kiểm thử

### Chức năng

- Xuất đủ các loại tài liệu từ dữ liệu rỗng, một dòng và nhiều trang.
- Danh sách PDF giữ đúng filter, sort, thứ tự cột và không có cột thao tác.
- Phiếu mượn/hóa đơn/lịch sử khớp dữ liệu database và tổng tiền hiện tại.
- Hóa đơn không được tạo nếu transaction trả sách thất bại.
- Tên file, phần mở rộng, hủy SaveFileDialog và lỗi file đang mở được xử lý đúng.

### PDF visual QA

- Tạo PDF mẫu đại diện trong `tmp/pdfs/`.
- Dùng Poppler render mọi trang thành PNG và kiểm tra header/footer, bảng, phân trang, font tiếng Việt, số tiền và vùng ký tên.
- Dùng `pdfinfo` kiểm tra khổ giấy/số trang và `pdftotext` hoặc `pdfplumber` kiểm tra nội dung chính.
- Không chấp nhận text bị cắt/chồng, ô đen do font, header thiếu ở trang sau hoặc bảng vượt lề.
- Xóa file trung gian sau khi nghiệm thu; không commit PDF mẫu hoặc PNG render.

### Regression

- Build solution Release với 0 warning, 0 error.
- Test hiện có vẫn đạt.
- Mượn, trả từng phần, mất sách, gia hạn, sửa phạt và thu tiền không thay đổi hành vi.

## Ngoài phạm vi

- Ký số, mật khẩu PDF, gửi email hoặc upload cloud.
- Logo ảnh tùy chỉnh, thông tin trường/thư viện cấu hình động.
- Xuất Word/Excel/CSV trong phiên bản này.
- Thay đổi schema database hoặc lưu bản PDF vào database.

