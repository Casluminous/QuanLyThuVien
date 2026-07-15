using System.Security.Cryptography;
using System.Text;

namespace QuanLyThuVien.Helpers
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algo, HashSize);
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (storedHash.StartsWith("PBKDF2$"))
                return VerifyPbkdf2(password, storedHash);

            // Legacy SHA-256 fallback
            return VerifyLegacySha256(password, storedHash);
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            try
            {
                string[] parts = storedHash.Split('$');
                if (parts.Length != 4) return false;

                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algo, expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool VerifyLegacySha256(string password, string storedHash)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            string computed = sb.ToString();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(storedHash));
        }
    }
}