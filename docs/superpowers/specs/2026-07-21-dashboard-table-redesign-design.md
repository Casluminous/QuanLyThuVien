# Dashboard Bento và chuẩn hóa bảng thư viện

## Mục tiêu

Redesign phần hiển thị của ứng dụng WinForms theo hướng **Bento quản trị thư viện**: thông tin vận hành được ưu tiên, bố cục rõ ràng ở nhiều kích thước cửa sổ, và tất cả bảng dùng chung một quy tắc căn chỉnh. Không thay đổi schema database, nghiệp vụ mượn/trả, quyền hạn hoặc API dữ liệu.

## Phạm vi

- Trang Tổng quan (`FormDashboard`).
- Danh sách phiếu mượn và các bảng liên quan (`FormPhieuMuon`, `FormPhieuTra`).
- `ModernDataGridView` và cấu hình bảng dùng chung trên Kho sách, Độc giả, Nhân viên, Danh mục, Báo cáo.
- Không thêm thư viện UI bên ngoài; giữ nguyên bộ token Library Teal.

## Thiết kế Dashboard

Dashboard dùng lưới 12 cột với khoảng cách theo nhịp 8px:

1. Header: tiêu đề, ngày hiện tại và mô tả ngắn.
2. Hàng KPI: bốn card cùng chiều cao cho sách trong kho, độc giả, phiếu đang mở và sách quá hạn.
3. Khu “Cần xử lý”: phiếu quá hạn/sắp đến hạn, có số lượng và nút điều hướng tới phiếu mượn.
4. Khu phân tích: biểu đồ cơ cấu thể loại dạng donut và xu hướng mượn dạng line/bar; có tiêu đề, chú giải và trạng thái rỗng.
5. Khu hoạt động gần đây: bảng nhỏ các phiếu mượn mới nhất, dùng chung style với bảng chính.
6. Banner/sách nổi bật là khu phụ, không chiếm ưu tiên cao hơn KPI và cảnh báo vận hành.

Ở chiều rộng nhỏ, các khu chuyển thành một cột theo thứ tự KPI → cần xử lý → hoạt động → biểu đồ. Không để control bị cắt hoặc tạo vùng cuộn ngang ngoài ý muốn.

### Visual hierarchy

- Surface nền `AppColors.ContentBg`, card trắng, viền `AppColors.Border` và bóng nhẹ.
- Primary teal dùng cho CTA, header, trạng thái active và điểm nhấn quan trọng.
- Danger/warning luôn đi kèm nhãn chữ như “Quá hạn”, không chỉ dùng màu.
- Tiêu đề 20–24px, section 13–14px, dữ liệu 10–11px, metadata 9px.
- Số liệu KPI dùng font đậm; ngày dùng định dạng `dd/MM/yyyy`.

## Quy tắc phiếu mượn

### Thứ tự mặc định

`PhieuMuon` được lấy đầy đủ như hiện tại, sau đó sắp theo khóa hiển thị ổn định:

1. Phiếu quá hạn còn sách chưa trả.
2. Phiếu đang mượn, hạn trả gần nhất trước.
3. Phiếu đã trả một phần.
4. Phiếu đã trả hết.
5. Trong cùng nhóm: `HanTra ASC`, sau đó `NgayMuon DESC`, cuối cùng `MaPhieuMuon DESC`.

Trạng thái hiển thị được suy ra từ số dòng còn/chưa trả và hạn trả; không ghi ngược trạng thái vào database khi chỉ sắp xếp giao diện.

### Sort thủ công

Header bảng hỗ trợ sort theo cột với chỉ báo thứ tự tăng/giảm. Sort thủ công chỉ tác động trên danh sách hiển thị; không thay đổi truy vấn ghi dữ liệu. Khi reload sau thao tác, thứ tự mặc định được khôi phục nếu không có yêu cầu giữ bộ lọc.

## Chuẩn hóa bảng và lỗi lệch dòng

- Chỉ định một `DataGridViewCellStyle` dùng chung cho dòng thường và dòng xen kẽ; `Padding` trái/phải giống hệt nhau.
- `RowTemplate.Height` cố định, `AutoSizeRowsMode=None`, không dùng `WrapMode` cho dữ liệu dạng bảng trừ cột mô tả.
- Dữ liệu chữ căn trái; mã/số lượng/ngày/trạng thái căn giữa; số tiền căn phải.
- Không đặt `Padding`, `Margin` hoặc `Indent` riêng theo dòng/cell.
- `RowHeadersVisible=false`, `CellBorderStyle=SingleHorizontal`, `GridColor` theo token border.
- Zebra row chỉ thay đổi màu nền, tuyệt đối không thay đổi vị trí text.
- Cột thao tác có chiều rộng cố định tối thiểu 84px; cột dữ liệu có min width và cuộn ngang khi cần.
- Hover/selected giữ nguyên kích thước dòng và dùng màu nền semantic.
- Các bảng hiện có phải chuyển về cấu hình dùng chung, không lặp style gây khác biệt giữa từng form.

## Component boundaries

- `ModernDataGridView`: style, căn chỉnh, sort indicator, hover/selected và cột thao tác.
- `DashboardKpiCard` hoặc helper tương đương: hiển thị nhãn, giá trị, mô tả và trạng thái semantic.
- `DashboardSection`: tiêu đề section, action phụ và trạng thái rỗng.
- `FormDashboard`: chỉ chịu trách nhiệm bố cục và binding dữ liệu; không chứa style bảng lặp lại.
- `FormPhieuMuon`/`FormPhieuTra`: cung cấp dữ liệu và hành động nghiệp vụ, dùng component bảng chung.

## Accessibility và interaction

- Tab order theo thứ tự header → KPI/action → bảng → biểu đồ.
- Nút và cột thao tác có `AccessibleName` rõ ràng.
- Sort bằng bàn phím qua header; trạng thái sort được thể hiện bằng biểu tượng/chữ, không chỉ bằng màu.
- Empty/error state có thông báo và hành động khôi phục/tải lại.
- Không thêm animation trang trí; chỉ dùng hover/focus transition nhẹ, không làm đổi kích thước layout.

## Kiểm thử nghiệm thu

- Build Release với `TreatWarningsAsErrors=true`, không warning/error.
- UI smoke test tại `1280×780`, `1000×600`, `900×600`.
- Kiểm tra Dashboard không cắt card, banner, biểu đồ hoặc vùng cần xử lý.
- Kiểm tra tất cả bảng: dòng xen kẽ không bị thụt, cột thao tác thẳng hàng, scroll ngang hoạt động.
- Kiểm tra thứ tự phiếu: quá hạn → đang mượn → trả một phần → đã trả; tie-breaker ổn định.
- Kiểm tra sort header, keyboard navigation, focus/selected/disabled và trạng thái rỗng.
- Đối chiếu các thao tác xem/sửa/trả sách để bảo đảm chỉ đổi hiển thị, không đổi hành vi nghiệp vụ.
