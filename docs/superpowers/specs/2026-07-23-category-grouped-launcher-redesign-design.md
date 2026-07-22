# Redesign trang Danh mục thành Grouped Launcher

## Tóm tắt

Giữ vai trò hiện tại của trang `Danh mục` như một trang mở nhanh, nhưng mở rộng từ ba thẻ thành một chỉ mục đầy đủ cho các chức năng chính của ứng dụng. Bố cục được chia theo nhóm công việc để giảm khoảng trống, giúp nhân viên thư viện tìm đúng khu vực nhanh hơn và vẫn giữ ngôn ngữ Library Teal đã khóa trong `design.md`.

Thay đổi chỉ tác động đến lớp giao diện và điều hướng. Không thay đổi schema, dữ liệu, nghiệp vụ hoặc API của các màn hình đích.

## Bối cảnh hiện tại

- `FormDanhMuc` chỉ có ba thẻ: Thể loại, Tác giả và Nhà xuất bản.
- Các thẻ có kích thước cố định `220×140`, đặt bằng tọa độ tuyệt đối nên để lại khoảng trống lớn trên màn hình rộng.
- `LoadSub` tự thêm màn hình con vào `Parent.Controls`, làm menu trên giữ trạng thái `Danh mục` khi người dùng đã chuyển sang một trang cấp cao khác.
- Luồng điều hướng của menu nằm riêng trong `FormMain.MenuButton_Click`, chưa có một hàm dùng chung cho thẻ launcher.
- `QuanLyThuVien.csproj` đang tham chiếu tuyệt đối tới `C:\Users\steal\Downloads\DataAccess.cs`, tạo lớp `DataAccess` trùng lặp và làm build trên máy hiện tại thất bại.

## Mục tiêu

- Giữ trải nghiệm “bấm thẻ để mở trang” như hiện tại.
- Bổ sung các chức năng đang có của ứng dụng vào trang Danh mục.
- Chia thẻ theo nhóm công việc, sử dụng chiều rộng màn hình hợp lý.
- Khi mở một trang cấp cao, menu phía trên chuyển active đúng mục.
- Các trang con Thể loại, Tác giả và Nhà xuất bản vẫn giữ `Danh mục` active vì không có mục menu cấp cao riêng.
- Hỗ trợ chuột, bàn phím và screen reader.
- Bảo toàn quyền truy cập: thẻ Thủ thư chỉ xuất hiện với Admin, giống menu hiện tại.

## Không nằm trong phạm vi

- Không thêm bảng hoặc dữ liệu mới vào database.
- Không hiển thị số lượng, KPI hoặc dữ liệu xem trước trên thẻ.
- Không thay đổi các bảng quản lý bên trong `FormSach`, `FormPhieuMuon`, `FormPhieuTra`, `FormDocGia`, `FormNhanVien`, `FormTheLoai`, `FormTacGia`, `FormNhaXuatBan` hoặc `FormBaoCao`.
- Không thêm icon pack hoặc thư viện UI bên ngoài.
- Không đưa `Tổng quan` vào launcher; đây là trang chủ, không phải khu vực quản lý.

## Định hướng Hallmark

- Genre: modern-minimal.
- Tone: soft, utilitarian.
- Page macrostructure: `Index-First · Grouped Launcher`.
- Design system: đọc và tuân theo `design.md`.
- Theme: Library Teal hiện có; không thêm palette hoặc font mới.
- Enrichment: không dùng hình ảnh hay minh họa; chức năng là nội dung chính.
- Motion: cut; chỉ có hover, pressed và focus tức thời, không animation trang.

`design.md` sẽ được bổ sung một allowance cho trang Danh mục: app page này dùng biến thể `Grouped Launcher` trong cùng hệ Library Teal thay vì cấu trúc Dashboard `Operational Workbench`.

## Kiến trúc thông tin

### Vận hành

- Kho sách — “Quản lý đầu sách và tồn kho”.
- Mượn trả — “Tạo và quản lý phiếu mượn”.
- Trả sách — “Tiếp nhận sách trả và tiền phạt”.

