# Cấu hình Trợ lý thư viện

## 1. Chuẩn bị máy chủ

Cài .NET 10 Hosting Bundle trên máy chủ Windows có thể kết nối SQL Server và truy cập `api.openai.com`. Tạo AD group chứa người dùng được phép dùng trợ lý, ví dụ `DOMAIN\QLTV-Users`.

Tài khoản chạy ChatApi chỉ cần quyền đọc danh mục:

```sql
CREATE LOGIN [DOMAIN\QLTVChatSvc] FROM WINDOWS;
USE QuanLyThuVien;
CREATE USER [DOMAIN\QLTVChatSvc] FOR LOGIN [DOMAIN\QLTVChatSvc];
GRANT SELECT ON dbo.Sach TO [DOMAIN\QLTVChatSvc];
GRANT SELECT ON dbo.TacGia TO [DOMAIN\QLTVChatSvc];
GRANT SELECT ON dbo.TheLoai TO [DOMAIN\QLTVChatSvc];
GRANT SELECT ON dbo.NhaXuatBan TO [DOMAIN\QLTVChatSvc];
```

## 2. Biến môi trường của ChatApi

Thiết lập trên máy chủ hoặc trong cấu hình IIS, không đưa giá trị bí mật vào Git:

```text
OPENAI_API_KEY=<project-api-key>
Chat__Model=gpt-5.6-terra
Chat__AdGroup=DOMAIN\QLTV-Users
ConnectionStrings__QuanLyThuVien=Server=SQLSERVER;Database=QuanLyThuVien;Trusted_Connection=True;TrustServerCertificate=True;
```

`QLTV_CHAT_DB_CONNECTION` có thể thay cho `ConnectionStrings__QuanLyThuVien` nếu hạ tầng triển khai đang dùng tên biến này.

## 3. Build và chạy

```powershell
dotnet publish .\QuanLyThuVien.ChatApi\QuanLyThuVien.ChatApi.csproj --configuration Release --output .\publish\ChatApi
```

Host thư mục publish bằng IIS hoặc Windows Service, bật Windows Authentication, tắt Anonymous Authentication và gắn chứng chỉ HTTPS nội bộ. Kiểm tra `GET /health` trước khi phát hành cho máy trạm.

## 4. Cấu hình WinForms

Cập nhật `ChatApiBaseUrl` trong file config triển khai của ứng dụng:

```xml
<add key="ChatApiBaseUrl" value="https://qltv-chat.domain.local" />
```

Máy trạm phải đăng nhập bằng tài khoản Windows thuộc AD group đã cấu hình. Không chép `OPENAI_API_KEY` sang máy trạm.
