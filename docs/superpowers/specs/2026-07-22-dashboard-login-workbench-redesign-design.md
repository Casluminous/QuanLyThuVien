# Redesign Đăng nhập và Tổng quan theo Operational Workbench

## Mục tiêu

Redesign hai màn hình `FormDangNhap` và `FormDashboard` theo phương án A đã duyệt: một trải nghiệm thư viện ấm áp, gần gũi nhưng vẫn ưu tiên thao tác công việc. Giữ nguyên Library Teal, Segoe UI, xác thực, quyền hạn, database và toàn bộ số liệu Dashboard.

Thiết kế tuân theo hệ thống đã khóa trong `design.md`. Đây là thay đổi giao diện và UX nhỏ; không thay đổi schema, truy vấn ghi dữ liệu hoặc nghiệp vụ mượn/trả.

## Phạm vi

### Trong phạm vi

- Bố cục, typography, surface và trạng thái tương tác của `FormDangNhap`.
- Bố cục, KPI, vùng cần xử lý, phiếu gần đây và hai biểu đồ của `FormDashboard`.
- Token warm paper/surface dành cho hai màn hình trong `AppColors`.
- Tái sử dụng và hoàn thiện `StatCard` cho KPI Dashboard.
- Thêm `DashboardSection` để tách tiêu đề, nội dung và empty/error state khỏi form.
- Keyboard navigation, focus, `AccessibleName` và layout responsive.

### Ngoài phạm vi

- `FormMain`, menu điều hướng và các trang quản lý còn lại.
- Database schema, `DataAccess` public API và logic xác thực.
- Thêm thư viện UI, font hoặc ảnh bên ngoài.
- Thay đổi nội dung hoặc cách tính KPI, tiền phạt, phiếu mượn và tồn kho.
- Khôi phục chatbot hoặc thay đổi cấu hình ChatApi.

## Hệ thiết kế

- Genre: modern-minimal, tone mềm và gần gũi.
- Macrostructure: Operational Workbench.
- Primary: Library Teal `#0F766E`; focus `#14B8A6`.
- Warm paper `#F4F7F3`; warm surface `#FFFDF8`; muted surface `#E8F2ED`.
- Primary text `#17302D`; secondary text `#5B706C`; border `#D8E5E2`.
- Segoe UI cho toàn bộ nội dung; Segoe UI Variable Display hoặc Segoe UI Bold cho tiêu đề.
- Spacing theo base 4 px, ưu tiên nhịp 8 px.
- Card radius 14 px; input/button radius 10 px; control chính cao ít nhất 44 px.
- Không gradient, không banner ảnh, không side-stripe card, không card lồng card và không animation trang trí.

## Màn hình Đăng nhập

### Bố cục

`FormDangNhap` chuyển từ banner phụ thuộc kích thước ảnh sang layout ổn định bằng `TableLayoutPanel`:

- Client size mặc định khoảng `920×560`, min size `760×500`.
- Cột trái 42% là identity pane nền teal đậm.
- Cột phải 58% là form pane nền warm paper.
- Form content có chiều rộng hữu dụng khoảng 360 px, căn giữa theo chiều dọc và lệch trái nhẹ trong pane.
- Cửa sổ hẹp vẫn giữ hai cột đến min size; nội dung không bị cắt và không dựa vào banner.

### Nội dung

- Identity pane: `QLTV`, nhãn `QUẢN LÝ THƯ VIỆN`, câu chào ngắn “Một ngày đọc sách bắt đầu từ đây.” và mô tả một dòng.
- Form pane: tiêu đề `Chào mừng trở lại`, hai label hiển thị, username, password, show-password control, vùng helper/error cố định và nút `Đăng nhập` toàn chiều rộng.
- Nút đóng cửa sổ nằm góc phải trên, có vùng bấm tối thiểu 44 px và `AccessibleName`.

### Tương tác

- Thay `PictureBox` hình mũi tên bằng `ModernButton` thực sự để có keyboard, focus và disabled state.
- `AcceptButton` trỏ tới nút đăng nhập; Enter từ bất kỳ input nào sẽ submit đúng một lần.
- Esc đóng form qua `CancelButton` hoặc xử lý phím tương đương.
- Focus mặc định vào username.
- Show-password thay đổi `UseSystemPasswordChar` mà không làm thay đổi kích thước input.
- Khi đang xử lý đăng nhập, nút tạm disabled và label không đổi vị trí; kết thúc hoặc lỗi sẽ khôi phục trạng thái.
- Lỗi trống trường, sai thông tin và lỗi kết nối tiếp tục dùng nội dung hiện tại nhưng hiển thị trong vùng lỗi cố định.