### Con người

- Độc giả — “Quản lý hồ sơ bạn đọc”.
- Thủ thư — “Quản lý tài khoản nhân viên”. Chỉ hiển thị khi `Session.IsAdmin` là `true`.

### Dữ liệu nền

- Thể loại — “Quản lý phân loại sách”.
- Tác giả — “Quản lý thông tin tác giả”.
- Nhà xuất bản — “Quản lý thông tin nhà xuất bản”.

### Phân tích

- Báo cáo — “Theo dõi thống kê thư viện”.

## Bố cục

- Trang dùng một vùng cuộn chính, padding theo nhịp 8 px và không có scroll ngang.
- Header gồm tiêu đề `Danh mục` và mô tả ngắn `Truy cập nhanh các khu vực quản lý thư viện`.
- Mỗi nhóm có heading Semibold và một rule một pixel kéo dài phần còn lại của hàng.
- Các thẻ trong cùng nhóm dùng chiều cao thống nhất; nội dung gồm một accent rule ngắn, tiêu đề, mô tả và mũi tên ở mép phải.
- Không dùng side stripe dày, gradient, card lồng trong card hoặc bóng nhiều lớp.
- Nhóm Báo cáo chỉ có một thẻ và được trình bày dạng hàng rộng, tránh một ô đơn lẻ nằm trong lưới ba cột.

### Responsive

- Từ khoảng 1100 px nội dung trở lên: ba cột cho Vận hành và Dữ liệu nền; hai cột cho Con người.
- Từ khoảng 760 đến 1099 px: hai cột.
- Dưới khoảng 760 px: một cột.
- `FormMain.MinimumSize` vẫn là `900×600`; breakpoint nhỏ vẫn được giữ để điều khiển ổn định khi control được nhúng hoặc DPI scaling lớn.
- Kích thước thẻ được tính từ chiều rộng khả dụng, không dùng tọa độ X cố định.

## Component `NavigationTile`

Thêm `Controls/NavigationTile.cs` dưới dạng control có semantics của button:

- Thuộc tính: `Title`, `Description`, `AccentColor`, `TargetTag`.
- Sự kiện chuẩn `Click`; không tự biết hoặc tự tạo màn hình đích.
- Default: surface trắng, border một pixel.
- Hover: border teal và surface nhẹ; không scale hoặc dịch chuyển layout.
- Pressed: surface selected, màu chữ giữ contrast.
- Focus: focus ring teal xuất hiện ngay.
- Disabled: giảm nhấn mạnh, không thể click.
- `TabStop = true`, `AccessibleRole = PushButton`, `AccessibleName` ghép từ tiêu đề và mô tả.
- Enter và Space kích hoạt giống click chuột.
- Hit target tối thiểu 44 px; thẻ thực tế cao khoảng 108–120 px.

## Luồng điều hướng

### Dùng chung trong `FormMain`

Tách logic hiện tại trong `MenuButton_Click` thành hàm điều hướng dùng chung `NavigateTo(string tag)`:

- `Dashboard` → `FormDashboard`.
- `Sách` → `FormSach`.
- `Phiếu mượn` → `FormPhieuMuon`.
- `Phiếu trả` → `FormPhieuTra`.
- `Độc giả` → `FormDocGia`.
- `Thủ thư` → `FormNhanVien`, chỉ khi `Session.IsAdmin`.
- `Danh mục` → `FormDanhMuc`.
- `Báo cáo` → `FormBaoCao`.
- `Thể loại` → `FormTheLoai`.
- `Tác giả` → `FormTacGia`.
- `Nhà xuất bản` → `FormNhaXuatBan`.

Menu click và launcher click đều gọi cùng hàm này. `FormDanhMuc` phát `NavigationRequested` với tag, không thao tác trực tiếp trên `Parent.Controls`.

### Trạng thái active của menu

