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
            try
            {
                ChangeIPAtServer();
                this.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangeIPAtServer() //Функция изменения IP адреса передачи у dcCore 
        {
            TcpClient client = new TcpClient();

            NetworkStream ns;

            //string ip_address = "10.11.2.189";
            //int port = 11000;

            string ip_address = textBoxIPAddress.Text;
            int port = 11000;

            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12000)); // локальный

            string str;
            StreamWriter sw = new StreamWriter(client.GetStream());
            sw.AutoFlush = true;
            //str = textBoxIPAddress.Text+"shitshitshitshit";
            //sw.WriteLine(str);
            str = ip_address;
            sw.WriteLine(str);
            client.Close();



            //client.Connect(new IPEndPoint(IPAddress.Parse(ip_address), port)); // ноута

            
        }
    }
}