### Tài nguyên

- Bỏ việc đọc và giữ `Images/Banner/banner1.jpg` trong `FormDangNhap`.
- Không xóa file banner khỏi repository vì có thể còn được dùng ở nơi khác; không có file production nào bị xóa.
- Không tạo bitmap nút đăng nhập thủ công, tránh rò rỉ tài nguyên GDI từ image control.

## Màn hình Tổng quan

### Kiến trúc layout

`FormDashboard` chuyển từ tọa độ tuyệt đối cho toàn bộ page sang container theo hàng:

1. Header.
2. KPI strip.
3. Work row.
4. Analytics row.

Outer content dùng `AutoScroll`. Mỗi hàng dùng `TableLayoutPanel` hoặc panel responsive tương đương. Resize chỉ thay column count/ratio ở breakpoint; không tính lại vị trí từng control bằng tọa độ rải rác.

### Header

- Trái: `Chào buổi sáng/chiều/tối, {HoTen}` dựa trên thời gian và session hiện tại.
- Dòng phụ: mô tả ngắn `Đây là những việc cần chú ý trong ca làm việc.`
- Phải: ngày `dd/MM/yyyy` và nhãn `HÔM NAY`.
- Nếu không có tên session, dùng `Thủ thư`, không hiển thị chuỗi rỗng.

### KPI strip

Bốn `StatCard` cùng chiều cao:

1. Phiếu đang mở.
2. Sách trong kho.
3. Bạn đọc.
4. Chưa thu.

`StatCard` được chỉnh để:

- Dùng warm surface, border mảnh và shadow nhẹ.
- Bỏ accent line dưới card.
- Giá trị lớn nhưng không vượt card.
- Có `AccessibleName` gồm label và giá trị.
- Không dùng màu riêng làm tín hiệu duy nhất.

### Work row

- `Cần xử lý hôm nay` chiếm khoảng 60% chiều rộng.
- `Phiếu gần đây` chiếm khoảng 40%.
- Cần xử lý sắp theo mức ưu tiên hiện tại: quá hạn trước, sau đó sắp đến hạn; các cảnh báo thẻ, tồn kho thấp và khoản chưa thu vẫn được giữ.
- Mỗi mục hiển thị tiêu đề và chi tiết trạng thái. Không thêm điều hướng mới trong lần redesign này để giữ nguyên ranh giới giữa Dashboard và `FormMain`.
- Empty state: `Không có việc cần xử lý ngay.` với màu success và không có CTA giả.
- Error state nêu rõ khu vực không tải được và có action `Tải lại` cục bộ.
- Bảng phiếu gần đây giữ bốn cột: mã, độc giả, hạn trả và trạng thái; tối đa bốn dòng.

### Analytics row

- Giữ cả `XU HƯỚNG MƯỢN SÁCH` và `KHO SÁCH THEO THỂ LOẠI`.
- Biểu đồ xu hướng chiếm khoảng hai phần ba; biểu đồ thể loại chiếm một phần ba ở desktop.
- Màu chart lấy từ token Library Teal và semantic colors hiện có.
- Chart title nằm trong section header, không chèn label nổi trên vùng vẽ.
- Không chạy animation trang trí khi tải.
- Không có dữ liệu vẫn hiển thị trục/empty state có nhãn, không phát sinh exception.

### Responsive

- Từ `1000 px` trở lên: KPI 4 cột, work row 60/40, analytics 2/3–1/3.
- Từ `760–999 px`: KPI 2×2; work và analytics chuyển thành một cột.
- Main form tối thiểu `900×600` vẫn hiển thị header, KPI đầu tiên và work row thông qua cuộn dọc; không có cuộn ngang ngoài ý muốn.
- Thứ tự một cột: KPI → cần xử lý → phiếu gần đây → xu hướng → thể loại.

## Component boundaries

- `FormDangNhap`: xây layout, điều phối validation và gọi `DataAccess.DangNhap`; không tự vẽ button bitmap.
- `FormDashboard`: điều phối dữ liệu và cấu trúc hàng; không chứa logic custom-paint card.
- `StatCard`: vẽ KPI surface, typography và accessibility; không truy cập database.
- `DashboardSection`: header, body host và empty/error state; không biết dữ liệu nghiệp vụ.
- `AppColors`: cung cấp warm paper/surface tokens; không chứa logic trang.

## Data flow

### Login

`ModernTextBox` → validate required fields → `DataAccess.DangNhap` → `Session.CurrentUser` → mở `FormMain`.

