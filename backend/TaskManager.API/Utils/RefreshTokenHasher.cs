using System.Security.Cryptography;
using System.Text;

namespace TaskManager.API.Utils
{
    public static class RefreshTokenHasher
    {
        public static string Hash(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }

        public static bool Matches(string providedToken, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            var providedHash = Encoding.UTF8.GetBytes(Hash(providedToken));
            var expectedHash = Encoding.UTF8.GetBytes(storedHash);

            return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
        }
    }
}
