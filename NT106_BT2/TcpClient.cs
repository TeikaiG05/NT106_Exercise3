using System;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NT106_BT2
{
    internal static class TcpHelper
    {
        private static string Host => ConfigurationManager.AppSettings["ServerHost"] ?? "127.0.0.1";
        private static int Port
        {
            get { int p; return int.TryParse(ConfigurationManager.AppSettings["ServerPort"], out p) ? p : 8080; }
        }

        public static async Task<string> SendLineAsync(string line)
        {
            using (var cli = new TcpClient())
            {
                await cli.ConnectAsync(Host, Port);
                using (var ns = cli.GetStream())
                using (var wr = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                using (var rd = new StreamReader(ns, new UTF8Encoding(false)))
                {
                    await wr.WriteLineAsync(line);             
                    string resp = await rd.ReadLineAsync();
                    return resp;
                }
            }
        }
    }
}