- Với `Sách`, `Phiếu mượn`, `Phiếu trả`, `Độc giả`, `Thủ thư` và `Báo cáo`: active đúng button có tag tương ứng.
- Với `Thể loại`, `Tác giả` và `Nhà xuất bản`: active button `Danh mục`.
- Nếu yêu cầu mở `Thủ thư` từ phiên không phải Admin, không tạo màn hình và giữ nguyên trang hiện tại.
- Tag không hợp lệ không được làm trắng `pnlContent`; ghi Debug và giữ nguyên màn hình.

## Xử lý lỗi

- Trang launcher chỉ chứa dữ liệu tĩnh nên không gọi database khi dựng UI.
- Lỗi tải dữ liệu vẫn do màn hình đích xử lý như hiện tại.
- Điều hướng phải tạo control đích thành công trước khi dispose màn hình đang hiển thị; nếu constructor thất bại, giữ màn hình hiện tại và hiển thị thông báo thân thiện.
- Không hiển thị thẻ không có quyền truy cập.

## Accessibility và keyboard

- Thứ tự Tab theo thứ tự nhóm và thứ tự thẻ từ trái sang phải, trên xuống dưới.
- Heading nhóm có `AccessibleRole.Grouping` hoặc label rõ nghĩa.
- Tile có `AccessibleName` đầy đủ; mũi tên chỉ là trang trí và không tạo focus riêng.
- Enter/Space mở tile; focus ring không bị cắt khi thẻ nằm sát biên.
- Meaning không dựa riêng vào màu accent; tiêu đề và mô tả luôn hiện rõ.

## File thay đổi

### Sửa

- `QuanLyThuVien/Forms/FormDanhMuc.cs` — bố cục nhóm, responsive và phát yêu cầu điều hướng.
- `QuanLyThuVien/Forms/FormMain.cs` — gom luồng điều hướng, đồng bộ trạng thái active và quyền Admin.
- `QuanLyThuVien/QuanLyThuVien.csproj` — xóa duy nhất tham chiếu compile tuyệt đối tới `C:\Users\steal\Downloads\DataAccess.cs`; giữ `Data/DataAccess.cs` trong project.
- `design.md` — bổ sung allowance `Danh mục · Grouped Launcher`.
- `.hallmark/log.json` — ghi lần redesign trang Danh mục.

### Thêm

- `QuanLyThuVien/Controls/NavigationTile.cs`.

### Xóa

- Không xóa file nào. File trong thư mục Downloads không bị sửa hoặc xóa; chỉ bỏ liên kết sai khỏi `.csproj`.

## Kiểm thử và nghiệm thu

- Build toàn solution bằng `dotnet build QuanLyThuVien.slnx --configuration Release`.
- Build phải đạt 0 lỗi và 0 cảnh báo.
- Chạy toàn bộ test hiện có bằng `dotnet test QuanLyThuVien.slnx --configuration Release --no-build`.
- Kiểm tra trực quan ở `1280×780`, `1000×600`, `900×600`.
- Xác nhận không có scroll ngang, thẻ không bị cắt, heading nhóm không đè nội dung.
- Click từng tile và xác nhận mở đúng form.
- Xác nhận active menu đúng quy tắc cấp cao/cấp con.
- Xác nhận phiên Admin thấy tile Thủ thư; phiên không phải Admin không thấy và không thể mở route này.
- Kiểm tra Tab, Enter, Space, focus, hover, pressed và disabled.
- Xác nhận các thao tác CRUD, mượn/trả và báo cáo không đổi hành vi.

## Tiêu chí hoàn thành

- Trang Danh mục không còn cảm giác chỉ có ba thẻ nằm ở góc trên trái.
- Tất cả chức năng đã thống nhất trong phạm vi xuất hiện đúng nhóm và điều hướng đúng.
- Menu trên phản ánh chính xác trang đang hiển thị.
- UI dùng cùng Library Teal, typography, spacing và shape với phần còn lại của ứng dụng.
- Solution build sạch và test hiện có đều đạt.
