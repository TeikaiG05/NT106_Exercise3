using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Common;
using Newtonsoft.Json;

namespace NT106_BT2
{
    public partial class Dashboard : Form
    {
        private readonly string email;
        public Dashboard(string firstname, string surname, string birthday, string gender, string email)
        {
            InitializeComponent();
            cName.Text= firstname + " " + surname;
            cBirthday.Text= birthday;
            cEmail.Text= email;

        }

        private void lb_profile_Click(object sender, EventArgs e)
        {
            if (pn_profile.Visible == true)
            {
                lb_profile.Checked = false;
                pn_profile.Visible = false;
            }
            else
            {
                pn_profile.Visible = true;
            }
        }

        private void Close_profile_Click(object sender, EventArgs e)
        {
            pn_profile.Visible = false;
        }
        #region Logout
        private async void btnLogout_Click(object sender, EventArgs e)
        {
            try
            {
                var req = new LogoutReq { username = Session.Email, token = Session.Token };
                string json = JsonConvert.SerializeObject(req);
                await TcpHelper.SendLineAsync(json);
            }
            catch
            {}
            Session.Clear();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        #endregion
    }
}
