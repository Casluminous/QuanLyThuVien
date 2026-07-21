# Thiết kế cải tổ UI WinForms responsive cho QuanLyThuVien

## Mục tiêu

Cải tổ toàn bộ giao diện WinForms để cửa sổ không cần full-screen vẫn hiển thị đầy đủ nội dung và thao tác. Trọng tâm là loại bỏ bố cục tọa độ/kích thước cố định, chuẩn hóa bảng dữ liệu và hộp thoại, đồng thời giữ lại mô hình bảng quen thuộc của ứng dụng.

## Phạm vi

Bao gồm:

- Khung chính `FormMain`: thanh điều hướng, vùng phiên đăng nhập, nút cửa sổ và vùng nội dung.
- Component dùng chung `ModernDataGridView` và các helper layout nếu cần.
- Các màn hình danh sách: dashboard, sách, độc giả, nhân viên, tác giả, nhà xuất bản, thể loại, danh mục, báo cáo.
- Màn hình mượn/trả đã có chức năng mới: phiếu mượn, trả sách, sửa phiếu, sửa tiền phạt.
- Các hộp thoại nhập/chỉnh sửa có nút hành động cố định và vùng cuộn.
- Kiểm tra trực quan ở kích thước 1280x780 và 1000x600.

Không bao gồm:

- Thay đổi schema hoặc luồng nghiệp vụ mượn–trả đã được chốt trong spec riêng.
- Đổi framework, thay WinForms bằng web/mobile.
- Thêm thư viện UI bên ngoài nếu các control hiện tại đáp ứng được.
- Thiết kế lại dữ liệu, truy vấn hoặc quyền người dùng ngoài những điều cần thiết để hiển thị UI.

## Nguyên tắc thiết kế

1. **Bảng là trung tâm**: giữ cách làm việc hiện tại, nhưng cho phép cuộn ngang/dọc khi không đủ chiều rộng.
2. **Không mất thao tác**: các cột `Chi tiết`, `Sửa`, `Xóa`, `Trả sách`, `Sửa phạt` có chiều rộng tối thiểu/cố định và không bị ép về 0.
3. **Dock-first**: vùng nội dung dùng `Dock`, `Anchor`, `TableLayoutPanel`, `FlowLayoutPanel`; tránh tính toán kiểu `Width - n` cho control chính.
4. **Một hệ thống layout**: các form danh sách và hộp thoại dùng cùng spacing, font, màu, bán kính nút và quy tắc trạng thái.
5. **Co giãn có kiểm soát**: form có `MinimumSize`; vùng dài dùng `AutoScroll`/scrollbar; nút hành động nằm ở vùng cuối rõ ràng.
6. **Trạng thái không chỉ dùng màu**: trạng thái luôn có chữ; màu chỉ hỗ trợ phân biệt.
7. **Không phá workflow**: tên nút, luồng mở chi tiết và luồng xác nhận giữ tương thích với logic hiện tại.

## Kiến trúc UI đề xuất

### 1. FormMain

- Giữ thanh trên cùng, nhưng chia thành ba vùng độc lập: logo, menu cuộn/co giãn, cụm phiên người dùng + nút cửa sổ.
- Menu đặt trong `FlowLayoutPanel` có `AutoScroll=true`; không cho menu đè lên nút cửa sổ.
- Nút cửa sổ nằm trong panel neo phải, kích thước hit target tối thiểu 36px.
- Khi cửa sổ hẹp, tên phiên có thể rút gọn hoặc ẩn, nhưng nút cửa sổ vẫn luôn hiển thị.
- `pnlContent` dùng `Dock=Fill` và chỉ chứa một control màn hình hiện tại.

### 2. ModernDataGridView

- Bật cuộn ngang/dọc và cho phép người dùng thay đổi chiều rộng cột.
- Tắt tự co giãn dòng theo nội dung để chiều cao ổn định.
- Cột dữ liệu có `MinimumWidth` phù hợp; cột thao tác dùng `AutoSizeMode=DisplayedCells` hoặc chiều rộng cố định.
- Giữ double buffering, hàng xen kẽ và selection state; tăng tương phản header và dòng đang chọn.
- Cung cấp helper để form cấu hình nhóm cột: dữ liệu, trạng thái, thao tác.

### 3. Form danh sách

- Cấu trúc chuẩn: tiêu đề + mô tả ngắn, thanh công cụ hành động, vùng bảng `Dock=Fill`.
- Thanh công cụ có thể wrap hoặc cuộn khi nhiều nút.
- Bảng không tự đặt `Size` theo chiều rộng parent trong event `Resize`.
- Các cột thao tác đặt ở cuối và được giữ visible.

### 4. Hộp thoại

- Dùng `ClientSize`, `MinimumSize`, `FormBorderStyle=Sizable` ở form dài.
- Nội dung đặt trong panel cuộn; phần nút đặt trong footer `Dock=Bottom`.
- Footer có `AcceptButton` và `CancelButton`, khoảng cách tối thiểu 8px.
- Bố cục biểu mẫu dùng `TableLayoutPanel` để label/control tự giãn theo chiều rộng.
- Bảng chọn sách dùng vùng `Dock=Fill` phía trên footer, không che khuất nút.

## Áp dụng theo màn hình

- `FormPhieuMuon`: bảng có `Chi tiết`, `Sửa`, `Sửa phạt`; hộp thoại mượn/sửa dùng layout cuộn và footer cố định.
- `FormPhieuTra`: bảng có cột `Trả sách`; hộp thoại chọn sách, tiền phạt và footer luôn nhìn thấy.
- `FormSach`, `FormDocGia`, `FormNhanVien`, `FormTacGia`, `FormNhaXuatBan`, `FormTheLoai`: dùng layout danh sách chuẩn và hộp thoại đồng nhất.
- `FormDanhMuc`, `FormBaoCao`, `FormDashboard`: bố trí card/biểu đồ theo panel co giãn, tránh cắt nội dung khi thu nhỏ.

## Tương thích và xử lý lỗi

- Không thay đổi API DataAccess hoặc schema trong phạm vi UI.
- Nếu container quá nhỏ, hiển thị scrollbar thay vì cắt control.
- Không cho phép nút thao tác bị disabled giả hoặc bị che bởi control khác.
- Giữ thông báo lỗi hiện tại; đặt message gần vùng thao tác nếu hộp thoại có thể cuộn.

## Kiểm thử và nghiệm thu

1. Build project ở cấu hình Debug, không có lỗi biên dịch.
2. Mở từng màn hình ở 1280x780 và 1000x600.
3. Kiểm tra thanh điều hướng không che nút cửa sổ.
4. Kiểm tra bảng có thể cuộn ngang/dọc và các nút thao tác vẫn bấm được.
5. Mở/đóng các hộp thoại thêm/sửa ở kích thước nhỏ; footer và nút `Lưu/Hủy` luôn thấy được.
6. Kiểm tra keyboard: Tab đi theo thứ tự hợp lý, Enter xác nhận, Esc hủy.
7. Không thay đổi dữ liệu nghiệp vụ khi chỉ resize hoặc cuộn UI.

## Kết quả mong đợi

Ở cửa sổ không full-screen, người dùng vẫn nhìn thấy vùng nội dung chính, có thể cuộn để xem cột còn lại, và luôn có thể tiếp cận các nút sửa/trả/lưu/hủy. Các màn hình có cảm giác thuộc cùng một hệ thống thay vì mỗi form một cách bố trí riêng.
