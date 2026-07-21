# Spec: Tạm tắt Trợ lý và sửa bố cục bảng nghiệp vụ

## Mục tiêu

Tạm ẩn hoàn toàn nút/panel Trợ lý khỏi giao diện chính, đồng thời sửa lỗi thanh lọc che header bảng và giảm độ chật của bảng Phiếu mượn khi cửa sổ không ở chế độ toàn màn hình.

Thay đổi chỉ tác động đến hiển thị và cách mở các thao tác hiện có. Nghiệp vụ, dữ liệu phiếu mượn, database và mã chatbot hiện tại được giữ nguyên.

## Thiết kế được chọn

### 1. Tạm tắt Trợ lý

- Thêm khóa cấu hình `ChatAssistantEnabled` với giá trị mặc định `false`.
- `FormMain` chỉ khởi tạo `ChatApiClient`, `ChatPanel` và `ChatLauncherButton` khi khóa này là `true`.
- Khi bị tắt, không tạo control ẩn, không gọi API và không dành khoảng trống cho Trợ lý.
- Không xóa source code chatbot để sau này có thể bật lại bằng cấu hình.

### 2. Sửa thanh lọc và header bảng

- `ResponsiveUi.AddFilterBar` chuyển vùng danh sách sang layout hai hàng rõ ràng: hàng bộ lọc cố định ở trên và bảng chiếm toàn bộ phần còn lại.
- Bộ lọc không được phủ lên `ColumnHeaders` của `DataGridView` ở bất kỳ kích thước cửa sổ hỗ trợ nào.
- `FilterBar` bỏ thanh cuộn nội bộ không cần thiết; giữ chiều cao, padding và khoảng cách theo nhịp 8px.
- Thay đổi dùng chung áp dụng cho Kho sách, Độc giả, Phiếu mượn và Trả sách nếu các trang sử dụng `ResponsiveUi.AddFilterBar`.

### 3. Gộp thao tác Phiếu mượn

- Thay năm cột `Chi tiết`, `Sửa`, `Gia hạn`, `Thu tiền`, `Sửa phạt` bằng một cột `Thao tác` có chiều rộng ổn định.
- Bấm ô `Thao tác` mở menu theo dòng gồm:
  - `Xem chi tiết`: luôn có.
  - `Sửa phiếu`: chỉ bật khi phiếu chưa có sách được trả.
  - `Gia hạn`: chỉ bật khi còn sách chưa trả.
  - `Thu tiền`: luôn hiển thị; dialog hiện tại tự xác định còn khoản phải thu hay không.
  - `Sửa tiền phạt`: chỉ bật cho Admin và phiếu đã trả hết.
- Các action tiếp tục gọi đúng các dialog/method đang có; không đổi kiểm tra nghiệp vụ ở `DataAccess`.
- Menu sử dụng chữ rõ ràng, trạng thái disabled và `AccessibleName`; không dùng emoji làm biểu tượng cấu trúc.

## Responsive và accessibility

- Header bảng và hàng đầu tiên luôn nhìn thấy tại `1280×780`, `1000×600` và `900×600`.
- Không xuất hiện thanh cuộn xám trong hàng lọc ở các kích thước nghiệm thu.
- Cột thao tác không ép tên độc giả, nhân viên và trạng thái xuống kích thước khó đọc.
- Có thể mở menu bằng chuột hoặc bàn phím; focus không bị mất khi menu đóng.

## Kiểm thử

- Kiểm tra Trợ lý không xuất hiện và không khởi tạo khi `ChatAssistantEnabled=false`.
- Kiểm tra bật lại bằng `true` vẫn hiển thị launcher/panel như trước.
- Kiểm tra bốn trang có bộ lọc: header bảng không bị che và bảng co giãn theo cửa sổ.
- Kiểm tra từng trạng thái Phiếu mượn để menu bật/tắt đúng các action.
- Chạy `dotnet build QuanLyThuVien.slnx --configuration Release` và yêu cầu 0 warning, 0 error.
- Chạy test hiện có để bảo đảm không làm hỏng Chat API và các chức năng trước đó.

## Ngoài phạm vi

- Xóa backend hoặc source code chatbot.
- Thay đổi schema database.
- Thay đổi cách tính tiền phạt, tiền đền hoặc điều kiện gia hạn.
- Redesign toàn bộ các dialog nghiệp vụ trong đợt sửa này.

