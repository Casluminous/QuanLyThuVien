# Code review hien tai - QuanLyThuVien

- Ngay review: 2026-07-17
- Branch/commit: `master` / `407367b`
- Pham vi: source C#, cau hinh, schema SQL va migration hien tai
- Ket qua: 14 phat hien da xac minh (2 cao, 9 trung binh, 3 thap)
- Build: thanh cong, 0 error va 33 warning

Bao cao nay la lan khao sat thu hai. Chi cac van de co tinh huong loi cu the hoac rui ro co bang chung trong code moi duoc giu lai. Cac nhan dinh sai/khong du bang chung cua lan review truoc nam o cuoi file.

Lenh kiem tra: `dotnet build QuanLyThuVien.slnx --no-restore`. Warning `CS8602` tai `FormPhieuTra.cs:104` xac nhan duong code parse tien phat co the dereference gia tri null. Cac warning con lai chu yeu la nullable field initialization (`CS8618`), nullability override (`CS8765`), bien exception khong dung (`CS0168`) va package pruning (`NU1510`).

## Phat hien

### 1. Them va sua tac gia luon truy cap sai ten cot

- Muc do: **Cao**
- Vi tri: `QuanLyThuVien/Data/DataAccess.cs:167-173`
- Doi chieu schema: `QuanLyThuVien/database.sql:27`, `QuanLyThuVien/Database/migration_2026-07-15-001.sql:19-24`
- Van de: schema hien tai dung cot `QuocTich`, migration cung doi `QuocTia` thanh `QuocTich`, nhung `InsertTacGia` va `UpdateTacGia` van dung `QuocTia`.
- Anh huong: thao tac them/sua tac gia nem loi `Invalid column name 'QuocTia'` tren ca database tao moi va database da migrate.
- Sua toi thieu: doi hai cau SQL sang `QuocTich`; doi ten tham so C# `quocTia` thanh `quocTich` de tranh tai dien.

### 2. Database seed tao tai khoan co mat khau mac dinh cong khai

- Muc do: **Cao**
- Vi tri: `QuanLyThuVien/database.sql:99-102`
- Van de: script tao `admin/admin` va `nhanvien1/admin` bang SHA-256 khong salt. `PasswordHelper` van chap nhan hash legacy nay.
- Anh huong: ai biet cau hinh mac dinh co the dang nhap Admin ngay sau khi deploy.
- Sua toi thieu: bo hai tai khoan seed va de `FormFirstRun` tao Admin. Neu bat buoc seed, dung mat khau ngau nhien PBKDF2 va bat buoc doi o lan dang nhap dau.

### 3. Migration khong dam bao `TenDangNhap` duy nhat

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Database/migration_2026-07-15-001.sql:88-93`
- Doi chieu: `QuanLyThuVien/database.sql:11`, `QuanLyThuVien/Data/DataAccess.cs:85-90`
- Van de: schema moi co `UNIQUE`, nhung migration chi tao index thuong. Database cu co the ton tai nhieu nhan vien cung username; dang nhap lay `Rows[0]` khong co thu tu xac dinh.
- Anh huong: mat khau dung van co the bi tu choi, hoac he thong xac thuc nham ban ghi.
- Sua toi thieu: phat hien/xu ly username trung, sau do tao `UNIQUE INDEX UX_NhanVien_TenDangNhap`.

### 4. Check va xoa/vo hieu hoa Admin khong atomic

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Forms/FormNhanVien.cs:128-142`, `QuanLyThuVien/Forms/FormNhanVien.cs:209-221`
- Doi chieu: `QuanLyThuVien/Data/DataAccess.cs:141-145`
- Van de: `CountActiveAdmins()` va thao tac `DeleteNhanVien`/`UpdateNhanVien` la cac lenh rieng, khong cung transaction. Check vo hieu hoa admin cung chi ap dung cho tai khoan dang dang nhap; Admin A co the vo hieu hoa Admin B cuoi cung ma khong bi chan.
- Anh huong: hai phien Admin dong thoi, hoac mot Admin sua Admin khac, co the lam he thong khong con Admin active.
- Sua toi thieu: dua invariant "luon con it nhat mot Admin active" vao mot method transaction o DataAccess, lock cac ban ghi Admin va thuc hien check + update/delete trong cung transaction.

