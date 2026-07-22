# Kế hoạch triển khai: Admin thiết lập trạng thái phiếu mượn

## Phạm vi

Triển khai spec `docs/superpowers/specs/2026-07-23-loan-status-admin-control-design.md`, giữ nguyên schema, UI Library Teal và các luồng mượn/trả hiện có.

## Bước 1 — Model và DataAccess dùng chung

1. Thêm `DemoLoanScenario` enum và `DemoLoanScenarioRequest` model, chỉ nhận các giá trị kịch bản whitelist.
2. Thêm helper kiểm tra actor là Admin đang hoạt động trong transaction.
3. Thêm `SyncPhieuMuonStatus`:
   - khóa header/chi tiết;
   - tính trạng thái từ `NgayTra`;
   - cập nhật duy nhất `PhieuMuon.TrangThai` khi cần;
   - trả lỗi rõ ràng cho phiếu thiếu chi tiết hoặc actor không hợp lệ.
4. Tách phần thao tác transaction của tạo phiếu và trả sách thành helper nội bộ có thể nhận `SqlConnection`/`SqlTransaction`, nhưng giữ nguyên API hiện có.
5. Thêm `CreateDemoLoanScenario`:
   - kiểm tra Admin, độc giả, sách và tồn kho;
   - tạo phiếu bằng cùng invariant của `InsertPhieuMuonFull`;
   - thực hiện bước trả một phần/toàn bộ hoặc đánh dấu mất bằng helper trả sách;
   - rollback toàn bộ nếu bất kỳ bước nào lỗi.

## Bước 2 — UI thiết lập trạng thái

1. Thêm action `Thiết lập trạng thái` vào menu thao tác của `FormPhieuMuon`, chỉ bật với Admin.
2. Tạo dialog dùng các control hiện có, hiển thị trạng thái lưu, trạng thái suy ra, số dòng đã/chưa trả và tổng tiền.
3. Nút `Đồng bộ trạng thái` gọi DataAccess, giữ dialog mở khi lỗi và tải lại bảng khi thành công.
4. Nút `Tạo kịch bản demo` mở dialog chọn độc giả, sách, ngày mượn, hạn trả, kịch bản và số lượng mất nếu cần.
5. Thiết lập `AcceptButton`, `CancelButton`, `TabIndex`, `AccessibleName`, focus đầu tiên, Enter/Esc và footer co giãn.

## Bước 3 — Kiểm thử

1. Thêm unit test cho hàm tính trạng thái từ số dòng đã/chưa trả và whitelist kịch bản.
2. Thêm integration/manual test cho sync header, phân quyền Admin, rollback, tồn kho và tiền đền.
3. Kiểm tra các kịch bản `Đang mượn`, `Quá hạn`, `Đã trả một phần`, `Đã trả`, có sách mất.
4. Kiểm tra đồng thời hai thao tác trên cùng phiếu không làm nhân đôi tồn kho.
5. Chạy `dotnet test QuanLyThuVien.slnx --configuration Release`.
6. Chạy `dotnet build QuanLyThuVien.slnx --configuration Release`; nếu file output bị khóa, dùng thư mục output xác minh riêng.
7. Chạy `git diff --check` và kiểm tra không sửa các thay đổi chưa commit ngoài phạm vi.

## Tệp dự kiến thay đổi

- `QuanLyThuVien/Models/DemoLoanScenarioRequest.cs` (mới)
- `QuanLyThuVien/Data/DataAccess.cs`
- `QuanLyThuVien/Forms/FormPhieuMuon.cs`
- `QuanLyThuVien.ChatApi.Tests` không đổi nghiệp vụ; chỉ thêm test vào project phù hợp nếu có seam test được.
- `docs/superpowers/specs/2026-07-23-loan-status-admin-control-design.md` (đã duyệt)

## Rủi ro và cách giảm thiểu

- Không cho đổi trạng thái bằng chuỗi tự do; mọi thay đổi đi qua transaction.
- Không tự động đoán ngày trả hoặc cộng kho khi đồng bộ header.
- Kịch bản demo tạo phiếu mới, không biến đổi phiếu thật đang sử dụng.
- Giữ API công khai hiện có để tránh ảnh hưởng form mượn/trả và báo cáo.
