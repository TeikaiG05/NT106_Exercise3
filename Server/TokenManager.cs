using System;
using System.Collections.Concurrent;

namespace Server
{
    internal static class TokenManager
    {
        private static readonly ConcurrentDictionary<string, (string user, DateTime exp)> store = new ConcurrentDictionary<string, (string, DateTime)>();

        private static readonly TimeSpan ttl = TimeSpan.FromDays(7);

        public static (string token, DateTime exp) Issue(string username)
        {
            var token = Guid.NewGuid().ToString("N");
            var exp = DateTime.UtcNow.Add(ttl);
            store[token] = (username, exp);
            return (token, exp);
        }

        public static bool Validate(string username, string token)
        {
            if (token == null) return false;
            if (store.TryGetValue(token, out var v) == false) return false;
            if (!string.Equals(v.user, username, StringComparison.OrdinalIgnoreCase)) return false;
            if (DateTime.UtcNow > v.exp) { store.TryRemove(token, out _); return false; }
            return true;
        }

        public static void Invalidate(string username, string token)
        {
            if (token == null) return;
            if (store.TryGetValue(token, out var v) &&
                string.Equals(v.user, username, StringComparison.OrdinalIgnoreCase))
            {
                store.TryRemove(token, out _);
            }
        }
    }
}
