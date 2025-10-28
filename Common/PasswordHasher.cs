using System.Security.Cryptography;
using System.Text;

namespace Common
{
    public static class PasswordHasher
    {
        public static string Sha256Hex(string s)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new byte[0];
            var len = hex.Length / 2;
            var buf = new byte[len];
            for (int i = 0; i < len; i++)
                buf[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return buf;
        }
    }
}
