# Spec: Tính năng vận hành thư viện cho local/demo

## Mục tiêu

Bổ sung các tính năng giúp nhân viên vận hành nhanh hơn trong một thư viện nhỏ: tìm kiếm và lọc dữ liệu, xem lịch sử độc giả, gia hạn phiếu mượn, theo dõi thu tiền phạt, in/xuất phiếu và cảnh báo trên dashboard.

Phạm vi là ứng dụng WinForms chạy local với SQL Server hiện tại. Không triển khai chatbot, barcode, đặt trước sách, đồng bộ giữa máy hoặc phân quyền server trong spec này. Các luồng mượn/trả, tiền đền sách mất và Library Teal hiện có được giữ nguyên.

## Nguyên tắc nghiệp vụ

- Mọi thao tác thay đổi dữ liệu phải dùng SQL tham số hóa và transaction phù hợp.
- Không cho gia hạn phiếu đã trả hết; hạn mới không được sớm hơn hạn hiện tại.
- Tiền phạt tiếp tục tính theo từng cuốn: số ngày quá hạn × mức phạt/ngày/cuốn × số lượng mượn. Tiền đền sách mất tiếp tục lấy từ `Sach.GiaTien`.
- Tổng tiền phải thu được tính từ tiền phạt và tiền đền; số đã thu và còn lại được lấy từ lịch sử thanh toán, không sửa ngược giá trị gốc.
- Chỉ Admin đang hoạt động mới được sửa tiền phạt theo quy tắc hiện tại. Nhân viên được ghi nhận thanh toán nếu có quyền thao tác trả sách.
- Các danh sách dùng mặc định an toàn: phiếu mới nhất trước, phiếu quá hạn được đưa lên đầu, không thay đổi dữ liệu chỉ vì sắp xếp/lọc.

## Chức năng và giao diện

### 1. Tìm kiếm, lọc và sắp xếp

Thêm vùng bộ lọc nhất quán phía trên các bảng:

- `Kho sách`: từ khóa tên sách/ISBN/tác giả/NXB, thể loại, còn sách trong kho.
- `Độc giả`: từ khóa họ tên/SĐT/email, trạng thái thẻ còn hiệu lực/hết hạn.
- `Mượn trả`: mã phiếu/độc giả, trạng thái, khoảng ngày mượn, quá hạn.
- `Trả sách`: mã phiếu/độc giả, còn sách chưa trả, quá hạn.

Ô tìm kiếm có nút xóa nhanh và cập nhật khi nhấn Enter hoặc thay đổi bộ lọc. Bảng hỗ trợ sort khi bấm header; cột số và ngày sort theo kiểu dữ liệu thật, không sort theo chuỗi hiển thị. Khi không có kết quả, hiển thị trạng thái rỗng có hướng dẫn xóa bộ lọc.

Để phù hợp dữ liệu local/demo, dữ liệu có thể tải theo truy vấn có tham số và lọc tại SQL; không thêm thư viện phân trang bên ngoài. Các giá trị sort được giới hạn bằng whitelist cột, không ghép tên cột tùy ý từ input.

### 2. Lịch sử độc giả

Thêm action `Lịch sử` ở bảng độc giả. Dialog lịch sử có:

- Thông tin tóm tắt độc giả và trạng thái thẻ.
- Danh sách phiếu: mã phiếu, ngày mượn, hạn trả, ngày trả cuối, trạng thái.
- Chi tiết sách trong phiếu: tên sách, số lượng, kết quả trả, tiền phạt, tiền đền.
- Các số tổng: số phiếu, số sách đã mượn, tổng phạt/đền, đã thu và còn lại.

Dialog chỉ đọc, có nút `In lịch sử` và đóng bằng `Esc`. Nếu độc giả không có lịch sử, hiển thị empty state thay vì bảng trống.

### 3. Gia hạn phiếu mượn

Thêm action `Gia hạn` ở danh sách/chi tiết phiếu mượn khi trạng thái là `Đang mượn` hoặc `Đã trả một phần`.

- Dialog hiển thị hạn hiện tại, hạn mới mặc định bằng hạn hiện tại cộng 14 ngày.
- Cho phép chọn ngày mới bằng DateTimePicker, không nhận ngày nhỏ hơn hạn hiện tại.
- Nếu phiếu đã `Đã trả`, nút bị disable và có tooltip giải thích.
- Cập nhật `PhieuMuon.HanTra` trong transaction, sau đó tải lại bảng và dashboard.

Không giới hạn số lần gia hạn trong phiên bản local/demo. Nhật ký audit không thuộc phạm vi của đợt này.

### 4. Theo dõi thu tiền phạt

Thêm bảng `ThanhToanPhat` bằng migration tương thích database hiện tại:

```sql
MaThanhToan INT IDENTITY PRIMARY KEY
MaPhieuMuon INT NOT NULL FOREIGN KEY REFERENCES PhieuMuon(MaPhieuMuon)
SoTien DECIMAL(18,2) NOT NULL CHECK (SoTien > 0)
NgayThu DATETIME2 NOT NULL DEFAULT SYSDATETIME()
MaNV INT NOT NULL FOREIGN KEY REFERENCES NhanVien(MaNV)
GhiChu NVARCHAR(250) NOT NULL DEFAULT N''
```