Luồng và thông báo xác thực hiện tại được bảo toàn. UI state được đặt lại trong `finally` để nút không bị disabled vĩnh viễn khi có exception.

### Dashboard

`FormDashboard.Load` → đọc count/query hiện có → bind KPI, alerts, recent loans và charts → hiển thị section-level empty/error state.

Không thêm cache, background service hoặc schema. Tránh gọi `GetAllPhieuMuon` nhiều lần trong cùng một lần load bằng cách tái sử dụng một snapshot `DataTable` nội bộ nếu không làm thay đổi dữ liệu hiển thị.

## Error handling

- Login giữ ba loại lỗi riêng: thiếu input, sai credential và lỗi kết nối/xử lý.
- Dashboard không để lỗi một section làm mất toàn bộ page.
- Mọi exception kỹ thuật tiếp tục ghi `Debug`; người dùng nhận nội dung tiếng Việt ngắn, có hướng xử lý.
- Không nuốt exception rỗng và không hiển thị stack trace hoặc connection string.
- Nếu bảng thanh toán phạt chưa tồn tại, Dashboard giữ fallback hiện có cho KPI chưa thu; vùng cảnh báo hiển thị phần còn lại.

## Accessibility và keyboard

- Tab order: username → password → show password → đăng nhập → đóng.
- Dashboard: KPI read-only → action cần xử lý → bảng gần đây → chart accessibility label.
- Nút/icon có `AccessibleName`, `AccessibleRole` phù hợp.
- Focus ring xuất hiện ngay, tương phản rõ trên warm paper và teal surface.
- Trạng thái warning/danger luôn có text `Quá hạn`, `Hạn hôm nay`, `Không tải được` hoặc nội dung tương đương.
- Dòng và control không thay đổi kích thước giữa default/hover/focus/error.

## File plan

### Tạo

- `design.md` — hệ thiết kế khóa cho hai trang.
- `tokens.css` — export Hallmark, không được WinForms load trực tiếp.
- `.hallmark/preflight.json` — kết quả pre-flight.
- `.hallmark/log.json` — project memory sau khi implementation hoàn tất.
- `QuanLyThuVien/Controls/DashboardSection.cs` — section surface dùng chung trong Dashboard.

### Sửa

- `QuanLyThuVien/Forms/FormDangNhap.cs`.
- `QuanLyThuVien/Forms/FormDashboard.cs`.
- `QuanLyThuVien/Controls/StatCard.cs`.
- `QuanLyThuVien/Helpers/AppColors.cs`.

### Xóa

- Không xóa file nào.

## Kiểm thử và nghiệm thu

### Build

- `dotnet build QuanLyThuVien.slnx --configuration Release -p:TreatWarningsAsErrors=true`.
- Kỳ vọng 0 warning, 0 error.
- Chạy toàn bộ test hiện có trong solution.

### Login

- Kiểm tra kích thước mặc định và min size.
- Không có banner hoặc vùng trắng phụ thuộc ảnh.
- Tab order đúng; Enter đăng nhập; Esc đóng; show password hoạt động.
- Trống input, sai credential và SQL unavailable không làm layout nhảy hoặc treo nút.
- Đăng nhập đúng vẫn mở `FormMain` và đóng ứng dụng đúng như trước.

### Dashboard

- Kiểm tra tại `1280×780`, `1000×600`, `900×600`.
- KPI không cắt số hoặc nhãn.
- Work row đúng tỷ lệ trên desktop và đúng thứ tự khi stack.
- Hai chart vẫn hiển thị dữ liệu hiện có và empty state.
- Alerts, recent loans và số tiền chưa thu khớp query hiện tại.
- Không có horizontal scroll ngoài bảng nội bộ; không có control chồng nhau.
- Lỗi một section không làm Dashboard trắng toàn bộ.

### Regression

- Không thay đổi `DataAccess` public behavior.
- Không thay đổi đăng nhập, session, quyền Admin/NhanVien hoặc điều hướng sau đăng nhập.
- Không thay đổi nghiệp vụ mượn/trả, tồn kho và thanh toán phạt.
- Không khởi động chatbot và không đổi `App.config`.

## Hallmark accountability

- Macrostructure: Operational Workbench.
- Theme: Library Teal with warm paper surfaces.
- Enrichment: none; typography and operational data only.
- Motion: hover, pressed and focus states only.
- The redesign replaces the prior Bento emphasis with task-first hierarchy.
- Production files are edited in place; no deletion or route replacement is allowed.