### 5. Nang cap hash legacy co the lam dang nhap hop le that bai va lo chi tiet noi bo

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Data/DataAccess.cs:91-100`, `QuanLyThuVien/Forms/FormDangNhap.cs:196-214`
- Van de: sau khi verify SHA-256 thanh cong, he thong bat buoc `UPDATE` sang PBKDF2. Neu tai khoan SQL co quyen SELECT nhung mat quyen UPDATE, dang nhap hop le van that bai. UI hien nguyen `ex.Message` cho nguoi chua xac thuc.
- Anh huong: loi quyen/khoa database tai buoc rehash tu choi login va co the lo server, database, SQL detail.
- Sua toi thieu: rehash theo best-effort va log noi bo; login UI chi hien thong bao chung.

### 6. Cau hinh loi bi nuot va ung dung am tham dung database fallback

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Data/DataAccess.cs:11-36`
- Van de: hai `catch` rong bo qua loi config, sau do dung `Server=.\SQLEXPRESS;Database=QuanLyThuVien`. `Console.WriteLine` thuong khong hien trong WinForms.
- Anh huong: app co the doc/sua nham database cung ten ma user khong biet.
- Sua toi thieu: bo fallback silent; nem `ConfigurationErrorsException` voi thong bao ro rang va ghi chi tiet vao log.

### 7. Luong tao phieu muon co the crash khi nhap so luong khong phai so

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Forms/FormPhieuMuon.cs:193-209`, `QuanLyThuVien/Forms/FormPhieuMuon.cs:234-240`
- Van de: cot `SoLuongMuon` cho phep edit tu do va handler goi `Convert.ToInt32` ben ngoai `try/catch`. Nguoi dung co the nhap `abc`, chuoi rong hoac so qua lon.
- Anh huong: nhan "Tao phieu" co the nem `FormatException`/`OverflowException` tren UI thread.
- Sua toi thieu: dung `DataGridView.CellValidating`, chi chap nhan integer duong; parse bang `int.TryParse` trong handler.

### 8. So luong muon khong duoc validate tai bien DataAccess

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Data/DataAccess.cs:305-375`
- Doi chieu schema: `QuanLyThuVien/database.sql:81`, constraint chi duoc them boi `migration_2026-07-15-001.sql:74-78`
- Van de: `InsertPhieuMuonFull` khong reject danh sach rong, `soLuong <= 0`, hoac `maSach` trung. Database tao moi bang `database.sql` khong co check constraint `SoLuong > 0`; voi so am, `SoLuong=SoLuong-@sl` lai tang ton kho va insert chi tiet am. Danh sach trung va so luong duong se va cham primary key sau khi da update stock, tuy transaction rollback duoc.
- Anh huong: caller khac ngoai UI co the tao phieu rong hoac lam sai ton kho tren schema tao moi.
- Sua toi thieu: validate `chiTiet.Count > 0`, tat ca so luong > 0 va `MaSach` duy nhat truoc khi mo transaction; dua check constraints vao `database.sql` de schema moi va migration dong nhat.

### 9. Tinh/parse tien phat phu thuoc chuoi hien thi va locale

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Forms/FormPhieuTra.cs:79-105`
- Van de: gia tri decimal duoc format thanh text `N0 + "d"`, sau do parse nguoc bang cach xoa dau phay. Tren locale dung dau cham ngan cach hang nghin, `10.000d` co the khong parse duoc hoac bi hieu thanh gia tri khac.
- Anh huong: click "Tra sach" co the crash hoac tinh sai tien phat theo regional settings.
- Sua toi thieu: luu decimal goc trong cell an/`Tag`, chi format cot hien thi; khong parse nguoc UI text.

### 10. Anh thumbnail sach khong duoc dispose khi tai lai catalog

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Forms/FormSach.cs:178-207`, `QuanLyThuVien/Controls/BookCardControl.cs:38`
- Van de: moi card so huu image tao boi `GetThumbnailImage`, nhung setter `CoverImage` khong dispose image cu va `BookCardControl` khong dispose `_coverImage`. `pnlCatalog.Controls.Clear()` chi remove control; no khong bao dam dispose cac control da remove.
- Anh huong: them/sua/xoa sach goi `LoadData` nhieu lan se tich luy GDI/image handles, cuoi cung co the gay loi `OutOfMemoryException`/"A generic error occurred in GDI+".
- Sua toi thieu: override `Dispose(bool)` trong `BookCardControl` de dispose `_coverImage`; dispose cac card cu truoc `Controls.Clear()`.

### 11. Custom rounded controls tao `Region` moi moi lan paint ma khong dispose region cu

- Muc do: **Trung binh**
- Vi tri: `QuanLyThuVien/Controls/BookCardControl.cs:79-82`, `CardPanel.cs:29-32`, `StatCard.cs:54-57`, `RoundedPanel.cs:38-50`
- Van de: moi repaint gan `Region = new Region(...)`. Region cu khong duoc dispose truoc khi thay. `RoundedPanel` con tao `GraphicsPath` tai line 49 ma khong dispose.
- Anh huong: resize/hover/repaint lien tuc lam tang GDI handles.
- Sua toi thieu: cap nhat region trong `OnResize`, dispose region cu truoc khi gan; wrap moi `GraphicsPath` trong `using`.

### 12. Mot so loi tai du lieu bi an hoan toan

