using System;
using System.Security.Cryptography;

namespace NextWave.Erp.Restaurant
{
    public static class RestaurantPinHasher
    {
        private const int Iterations = 210000;

        public static string Hash(string pin)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, 32);
            return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string stored, string supplied, out bool wasLegacy)
        {
            wasLegacy = false;
            if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(supplied))
                return false;

            var parts = stored.Split('$');
            if (parts.Length == 4 && parts[0] == "pbkdf2-sha256" &&
                int.TryParse(parts[1], out var iterations) && iterations is >= 10000 and <= 1000000)
            {
                try
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    var expected = Convert.FromBase64String(parts[3]);
                    if (salt.Length < 16 || expected.Length != 32)
                        return false;
                    var actual = Rfc2898DeriveBytes.Pbkdf2(supplied, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch (Exception exception) when (exception is FormatException or ArgumentException)
                {
                    return false;
                }
            }

            wasLegacy = true;
            var legacyExpected = System.Text.Encoding.UTF8.GetBytes(stored.Trim());
            var legacyActual = System.Text.Encoding.UTF8.GetBytes(supplied.Trim());
            return legacyExpected.Length == legacyActual.Length &&
                   CryptographicOperations.FixedTimeEquals(legacyExpected, legacyActual);
        }
    }
}