Thêm index theo `MaPhieuMuon` và `NgayThu`. Khi ghi nhận thanh toán:

- Tải tổng phải thu từ chi tiết phiếu và tổng đã thu trong transaction có khóa dòng.
- Từ chối số tiền lớn hơn số còn lại hoặc nhỏ hơn/equal 0.
- Cho phép nhiều lần thu, không cho xóa lịch sử thanh toán từ giao diện thường.
- Hiển thị trạng thái `Chưa thu`, `Thu một phần`, `Đã thu đủ`.
- Phiên bản này chỉ hỗ trợ thêm giao dịch thu tiền; không hỗ trợ sửa/xóa giao dịch đã ghi nhận.

Form trả sách và chi tiết phiếu mượn hiển thị tổng phải thu, đã thu, còn lại và nút `Thu tiền`. Dashboard có cảnh báo tổng khoản còn chưa thu.

### 5. In và xuất dữ liệu

Không thêm package UI bên ngoài.

- `In phiếu mượn`: mã phiếu, độc giả, nhân viên, ngày mượn, hạn trả, danh sách sách.
- `In phiếu trả`: sách đã trả/mất, ngày trả, phạt, tiền đền, đã thu/còn lại.
- `In lịch sử độc giả`: phần tóm tắt và bảng lịch sử.
- `Xuất CSV`: các bảng sách, độc giả, phiếu mượn, phiếu trả và báo cáo đang lọc.

Dùng `PrintDocument`/`PrintPreviewDialog` của WinForms. CSV dùng UTF-8 có BOM, escape đúng dấu phẩy, dấu ngoặc kép và xuống dòng để mở được bằng Excel. Tên file mặc định gồm loại báo cáo và timestamp.

### 6. Cảnh báo Dashboard

Chuẩn hóa khu vực `Cần xử lý` thành các card có số lượng và action:

- Phiếu quá hạn.
- Độc giả sắp hết hạn thẻ trong 30 ngày.
- Sách sắp hết, ngưỡng mặc định `SoLuong <= 2`.
- Khoản phạt còn chưa thu.

Mỗi card có thể bấm để mở trang liên quan với bộ lọc tương ứng. Nếu không có cảnh báo, dùng trạng thái thành công rõ ràng. Truy vấn dashboard phải dùng cùng các điều kiện với bảng nghiệp vụ để số liệu không lệch.

## Tổ chức code

- Tái sử dụng `PageHeader`, `ModernTextBox`, `ModernComboBox`, `ModernButton` và `ModernDataGridView` hiện có.
- Tạo helper dùng chung cho filter/sort và CSV export, tránh copy logic giữa các form.
- Tách truy vấn lịch sử, gia hạn và thanh toán vào `DataAccess`, không đặt SQL trong event handler.
- Tất cả dialog mới đặt `AcceptButton`, `CancelButton`, `TabIndex`, `AccessibleName`, focus vào trường đầu tiên và hỗ trợ `Enter`/`Esc`.
- Không cho panel lọc làm co hoặc đẩy bảng ra ngoài tại kích thước `900×600`.

## Kiểm thử và nghiệm thu

### Database/DataAccess

- Migration tạo `ThanhToanPhat` idempotent và chạy được trên database mới/lẫn database hiện có.
- Tính tổng phải thu/đã thu/còn lại đúng với nhiều lần thanh toán.
- Từ chối thanh toán âm, bằng 0, vượt số còn lại; transaction rollback khi lỗi.
- Gia hạn bị từ chối với phiếu đã trả hoặc ngày mới không hợp lệ.
- Lịch sử độc giả không lộ dữ liệu của độc giả khác.
- Filter dùng tham số; sort chỉ nhận whitelist.

### UI

- Tìm kiếm/lọc/sort ở bốn bảng không làm lệch cột hoặc tạo dòng thụt xen kẽ.
- Dialog lịch sử, gia hạn, thu tiền và in không cắt nút tại `1280×780`, `1000×600`, `900×600`.
- Keyboard navigation, focus, Enter/Esc và trạng thái disabled hoạt động.
- CSV mở đúng tiếng Việt trong Excel; Print Preview hiển thị đủ nội dung.

### Regression

- Tạo/sửa/xóa sách, độc giả, tác giả, NXB, thể loại vẫn hoạt động.
- Tạo/sửa phiếu mượn, trả từng phần, đánh dấu mất sách và sửa phạt không đổi hành vi.
- Dashboard và báo cáo cũ vẫn tải được khi không có dữ liệu.
- `dotnet build QuanLyThuVien.slnx --configuration Release` không warning/error.

## Ngoài phạm vi

- Chatbot AI.
- Barcode/QR và quản lý từng bản sao vật lý.
- Đặt trước sách hoặc danh sách chờ.
- Đồng bộ qua mạng, web/mobile client và phân quyền AD.
- Thanh toán online.

## Tiêu chí hoàn thành

Phương án được xem là hoàn thành khi nhân viên có thể tìm đúng phiếu/độc giả trong vài thao tác, xem toàn bộ lịch sử độc giả, gia hạn phiếu hợp lệ, ghi nhận nhiều lần thu phạt không vượt số phải thu, in/xuất các phiếu chính và xử lý cảnh báo trực tiếp từ dashboard; toàn bộ luồng cũ vẫn build và chạy không thay đổi nghiệp vụ ngoài các bổ sung trên.
