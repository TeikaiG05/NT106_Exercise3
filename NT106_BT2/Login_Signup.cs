using Common;
using Guna.UI2.AnimatorNS;
using Guna.UI2.WinForms;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NT106_BT2
{
    public partial class Login_Signup : Form
    {
        private Guna2Transition Guna2Transistion1;

        private static readonly string SessPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"NT106_Exercise3", "session.json");

        private static readonly string[] MONTHS = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private bool initDone = false;
        public Login_Signup()
        {
            InitializeComponent();
            Guna2Transistion1 = new Guna2Transition();
            pn_login.Visible = true;
            pn_regis.Visible = false;
            this.Shown += async (_, __) => await TryAutoLoginAsync();
        }

        #region UI: switch 2 panel
        private void cToLogin_Click(object sender, EventArgs e)
        {
            pn_regis.Visible = false;
            Guna2Transistion1.ShowSync(pn_login);
        }

        private void cSignup_Click(object sender, EventArgs e)
        {
            pn_regis.Visible = true;
            Guna2Transistion1.ShowSync(pn_regis);
        }
        #endregion

        #region Form life-cyclex
        private void Login_Signup_Load(object sender, EventArgs e)
        {
            InitCombos();

            initDone = false; // ĐANG KHỞI TẠO
            tsRemember.Checked = Properties.Settings.Default.RememberMe;
            cUsername.Text = Properties.Settings.Default.SavedEmail ?? "";
            initDone = true;  // KHỞI TẠO XONG
        }
        #endregion

        #region Helpers (validate + ui + session)
        private void InitCombos()
        {
            // Year
            cYear.Items.Clear();
            int currentYear = DateTime.Now.Year;
            for (int y = currentYear; y >= 1950; y--) cYear.Items.Add(y.ToString());
            cYear.MaxDropDownItems = 5; cYear.DropDownHeight = 120; cYear.SelectedIndex = 0;

            // Month
            cMonth.Items.Clear();
            foreach (var m in MONTHS) cMonth.Items.Add(m);
            cMonth.MaxDropDownItems = 5; cMonth.DropDownHeight = 120; cMonth.SelectedIndex = 0;

            // Day
            cDay.Items.Clear();
            for (int d = 1; d <= 31; d++) cDay.Items.Add(d.ToString());
            cDay.MaxDropDownItems = 5; cDay.DropDownHeight = 120; cDay.SelectedIndex = 0;
        }

        private static bool HasDigit(string s) => !string.IsNullOrEmpty(s) && s.Any(char.IsDigit);

        private static bool IsValidEmail(string email)
        {
            try { var addr = new System.Net.Mail.MailAddress(email); return addr.Address == email; }
            catch { return false; }
        }

        private static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            bool up = password.Any(char.IsUpper);
            bool lo = password.Any(char.IsLower);
            bool di = password.Any(char.IsDigit);
            bool sp = password.Any(ch => !char.IsLetterOrDigit(ch));
            return up && lo && di && sp;
        }

        private string GetSelectedGender()
        {
            if (cMale.Checked) return "Male";
            if (cFemale.Checked) return "Female";
            if (cOther.Checked) return "Other";
            return null;
        }

        private void ClearSignupFields()
        {
            cFirstname.Text = cSurname.Text = cEmail.Text = "";
            nw_password.Text = nw_cfpassword.Text = "";
            cMale.Checked = cFemale.Checked = cOther.Checked = false;
            cYear.SelectedIndex = cMonth.SelectedIndex = cDay.SelectedIndex = 0;
        }

        private static void SplitName(string full, out string first, out string sur)
        {
            first = sur = "";
            if (string.IsNullOrWhiteSpace(full)) return;
            var parts = full.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            first = parts.FirstOrDefault() ?? "";
            sur = parts.Length > 1 ? parts[parts.Length - 1] : first;
        }

        private void SaveSessionToDisk(string email, string token)
        {
            try
            {
                var dir = Path.GetDirectoryName(SessPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SessPath, Newtonsoft.Json.JsonConvert.SerializeObject(new { email, token }));
            }
            catch { /* ignore */ }
        }

        private static void SaveRememberToSettings(string email, string token)
        {
            Properties.Settings.Default.SavedEmail = email ?? "";
            Properties.Settings.Default.SavedToken = token ?? "";
            Properties.Settings.Default.RememberMe = true;
            Properties.Settings.Default.Save();
        }

        private static void ClearSavedSession()
        {
            Properties.Settings.Default.SavedEmail = string.Empty;
            Properties.Settings.Default.SavedToken = string.Empty;
            Properties.Settings.Default.RememberMe = false;
            Properties.Settings.Default.Save();
        }

        private DialogResult ShowDashboardModal(string first, string sur, string bday, string gender, string email)
        {
            this.Hide();
            DialogResult dr;
            using (var main = new Dashboard(first, sur, bday, gender, email))
            {
                dr = main.ShowDialog(this);
            }
            if (dr == DialogResult.OK) this.Show(); else this.Close();
            return dr;
        }
        #endregion

        #region Sign up
        private async void bt_signup_Click(object sender, EventArgs e)
        {
            string firstname = cFirstname.Text.Trim();
            string surname = cSurname.Text.Trim();
            string year = cYear.SelectedItem?.ToString();
            string month = cMonth.SelectedItem?.ToString();
            string day = cDay.SelectedItem?.ToString();
            string gender = GetSelectedGender();
            string email = cEmail.Text.Trim();
            string pass = nw_password.Text;
            string conf = nw_cfpassword.Text;

            if (string.IsNullOrWhiteSpace(firstname) || HasDigit(firstname)) { MessageBox.Show("Họ trống hoặc chứa số."); cFirstname.Focus(); return; }
            if (string.IsNullOrWhiteSpace(surname) || HasDigit(surname)) { MessageBox.Show("Tên trống hoặc chứa số."); cSurname.Focus(); return; }

            if (!int.TryParse(year, out int y) || !MONTHS.Contains(month) || !int.TryParse(day, out int d))
            { MessageBox.Show("Vui lòng chọn ngày sinh hợp lệ."); return; }

            int m = Array.IndexOf(MONTHS, month) + 1;
            DateTime birthdayDt;
            try { birthdayDt = new DateTime(y, m, d); }
            catch { MessageBox.Show("Ngày sinh không hợp lệ."); return; }

            if (string.IsNullOrEmpty(gender)) { MessageBox.Show("Vui lòng chọn giới tính."); return; }
            if (!IsValidEmail(email)) { MessageBox.Show("Email không hợp lệ."); cEmail.Focus(); return; }
            if (!IsStrongPassword(pass)) { MessageBox.Show("Mật khẩu phải ≥8 ký tự, có hoa, thường, số, ký tự đặc biệt."); nw_password.Focus(); return; }
            if (pass != conf) { MessageBox.Show("Mật khẩu xác nhận không khớp."); nw_cfpassword.Focus(); return; }

            var hashedPass = PasswordHasher.Sha256Hex(pass);

            var req = new RegisterReq
            {
                type = MsgType.REGISTER,
                username = email,
                email = email,
                passwordHash = hashedPass,
                fullName = (firstname + " " + surname).Trim(),
                birthday = birthdayDt.ToString("yyyy-MM-dd")
            };

            try
            {
                string jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(req);
                string jsonRes = await TcpHelper.SendLineAsync(jsonReq);

                var ok = Newtonsoft.Json.JsonConvert.DeserializeObject<OkRes>(jsonRes);
                var err = ok == null ? Newtonsoft.Json.JsonConvert.DeserializeObject<ErrRes>(jsonRes) : null;

                if (ok != null && ok.ok && ok.type == MsgType.REGISTER)
                {
                    MessageBox.Show("Đăng ký thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cToLogin_Click(sender, e);
                    ClearSignupFields();
                }
                else
                {
                    MessageBox.Show(err != null ? err.error : "Đăng ký thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được server: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Login + Auto-login
        private async void bt_login_Click(object sender, EventArgs e)
        {
            string email = cUsername.Text.Trim();
            string password = cPassword.Text.Trim();

            // Admin (local)
            if (email == "admin" && password == "admin")
            {
                MessageBox.Show("Hello admin", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowDashboardModal("Admin", "User", DateTime.Now.ToString("yyyy-MM-dd"), "Other", "admin@localhost");
                return;
            }

            if (!IsValidEmail(email)) { MessageBox.Show("Email không hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); cUsername.Focus(); return; }
            if (string.IsNullOrEmpty(password)) { MessageBox.Show("Vui lòng nhập mật khẩu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); cPassword.Focus(); return; }

            var req = new LoginReq
            {
                type = MsgType.LOGIN,
                username = email,
                passwordHash = PasswordHasher.Sha256Hex(password)
            };

            try
            {
                string jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(req);
                string jsonRes = await TcpHelper.SendLineAsync(jsonReq);

                var ok = Newtonsoft.Json.JsonConvert.DeserializeObject<OkRes>(jsonRes);
                var err = ok == null ? Newtonsoft.Json.JsonConvert.DeserializeObject<ErrRes>(jsonRes) : null;

                if (ok != null && ok.ok && ok.type == MsgType.LOGIN)
                {
                    var u = ok.user ?? new UserDto();

                    // session (token)
                    Session.Email = u.email;
                    Session.Token = ok.token;
                    Session.Expire = ok.expires;

                    if (tsRemember.Checked)
                    {
                        SaveRememberToSettings(u.email, ok.token);
                        SaveSessionToDisk(u.email, ok.token); // optional demo
                    }
                    else
                    {
                        ClearSavedSession();
                    }

                    SplitName(u.fullName, out var first, out var sur);
                    MessageBox.Show("Login Successfully!", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowDashboardModal(first, sur, u.birthday ?? "", "Other", u.email ?? "");
                }
                else
                {
                    MessageBox.Show(err != null ? err.error : "Login Failed!", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được server: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task TryAutoLoginAsync()
        {
            var remember = Properties.Settings.Default.RememberMe;
            var savedEmail = Properties.Settings.Default.SavedEmail;
            var savedToken = Properties.Settings.Default.SavedToken;

            if (!remember || string.IsNullOrWhiteSpace(savedEmail) || string.IsNullOrWhiteSpace(savedToken))
                return;

            try
            {
                var req = new TokenLoginReq { username = savedEmail, token = savedToken };
                string jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(req);
                string jsonRes = await TcpHelper.SendLineAsync(jsonReq);

                var ok = Newtonsoft.Json.JsonConvert.DeserializeObject<OkRes>(jsonRes);
                if (ok != null && ok.ok && ok.type == Common.MsgType.LOGIN)
                {
                    var u = ok.user ?? new Common.UserDto();

                    Session.Email = u.email;
                    Session.Token = ok.token;
                    Session.Expire = ok.expires;

                    // GHI LẠI token/email mới nếu Remember đang bật
                    if (Properties.Settings.Default.RememberMe)
                    {
                        Properties.Settings.Default.SavedEmail = u.email ?? "";
                        Properties.Settings.Default.SavedToken = ok.token ?? "";
                        Properties.Settings.Default.Save();
                    }

                    SplitName(u.fullName, out var first, out var sur);
                    ShowDashboardModal(first, sur, u.birthday ?? "", "Other", u.email ?? "");
                }
                else
                {
                    ClearSavedSession(); // token cũ hỏng/hết hạn → xoá để khỏi lặp
                }
            }
            catch
            {
                // ignore
            }
        }
        #endregion

        #region Remember toggle
        private void tsRemember_CheckedChanged(object sender, EventArgs e)
        {
            if (!initDone) return;

            Properties.Settings.Default.RememberMe = tsRemember.Checked;

            if (!tsRemember.Checked)
            {
                Session.Clear();
                ClearSavedSession();
            }
            else
            {
                Properties.Settings.Default.Save();
            }
        }
        #endregion
    }
}
