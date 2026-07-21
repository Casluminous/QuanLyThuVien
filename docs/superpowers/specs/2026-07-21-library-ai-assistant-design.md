# Trợ lý AI thư viện — thiết kế

## Mục tiêu

Thêm trợ lý AI tiếng Việt vào ứng dụng WinForms QuanLyThuVien. Trợ lý xuất hiện bằng nút nổi ở góc phải dưới `FormMain`, mở panel bên phải và tồn tại khi người dùng chuyển trang. Phiên bản đầu chỉ đọc danh mục sách để tìm kiếm, kiểm tra tồn kho và gợi ý sách; không đọc dữ liệu độc giả/phiếu mượn và không thay đổi database.

## Kiến trúc

- Thêm `QuanLyThuVien.ChatApi` (ASP.NET Core Minimal API) và `QuanLyThuVien.Chat.Contracts` (DTO/event dùng chung) vào solution.
- WinForms chỉ gọi ChatApi qua HTTPS và Windows Authentication; API key OpenAI chỉ nằm trên máy chủ backend.
- ChatApi dùng OpenAI Responses API qua thư viện .NET chính thức, model mặc định `gpt-5.6-terra`, reasoning `low`, streaming, `store: false`.
- ChatApi bảo vệ endpoint bằng Windows Authentication và nhóm AD cấu hình. Tài khoản dịch vụ SQL chỉ có quyền đọc bốn bảng danh mục.
- Công cụ mô hình được giới hạn ở `search_catalog` và `get_book_detail`; mọi truy vấn đều tham số hóa, giới hạn tối đa 10 sách.

## Hợp đồng API

`POST /api/chat/stream` nhận `ChatRequest` gồm `SessionId`, `Message` và tối đa 8 lượt `HistoryItem`. Phản hồi `text/event-stream` gồm các loại `delta`, `books`, `done`, `error`. `GET /health` kiểm tra cấu hình, SQL và trạng thái dịch vụ.

Giới hạn mặc định: câu hỏi 1.000 ký tự, khoảng 700 token phản hồi, 10 yêu cầu/phút/người, tối đa 2 yêu cầu đồng thời và timeout 60 giây. Không lưu nội dung hội thoại hay API key vào log.

## Giao diện

- `FormMain` sở hữu `ChatPanel` độc lập với `pnlContent` để panel không bị hủy khi chuyển trang.
- Panel rộng khoảng 400px; khi cửa sổ hẹp panel phủ nội dung thay vì làm co bảng.
- Dùng Library Teal, control native/custom hiện có, focus ring rõ, nhãn accessibility, nút đóng và trạng thái loading/error/retry/cancel.
- `Enter` gửi, `Shift+Enter` xuống dòng, `Esc` đóng. Lịch sử chỉ lưu trong bộ nhớ của phiên ứng dụng.
- Kết quả sách hiển thị thẻ bấm được; bấm thẻ chuyển tới Kho sách và mở chi tiết đúng `MaSach`.

## Bảo mật và dữ liệu

- Không đưa API key vào WinForms, `App.config` hoặc Git.
- Không gửi dữ liệu độc giả, nhân viên, phiếu mượn/trả cho OpenAI.
- Hash định danh Windows khi log/safety identifier; log chỉ mã lỗi, độ trễ và usage.
- Rate limit và giới hạn kích thước request được áp dụng trước khi gọi OpenAI.

## Kiểm thử và nghiệm thu

- Unit test validation, giới hạn history, catalog query và SSE parser.
- Integration test endpoint với OpenAI client giả lập, auth, rate limit, cancellation và lỗi mạng.
- Kiểm tra UI ở 1280×780, 1000×600 và 900×600; keyboard/focus/accessibility; thẻ sách mở đúng chi tiết.
- Build toàn solution bằng `dotnet build --configuration Release`.

