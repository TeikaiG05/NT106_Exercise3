using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : Form
    {
        private TcpServer server;
        public Server()
        {
            InitializeComponent();
            lvLog.View = View.Details;
            lvLog.FullRowSelect = true;
            lvLog.GridLines = true;
            lvLog.Columns.Add("Time", 120);
            lvLog.Columns.Add("Source", 120);
            lvLog.Columns.Add("Message", 600);
            server = new TcpServer(Log);
        }
        private void Log(string source, string message)
        {
            if (lvLog.IsDisposed) return;
            if (lvLog.InvokeRequired)
            {
                lvLog.BeginInvoke(new Action(() => AddRow(source, message)));
            }
            else
            {
                AddRow(source, message);
            }
        }
        private void AddRow(string source, string message)
        {
            var it = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            it.SubItems.Add(source ?? "");
            it.SubItems.Add(message ?? "");
            lvLog.Items.Add(it);
            it.EnsureVisible();
            if (lvLog.Items.Count > 1000) lvLog.Items.RemoveAt(0);
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            server.Start(8080);
            Log("Server", "Started on :8080");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            server.Stop();
            Log("Server", "Stopped");
        }
    }
}
