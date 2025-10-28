using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NT106_BT2
{
    internal static class Session
    {
        public static string Email { get; set; }
        public static string Token { get; set; }
        public static string Expire { get; set; }

        public static bool IsLoggedIn
            => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Token);

        public static void Clear()
        {
            Email = null;
            Token = null;
            Expire = null;
        }
    }
}
