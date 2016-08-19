using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;
using System.IO;

namespace WinFormsTCP_Server.SettingsForm
{
    public partial class SettingsForm : Form
    {
        public SettingsForm(string ip_address, int port)
        {
            //Form1 frm1 = new Form1();
            InitializeComponent();
            textBoxIPAddress.Text = ip_address;
            textBoxPort.Text = port.ToString();
            this.Text = "Параметры";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            TcpClient client = new TcpClient();

            NetworkStream ns;

            string ip_address = "10.11.2.189";
            int port = 11000;

        //client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000)); // локальный
        client.Connect(new IPEndPoint(IPAddress.Parse(ip_address), port)); // ноута
            string str;
            StreamWriter sw = new StreamWriter(client.GetStream());
            sw.AutoFlush = true;
            //str = textBoxIPAddress.Text+"shitshitshitshit";
            //sw.WriteLine(str);
            str = textBoxPort.Text + "shitshitshitshit";
            sw.WriteLine(str);
            client.Close();
        }
    }
}
