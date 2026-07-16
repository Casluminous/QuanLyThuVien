using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuanLyThuVien.Helpers
{
    public sealed class BookImageStorage
    {
        private const string AssetPrefix = "Sach/";

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif"
        };

        private readonly string _assetDirectory;

        public BookImageStorage(string? applicationBaseDirectory = null)
        {
            string baseDirectory = string.IsNullOrWhiteSpace(applicationBaseDirectory)
                ? AppContext.BaseDirectory
                : applicationBaseDirectory;

            _assetDirectory = Path.Combine(baseDirectory, "Images", "Sach");
        }

        public string ImportFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidOperationException("Chưa chọn ảnh bìa.");

            if (!File.Exists(sourcePath))
                throw new InvalidOperationException("Không tìm thấy tệp ảnh đã chọn.");

            string extension = Path.GetExtension(sourcePath);
            if (!SupportedExtensions.Contains(extension))
                throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");

            ValidateImage(sourcePath);
            Directory.CreateDirectory(_assetDirectory);

            string fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            string destinationPath = Path.Combine(_assetDirectory, fileName);

            try
            {
                File.Copy(sourcePath, destinationPath, overwrite: false);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Không thể sao chép ảnh bìa vào thư mục ứng dụng.", ex);
            }

            return AssetPrefix + fileName;
        }

        public string SaveCroppedImage(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            Directory.CreateDirectory(_assetDirectory);
            string fileName = $"{Guid.NewGuid():N}.jpg";
            string destinationPath = Path.Combine(_assetDirectory, fileName);

            try
            {
                image.Save(destinationPath, ImageFormat.Jpeg);
            }
            catch (Exception ex) when (ex is ExternalException || ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Không thể lưu ảnh bìa đã cắt.", ex);
            }

            return AssetPrefix + fileName;
        }

        public string? ResolvePath(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return null;

            string value = storedValue.Trim();
            if (Path.IsPathRooted(value))
                return File.Exists(value) ? value : null;

            return TryGetLocalAssetPath(value, out string? localPath) && File.Exists(localPath)
                ? localPath
                : null;
        }

        public Image? LoadImage(string? storedValue)
        {
            string? path = ResolvePath(storedValue);
            if (path == null)
                return null;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var original = Image.FromStream(stream);
                return new Bitmap(original);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void DeleteLocalAsset(string? assetKey)
        {
            if (!TryGetLocalAssetPath(assetKey, out string? localPath))
                return;

            try
            {
                if (File.Exists(localPath))
                    File.Delete(localPath);
            }
            catch (Exception)
            {
                // Cleanup is best-effort and must not hide the original save failure.
            }
        }

        private static void ValidateImage(string sourcePath)
        {
            try
            {
                using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var image = Image.FromStream(stream);
                using var bitmap = new Bitmap(image);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ExternalException || ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Tệp đã chọn không phải là ảnh hợp lệ.", ex);
            }
        }

        private bool TryGetLocalAssetPath(string? assetKey, out string? localPath)
        {
            localPath = null;
            if (string.IsNullOrWhiteSpace(assetKey) || Path.IsPathRooted(assetKey))
                return false;

            string normalizedKey = assetKey.Trim().Replace('\\', '/');
            if (!normalizedKey.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string fileName = normalizedKey[AssetPrefix.Length..];
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || Path.GetFileName(fileName) != fileName)
                return false;

            string rootPath = Path.GetFullPath(_assetDirectory);
            string candidate = Path.GetFullPath(Path.Combine(rootPath, fileName));
            string rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            localPath = candidate;
            return true;
        }
    }
}
