using System;
using System.Text;

namespace Sql.Utils
{
    public static class IdEncoder
    {
        private const string Salt = "SHOP_2025"; 

        public static string Encode(int id)
        {
            var raw = $"{Salt}:{id}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                          .TrimEnd('=')
                          .Replace('+', '-')
                          .Replace('/', '_');
        }

        public static bool TryDecode(string encoded, out int id)
        {
            id = 0;
            try
            {
                var base64 = encoded.Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                var parts = raw.Split(':');
                if (parts.Length != 2 || parts[0] != Salt) return false;
                return int.TryParse(parts[1], out id);
            }
            catch
            {
                return false;
            }
        }

        // 🔥 Convenience wrapper so callers can use Decode(...) -> int? instead of the out-param pattern
        public static int? Decode(string encoded)
        {
            return TryDecode(encoded, out var id) ? id : null;
        }
    }
}