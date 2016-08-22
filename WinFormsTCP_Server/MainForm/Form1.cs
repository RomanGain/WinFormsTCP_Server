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
using System.ServiceProcess;

using log4net;
using log4net.Config;

using WinFormsTCP_Server.SettingsForm;

namespace WinFormsTCP_Server
{
    public partial class Form1 : Form
    {
        //private static ILog someLOg;
        CreateLogClass cls = new CreateLogClass();
        static string local_host = System.Net.Dns.GetHostName();
        static string local_ipAddress = Dns.GetHostByName(local_host).AddressList[0].ToString(); // устаревшее?
        //static string ip_address = System.Net.Dns.GetHostName();
            //"127.0.0.1";
        static int port = 11000;
        TcpListener listner = new TcpListener(new IPEndPoint(IPAddress.Parse(local_ipAddress), port));
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

                    if (checkMark(varFromClientEnd))
                    {
                        int index = listView1.Items.Add(listCounter.ToString()).Index; // Добавление ID в listView
                        listView1.Items[index].SubItems.Add(varFromClient_onlyCommand); // Добавление Сообщения в listView 
                        listView1.Items[index].SubItems.Add(DateTime.Now.ToString()); // Добавление времени в listView 
                        listView1.Items[index].SubItems.Add(addedCondition(varFromClient, varFromClient_onlyCommand)); // Добавление Типа сообщения в listView 
                        condition(varFromClientEnd, varFromClient_onlyCommand, index, ref countErrors, ref countExceptions);
                        listView1.Items[index].SubItems.Add(ProgramCondition(varFromClientBegin)); // Добавление приложения в listView 
                        ++listCounter;
                    }
                    else
                    {
                        addedCondition(varFromClient, varFromClient_onlyCommand);
                    }
                    client.Close();
                }
                catch (System.IO.IOException io)
                {
                    MessageBox.Show("IOException:\n" + io.ToString());
                }
            }
        }

        //---------------------------Проверка условия----------------------------------------------

        public void condition(string varFromClientEnd, string varFromClient_onlyCommand, int index, ref int countErrors, ref int countExceptions)
        {
            switch (varFromClientEnd)
            {
                case "ftl":
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\delete1");
                    listView1.Items[index].BackColor = Color.Coral;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Фатальная ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "err":
                    ++countErrors;
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\delete1.ico");
                    listView1.Items[index].BackColor = Color.Coral;

                    //contextMenuStrip1.Show();
                    contextMenuStrip1.Items.Add(new ToolStripSeparator());
                    contextMenuStrip1.Items.Add(varFromClient_onlyCommand);
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "wrn":
                    ++countExceptions;
                    lblExceptionsValue.Text = countExceptions.ToString();
                    this.notifyIcon1.Icon = new Icon("..\\Debug\\icons\\alert.ico");
                    listView1.Items[index].BackColor = Color.SandyBrown;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon1.BalloonTipTitle = "Предупреждение";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                default:
                    //this.notifyIcon1.Icon = WinFormsTCP_Server.Properties.Resources();
                        //new Icon("success.ico");
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
                    return "Инфо";
                    break;
                case "wrn":
                    cls.Warning(varFromClient_onlyCommand);
                    return "Предупреждение";
                    break;
                case "err":
                    cls.Error(varFromClient_onlyCommand);
                    return "Ошибка";
                    break;
                case "dbg":
                    cls.Debug(varFromClient_onlyCommand);
                    return "Отладочное сообщение";
                    break;
                case "ftl":
                    cls.Fatal(varFromClient_onlyCommand);
                    return "Фатальная ошибка";
                    break;
                default:
                    cls.Info(varFromClient_onlyCommand);
                    return "Неизвестно";
                    break;
            }
        }

        public bool checkMark(string varFromClientEnd)
        {
            switch (varFromClientEnd)
            {
                case "inf":
                    if (informMessagesToolStripMenuItem.Checked == true)
                        return true;
                    else
                        return false;
                    break;
                case "wrn":
                    if (warningsToolStripMenuItem.Checked == true)
                        return true;
                    else
                        return false;
                    break;
                default:
                    return true;
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
                    ProgramTitle = "Неизвестно";
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

            //listView1.Items.Add("shit");


            //if (this.WindowState == FormWindowState.Normal)
            //    MessageBox.Show("It's normal");
            //else if (this.WindowState == FormWindowState.Minimized)
            //    MessageBox.Show("It's minimized");
            //else if (this.WindowState == FormWindowState.Maximized)
            //    MessageBox.Show("It's maximized");

            //MessageBox.Show(local_host);
            //MessageBox.Show(local_ipAddress);

            try
            {
                ServiceController sc = new ServiceController("Report Manager32");
                MessageBox.Show(sc.Status.ToString());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e) // Двойной клик по элементу списка 
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
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
            }
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e) // Клик по инф. облаку в трее 
        {
            if (this.WindowState == FormWindowState.Minimized)
            this.WindowState = FormWindowState.Normal;
        }

        private void параметрыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm.SettingsForm sf = new SettingsForm.SettingsForm(local_ipAddress, port);
            sf.ShowDialog();

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }


        //---------------------------------------- Формат -------------------------------

        private void errorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (errorsToolStripMenuItem.Checked == true)
                errorsToolStripMenuItem.Checked = false;
            else
                errorsToolStripMenuItem.Checked = true;
        }

        private void warningsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (warningsToolStripMenuItem.Checked == true)
                warningsToolStripMenuItem.Checked = false;
            else
                warningsToolStripMenuItem.Checked = true;
        }

        private void informMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (informMessagesToolStripMenuItem.Checked == true)
                informMessagesToolStripMenuItem.Checked = false;
            else
                informMessagesToolStripMenuItem.Checked = true;
        }

        private void timerCheckStatus_Tick(object sender, EventArgs e)
        {

        }
        //-------------------------------------------------------------------------------
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
