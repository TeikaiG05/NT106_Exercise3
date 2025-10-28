using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Common;

namespace Server
{
    internal sealed class TcpServer
    {
        private readonly Action<string, string> log;
        private TcpListener lis;

        public TcpServer() : this(null) { }

        public TcpServer(Action<string, string> logger)
        {
            log = logger;
        }

        private void Log(string src, string msg)
        {
            if (log != null) log(src, msg);
        }

        public void Start(int port)
        {
            lis = new TcpListener(IPAddress.Any, port);
            lis.Start();
            Log("Server", $"Listening on 0.0.0.0:{port}");
            _ = AcceptLoop();
        }

        public void Stop()
        {
            try { lis?.Stop(); Log("Server", "Stopped"); } catch { }
        }

        private async Task AcceptLoop()
        {
            while (true)
            {
                TcpClient cli;
                try { cli = await lis.AcceptTcpClientAsync(); }
                catch { break; }
                var ep = cli.Client.RemoteEndPoint != null ? cli.Client.RemoteEndPoint.ToString() : "client";
                Log("Accept", ep);
                _ = Task.Run(() => Handle(cli, ep));
            }
        }

        private async Task Handle(TcpClient cli, string ep)
        {
            try
            {
                using (cli)
                using (var ns = cli.GetStream())
                using (var rd = new StreamReader(ns, new UTF8Encoding(false)))
                using (var wr = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                {
                    string line;
                    while ((line = await rd.ReadLineAsync()) != null)
                    {
                        Log(ep, "recv: " + line);

                        string type = null;
                        try
                        {
                            var env = JsonConvert.DeserializeObject<Envelope>(line);
                            type = env != null ? env.type : null;
                        }
                        catch
                        {
                            await SendErr(wr, "JSON không hợp lệ"); Log(ep, "send: ERROR json"); continue;
                        }

                        if (type == MsgType.LOGIN)
                        {
                            LoginReq login = null;
                            try { login = JsonConvert.DeserializeObject<LoginReq>(line); }
                            catch { await SendErr(wr, "LOGIN: dữ liệu không hợp lệ"); Log(ep, "send: ERROR login"); continue; }

                            var r = Db.FindByLogin(login.username, login.passwordHash); // username = email
                            if (r.HasValue)
                            {
                                var v = r.Value;
                                var user = new UserDto
                                {
                                    username = login.username,
                                    fullName = (v.Firstname + " " + v.Surname).Trim(),
                                    email = v.Email,
                                    birthday = v.Birthday.HasValue ? v.Birthday.Value.ToString("yyyy-MM-dd") : null
                                };

                                var issued = TokenManager.Issue(login.username); // << cấp token
                                await SendOk(wr, MsgType.LOGIN, "OK", user, issued.token, issued.exp);
                                Log(ep, "send: OK login (token)");
                            }

                            else
                            {
                                await SendErr(wr, "Sai email/mật khẩu"); Log(ep, "send: ERROR wrong creds");
                            }
                            continue;
                        }

                        if (type == MsgType.REGISTER)
                        {
                            RegisterReq reg = null;
                            try { reg = JsonConvert.DeserializeObject<RegisterReq>(line); }
                            catch { await SendErr(wr, "REGISTER: dữ liệu không hợp lệ"); Log(ep, "send: ERROR register json"); continue; }

                            if (Db.UsernameExists(reg.email))
                            {
                                await SendErr(wr, "Email đã tồn tại"); Log(ep, "send: ERROR email exists");
                            }
                            else
                            {
                                DateTime? bd = null;
                                DateTime d;
                                if (!string.IsNullOrWhiteSpace(reg.birthday) && DateTime.TryParse(reg.birthday, out d))
                                    bd = d;

                                string fn = "", sn = "";
                                if (!string.IsNullOrWhiteSpace(reg.fullName))
                                {
                                    var parts = reg.fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    fn = parts.FirstOrDefault() ?? "";
                                    sn = parts.Length > 1 ? parts[parts.Length - 1] : fn;
                                }

                                Db.InsertUser(fn, sn, bd, "Other", reg.email, reg.passwordHash);
                                await SendOk(wr, MsgType.REGISTER, "Registered", null); Log(ep, "send: OK register");
                            }
                            continue;
                        }
                        if (type == MsgType.LOGOUT)
                        {
                            LogoutReq lo = null;
                            try { lo = JsonConvert.DeserializeObject<LogoutReq>(line); }
                            catch { await SendErr(wr, "LOGOUT: dữ liệu không hợp lệ"); Log(ep, "send: ERROR logout json"); continue; }

                            TokenManager.Invalidate(lo.username, lo.token); // << thu hồi
                            await SendOk(wr, MsgType.LOGOUT, "Logged out", null);
                            Log(ep, "send: OK logout");
                            continue;
                        }
                        if (type == MsgType.LOGIN_WITH_TOKEN)
                        {
                            TokenLoginReq treq = null;
                            try { treq = JsonConvert.DeserializeObject<TokenLoginReq>(line); }
                            catch { await SendErr(wr, "TOKEN LOGIN: dữ liệu không hợp lệ"); Log(ep, "send: ERROR token login json"); continue; }

                            if (TokenManager.Validate(treq.username, treq.token))
                            {
                                var r = Db.GetByEmail(treq.username);
                                if (r.HasValue)
                                {
                                    var v = r.Value;
                                    var user = new UserDto
                                    {
                                        username = treq.username,
                                        fullName = (v.Firstname + " " + v.Surname).Trim(),
                                        email = v.Email,
                                        birthday = v.Birthday.HasValue ? v.Birthday.Value.ToString("yyyy-MM-dd") : null
                                    };

                                    var issued = TokenManager.Issue(treq.username); // làm mới token
                                    await SendOk(wr, MsgType.LOGIN, "OK", user, issued.token, issued.exp);
                                    Log(ep, "send: OK token login");
                                }
                                else
                                {
                                    await SendErr(wr, "Không tìm thấy người dùng");
                                }
                            }
                            else
                            {
                                await SendErr(wr, "Token không hợp lệ hoặc đã hết hạn");
                            }
                            continue;
                        }
                        await SendErr(wr, "Yêu cầu không hợp lệ"); Log(ep, "send: ERROR unknown type");
                    }
                }
                Log(ep, "disconnected");
            }
            catch (Exception ex)
            {
                Log(ep, "error: " + ex.Message);
            }

        }
        private static Task SendOk(StreamWriter wr, string type, string message, UserDto user, string token, DateTime exp) => wr.WriteLineAsync(JsonConvert.SerializeObject(new OkRes { ok = true, type = type, message = message, user = user, token = token, expires = exp.ToString("o") }));

        private class Envelope { public string type { get; set; } }

        private static Task SendOk(StreamWriter wr, string type, string message, UserDto user)
            => wr.WriteLineAsync(JsonConvert.SerializeObject(new OkRes { ok = true, type = type, message = message, user = user }));

        private static Task SendErr(StreamWriter wr, string error)
            => wr.WriteLineAsync(JsonConvert.SerializeObject(new ErrRes { ok = false, type = "ERROR", error = error }));
    }
}
