using System.Globalization;
using System.Security.Cryptography;

namespace ms.users.application.Security
{
    /// <summary>
    /// PBKDF2-HMAC-SHA256 with a random salt per password. Stored as pbkdf2-sha256$iterations$salt$hash (Base64).
    /// </summary>
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        public const string Scheme = "pbkdf2-sha256";
        public const int Iterations = 600_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return string.Join('$', Scheme, Iterations.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public bool Verify(string password, string storedValue)
        {
            if (password == null || !TryParse(storedValue, out var iterations, out var salt, out var expected))
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        public bool IsProtected(string storedValue) => TryParse(storedValue, out _, out _, out _);

        private static bool TryParse(string storedValue, out int iterations, out byte[] salt, out byte[] hash)
        {
            iterations = 0;
            salt = hash = null;
            var parts = storedValue?.Split('$');
            return parts is { Length: 4 }
                && parts[0] == Scheme
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
                && iterations > 0
                && TryDecode(parts[2], out salt)
                && TryDecode(parts[3], out hash);
        }

        private static bool TryDecode(string value, out byte[] bytes)
        {
            var buffer = new byte[value.Length];
            if (value.Length > 0 && Convert.TryFromBase64String(value, buffer, out var written))
            {
                bytes = buffer[..written];
                return true;
            }

            bytes = null;
            return false;
        }
    }
}