- Muc do: **Thap**
- Vi tri: `QuanLyThuVien/Forms/FormSach.cs:273-285`, `QuanLyThuVien/Forms/FormPhieuMuon.cs:142-151`, `QuanLyThuVien/Forms/FormBaoCao.cs:113,142`
- Van de: cac `catch { }` lam UI hien bang/chart rong nhu the khong co du lieu.
- Anh huong: user khong phan biet database loi voi danh sach rong; viec chan doan kho.
- Sua toi thieu: log exception va hien thong bao chung, khong hien raw SQL exception.

### 13. Man hinh dang nhap khong gioi han so lan thu

- Muc do: **Thap**
- Vi tri: `QuanLyThuVien/Forms/FormDangNhap.cs:185-214`
- Van de: moi click goi truy van/xac thuc ngay, khong delay, rate limit hay khoa tam thoi.
- Anh huong: co the brute-force tai khoan, dac biet nguy hiem khi script con username/mat khau mac dinh.
- Sua toi thieu: gioi han theo username va cua so thoi gian, tang delay hoac khoa tam thoi sau nhieu lan that bai.

### 14. `TrustServerCertificate=True` khong phu hop neu database chay tu xa

- Muc do: **Thap**
- Vi tri: `QuanLyThuVien/App.config:4`, `QuanLyThuVien/Data/DataAccess.cs:36`
- Van de: client bo qua xac minh chung chi SQL Server.
- Anh huong: rui ro thap voi `localhost`, nhung lam suy yeu bao ve MITM neu doi server sang may tu xa.
- Sua toi thieu: tach config dev/prod; production dung chung chi hop le va `TrustServerCertificate=False`.

## Nen cai thien sau khi sua loi chinh

1. Xoa hoac thu hep visibility cua cac API khong co call site: `InsertPhieuMuon`, `TraSach`, `CapNhatTrangThaiPhieuMuon` tai `DataAccess.cs:273,389,470`. Chung de tao state khong day du neu bi dung nham, nhung hien tai chua phai bug runtime vi khong co caller.
2. Dung `using` cho `SqlDataAdapter` tai `DataAccess.cs:52`. Day la cleanup dung chuan, khong phai loi connection: `SqlDataAdapter.Fill` tu mo va dong connection neu ban dau connection dang dong.
3. Validate email/so dien thoai va dung `DateTime.Now.Year` cho gioi han nam xuat ban. Day la cai thien UX/data quality, khong phai loi nghiep vu da xac minh.
4. Them automated tests cho `PasswordHelper`, validation phieu muon va transaction muon/tra. Du an hien khong co test project.

## Cac nhan dinh lan truoc da loai

### `TOP {top}` la SQL injection Critical

Khong dung. `top` co kieu `int`, nen input khong the chen SQL text. Nen validate `top > 0` de tranh SQL error, nhung khong xep day la SQL injection.

### `SqlDataAdapter.Fill` thieu `conn.Open()` la loi High

Khong dung. `Fill` tu mo connection dang dong va dong lai sau khi xong. Van nen dispose adapter, nhung khong co loi ket noi vi thieu `Open()`.

### `NhanVien.MatKhau` dang lam lo hash qua session

Chua co bang chung. `DataAccess.DangNhap` khong gan `MatKhau` vao object tra ve, va code hien tai khong serialize/log object session. Co the xoa property de giam be mat rui ro, nhung khong phai lo credential hien tai.

### Session khong clear khi logout

Hien tai khong co luong logout quay lai login. Khi `FormMain` dong, `FormDangNhap` cung dong va process ket thuc, nen chua co bug session tai su dung. Can xu ly neu sau nay them logout.

### Placeholder mat khau cua `FormNhanVien` bi nhan la mat khau moi

Khong dung voi implementation hien tai. `ModernTextBox.Text` tra chuoi rong khi placeholder active. Tuy nhien chuoi placeholder dang duoc gan nhu text thuc, nen code so sanh literal van mong manh va nen thay bang state/placeholder dung nghia.

### Moi `Font` gan cho control deu bi leak

Khong nen ket luan hang loat. Control/Form disposal thuong dispose cac tai nguyen font do control so huu; can profile GDI handles de khang dinh. Cac leak image/region o tren co ownership ro rang hon va da duoc giu lai.

### `FormDanhMuc.LoadSub` chac chan leak UserControl

Chua du bang chung sau khi doi chieu navigation. `FormMain.LoadForm` dispose control cu khi chuyen menu. Can test navigation cu the truoc khi coi la leak.

## Thu tu sua de nghi

1. Sua `QuocTia` thanh `QuocTich` va them test CRUD tac gia.
2. Bo tai khoan seed mac dinh.
3. Sua migration username unique.
4. Dua admin invariant vao transaction.
5. Validate input/DataAccess cua phieu muon va bo parse tien phat tu text.
6. Sua image/Region disposal.
7. Chuan hoa error handling va bo raw exception tren login.
