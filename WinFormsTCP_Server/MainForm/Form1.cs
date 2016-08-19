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

using log4net;
using log4net.Config;

namespace WinFormsTCP_Server
{
    public partial class Form1 : Form
    {
        //private static ILog someLOg;
        CreateLogClass cls = new CreateLogClass();
        TcpListener listner = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000));
        public Form1()
        {
            InitializeComponent();

            //this.Hide();
            this.WindowState = FormWindowState.Minimized;

            notifyIcon1.Visible = true;
            lblErrorsValue.Text = "0";
            lblExceptionsValue.Text = "0";
            log4net.Config.DOMConfigurator.Configure();

            listner.Start();
            Thread myThread = new Thread(someFunc);
            myThread.IsBackground = true;
            myThread.Start();
        }

        public void someFunc()
        {
            int countErrors = 0, countExceptions = 0, listCounter = 1;
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; // ненадежная фигня :/
            while (true)
            {
                try
                {
                    TcpClient client = listner.AcceptTcpClient();
                    StreamReader sr = new StreamReader(client.GetStream());
                    string varFromClient = sr.ReadLine();
                    
                    //--------------------------
                    string varFromClientBegin = varFromClient.Remove(varFromClient.Length - 3);
                    string varFromClientEnd = varFromClient.Remove(0,varFromClient.Length - 3);
                    string varFromClient_onlyCommand = varFromClientBegin.Remove(varFromClientBegin.Length - 6);
                    //--------------------------

                    int index = listView1.Items.Add(listCounter.ToString()).Index; // Добавление ID в listView
                    listView1.Items[index].SubItems.Add(varFromClient_onlyCommand); // Добавление Сообщения в listView 
                    listView1.Items[index].SubItems.Add(DateTime.Now.ToString()); // Добавление времени в listView 
                    listView1.Items[index].SubItems.Add(addedCondition(varFromClient, varFromClient_onlyCommand)); // Добавление Типа сообщения в listView 
                    condition(varFromClientEnd, varFromClientBegin, index, ref countErrors, ref countExceptions);
                    listView1.Items[index].SubItems.Add(ProgramCondition(varFromClientBegin)); // Добавление приложения в listView 
                    ++listCounter;
                    client.Close();
                }
                catch (System.IO.IOException io)
                {
                    MessageBox.Show("IOException:\n" + io.ToString());
                }
            }
        }

        //---------------------------Проверка условия----------------------------------------------

        public void condition(string varFromClientEnd, string varFromClientBegin, int index, ref int countErrors, ref int countExceptions)
        {
            switch (varFromClientEnd)
            {
                case "ftl":
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\delete1.ico");
                    listView1.Items[index].BackColor = Color.Coral;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Fatal Error";
                    notifyIcon1.BalloonTipText = "Fatal Error text: " + varFromClientBegin;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "err":
                    ++countErrors;
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\delete1.ico");
                    listView1.Items[index].BackColor = Color.Coral;

                    //contextMenuStrip1.Show();
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Error";
                    notifyIcon1.BalloonTipText = "Error text: " + varFromClientBegin;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "wrn":
                    ++countExceptions;
                    lblExceptionsValue.Text = countExceptions.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\alert.ico");
                    listView1.Items[index].BackColor = Color.SandyBrown;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon1.BalloonTipTitle = "Exception";
                    notifyIcon1.BalloonTipText = "Exception text: " + varFromClientBegin;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                default:
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\success.ico");
                    break;
            }
        }

        //------------------------Проверка на тип-----------------------------
        public string addedCondition(string varFromClient, string varFromClient_onlyCommand)
        {
            string endStr = varFromClient.Remove(0, varFromClient.Length - 3);
            switch (endStr)
            {
                case "inf":
                    cls.Info(varFromClient_onlyCommand);
                    return "Info";
                    break;
                case "wrn":
                    cls.Warning(varFromClient_onlyCommand);
                    return "Warning";
                    break;
                case "err":
                    cls.Error(varFromClient_onlyCommand);
                    return "Error";
                    break;
                case "dbg":
                    cls.Debug(varFromClient_onlyCommand);
                    return "Debug";
                    break;
                case "ftl":
                    cls.Fatal(varFromClient_onlyCommand);
                    return "Fatal";
                    break;
                default:
                    return "Unknown";
                    break;
            }
        }
        //-------------------------Определение программы-------------------------

        public string ProgramCondition(string varFromClientBegin)
        {
            string ProgramTitle = varFromClientBegin.Remove(0, varFromClientBegin.Length - 6);

            switch (ProgramTitle)
            {
                case "DcCore":
                    return ProgramTitle;
                    break;
                case "cAdmin":
                    ProgramTitle = "DcAdmin";
                    return ProgramTitle;
                    break;
                default:
                    ProgramTitle = "Unknown";
                    return ProgramTitle;
                    break;
            }
        }
        //-----------------------------------------------------------------------
        private void btnStop_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e) // Тестовая кнопка
        {
            //cls.Error("This is Error12122!");
            //cls.Fatal("This is Fatal!");
            //cls.Info("This is Info!");
            //cls.Warning("This is Warning!");
            //cls.Debug("This is Debug!");

            if (this.WindowState == FormWindowState.Normal)
                MessageBox.Show("It's normal");
            else if (this.WindowState == FormWindowState.Minimized)
                MessageBox.Show("It's minimized");
            else if (this.WindowState == FormWindowState.Maximized)
                MessageBox.Show("It's maximized");
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess();
            MessageBox.Show(System.Diagnostics.Process.GetCurrentProcess().ToString());
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }
    }

    public class CreateLogClass
    {
        public static readonly ILog log = LogManager.GetLogger(typeof(CreateLogClass));

        public void Error(string send_message)
        {
            log.Error(send_message);
        }

        public void Info(string send_message)
        {
            log.Info(send_message);
        }

        public void Debug(string send_message)
        {
            log.Debug(send_message);
        }

        public void Warning(string send_message)
        {
            log.Warn(send_message);
        }

        public void Fatal(string send_message)
        {
            log.Fatal(send_message);
        }
    }
}
