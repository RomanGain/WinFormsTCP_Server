using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Globalization;


namespace WinFormsTCP_Server
{
    public partial class Form1 : Form
    {
        TcpListener listner = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000));
        public Form1()
        {
            InitializeComponent();
            notifyIcon1.Visible = true;
            //timer1.Enabled = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            listner.Start();
            Thread myThread = new Thread(someFunc);
            myThread.IsBackground = true;
            myThread.Start();
        }

        public void someFunc()
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; // ненадежная фигня :/
            while (true)
            {
                try
                {
                    TcpClient client = listner.AcceptTcpClient();
                    //label1.Text = client.Connected.ToString();
                    StreamReader sr = new StreamReader(client.GetStream());
                    string varFromClient = sr.ReadLine();
                    int index = listView1.Items.Add(varFromClient).Index;
                    listView1.Items[index].SubItems.Add(DateTime.Now.ToString());
                    condition(varFromClient);
                    client.Close();
                }
                catch (System.IO.IOException io)
                {
                    MessageBox.Show("IOException:\n" + io.ToString());
                }
            }
        }

        public void condition(string sendedVar)
        {
            int value = Convert.ToInt32(sendedVar);
            if (value % 5 != 0)
                this.notifyIcon1.Icon = new Icon("..\\Debug\\flameBlue.ico");
            else
                this.notifyIcon1.Icon = new Icon("..\\Debug\\flameRed.ico");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {

        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
