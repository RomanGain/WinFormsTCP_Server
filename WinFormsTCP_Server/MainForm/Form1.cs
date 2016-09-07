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
        static int maxNumberOfNotes = 20; // максимальное количество записей в dataSet
        DataSet ds = new DataSet();
        DataTable incomingMessagesTable = new DataTable();


        //static string ip_address = System.Net.Dns.GetHostName();
        //"127.0.0.1";
        static int port = 11000;
        TcpListener listner = new TcpListener(new IPEndPoint(IPAddress.Parse(local_ipAddress), port));


        public Form1()
        {
            InitializeComponent();
            //this.WindowState = FormWindowState.Minimized;

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
            int countErrors = 0, countExceptions = 0; // вывод на форму числа ошибок, исключений
            int listCounter = 1; // счетчик числа сообщений в listView
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; // ненадежная фигня :/

            ds.Tables.Add(incomingMessagesTable);

            DataColumn idColumn = new DataColumn("ID");
            idColumn.AutoIncrement = true;
            idColumn.AutoIncrementSeed = 1;
            idColumn.AutoIncrementStep = 1;

            DataColumn messageColumn = new DataColumn("Message", Type.GetType("System.String"));
            DataColumn dateColumn = new DataColumn("Date", Type.GetType("System.String"));
            DataColumn typeColumn = new DataColumn("Type", Type.GetType("System.String"));
            DataColumn appColumn = new DataColumn("App", Type.GetType("System.String"));

            incomingMessagesTable.Columns.Add(idColumn);
            incomingMessagesTable.Columns.Add(messageColumn);
            incomingMessagesTable.Columns.Add(dateColumn);
            incomingMessagesTable.Columns.Add(typeColumn);
            incomingMessagesTable.Columns.Add(appColumn);

            while (true)
            {
                try
                {
                    TcpClient client = listner.AcceptTcpClient();
                    StreamReader sr = new StreamReader(client.GetStream());
                    string fullVarFromClient = sr.ReadLine();

                    //--------------------------
                    string varFromClientBegin = fullVarFromClient.Remove(fullVarFromClient.Length - 3);
                    string cut_type = fullVarFromClient.Remove(0, fullVarFromClient.Length - 3);
                    string varFromClient_onlyCommand = varFromClientBegin.Remove(varFromClientBegin.Length - 6);
                    //--------------------------
                    if (incomingMessagesTable.Rows.Count >= maxNumberOfNotes)
                    {
                        for (int i = 1; i < maxNumberOfNotes; i++)
                        {
                            //incomingMessagesTable.Rows[i - 1][0] = incomingMessagesTable.Rows[i][0].ToString();
                            incomingMessagesTable.Rows[i - 1][1] = incomingMessagesTable.Rows[i][1].ToString();
                            incomingMessagesTable.Rows[i - 1][2] = incomingMessagesTable.Rows[i][2].ToString();
                            incomingMessagesTable.Rows[i - 1][3] = incomingMessagesTable.Rows[i][3].ToString();
                            incomingMessagesTable.Rows[i - 1][4] = incomingMessagesTable.Rows[i][4].ToString();
                        }

                        //incomingMessagesTable.Rows[maxNumberOfNotes - 1][0] = 6;
                        incomingMessagesTable.Rows[maxNumberOfNotes - 1][1] = varFromClient_onlyCommand;
                        incomingMessagesTable.Rows[maxNumberOfNotes - 1][2] = DateTime.Now.ToString();
                        incomingMessagesTable.Rows[maxNumberOfNotes - 1][3] = addTypeOfMessage(fullVarFromClient, varFromClient_onlyCommand);
                        incomingMessagesTable.Rows[maxNumberOfNotes - 1][4] = ProgramCondition(varFromClientBegin);

                        if (checkMark(cut_type))
                        {
                            addAtListView(incomingMessagesTable, cut_type);
                            AttentionShowMore20(cut_type, varFromClient_onlyCommand, ref countErrors, ref countExceptions);
                        }
                    }
                    else
                    {
                        incomingMessagesTable.Rows.Add(new object[] { null, varFromClient_onlyCommand, DateTime.Now.ToString(), addTypeOfMessage(fullVarFromClient, varFromClient_onlyCommand), ProgramCondition(varFromClientBegin) }); // добавление в DataSet

                        if (checkMark(cut_type))
                        {
                            int index = listView1.Items.Add(incomingMessagesTable.Rows[listCounter - 1][0].ToString()).Index; // Добавление ID в listView
                            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[listCounter - 1][1].ToString()); // Добавление Сообщения в listView 
                            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[listCounter - 1][2].ToString()); // Добавление времени в listView 
                            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[listCounter - 1][3].ToString()); // Добавление Типа сообщения в listView 
                            AttentionShow(cut_type, varFromClient_onlyCommand, index, ref countErrors, ref countExceptions);
                            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[listCounter - 1][4].ToString()); // Добавление приложения в listView 
                            ++listCounter;
                        }
                    }

                    LogWrighter(fullVarFromClient, varFromClient_onlyCommand);
                    client.Close();
                }
                catch (System.IO.IOException io)
                {
                    MessageBox.Show("IOException:\n" + io.ToString());
                }
            }
        }

        public void addAtListView(DataTable incomingMessagesTable, string cut_type)
        {
            listView1.Items.Clear();
            for (int i = 0; i < incomingMessagesTable.Rows.Count; i++)
            {
                if (checkMarkNew(incomingMessagesTable.Rows[i][3].ToString()))
                {
                    int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());

                    if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                        listView1.Items[index].BackColor = Color.Coral;
                    if (incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                        listView1.Items[index].BackColor = Color.SandyBrown;
                }
            }
        }
        //---------------------------Проверка условия----------------------------------------------

        public void AttentionShow(string cut_type, string varFromClient_onlyCommand, int index, ref int countErrors, ref int countExceptions)
        {
            switch (cut_type)
            {
                case "ftl":
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\delete1");
                    
                    listView1.Items[index].BackColor = Color.Coral;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Фатальная ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "err":
                    ++countErrors;
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\delete1.ico");
                    listView1.Items[index].BackColor = Color.Coral;

                    //contextMenuStrip1.Show();
                    //contextMenuStrip1.Items.Add(new ToolStripSeparator());
                    //contextMenuStrip1.Items.Add(varFromClient_onlyCommand, func());

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "wrn":
                    ++countExceptions;
                    lblExceptionsValue.Text = countExceptions.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\alert.ico");
                    listView1.Items[index].BackColor = Color.SandyBrown;

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon1.BalloonTipTitle = "Предупреждение";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                default:
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\success.ico");
                    //this.notifyIcon1.Icon = WinFormsTCP_Server.Properties.Resources();
                        //new Icon("success.ico");
                    break;
            }
        }

        //-----------------------------------------------------------------------

        public void AttentionShowMore20(string cut_type, string varFromClient_onlyCommand, ref int countErrors, ref int countExceptions)
        {
            switch (cut_type)
            {
                case "ftl":
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\delete1");

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Фатальная ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "err":
                    ++countErrors;
                    lblErrorsValue.Text = countErrors.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\delete1.ico");

                    //contextMenuStrip1.Show();
                    //contextMenuStrip1.Items.Add(new ToolStripSeparator());
                    //contextMenuStrip1.Items.Add(varFromClient_onlyCommand, func());

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                    notifyIcon1.BalloonTipTitle = "Ошибка";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                case "wrn":
                    ++countExceptions;
                    lblExceptionsValue.Text = countExceptions.ToString();
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\alert.ico");

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon1.BalloonTipTitle = "Предупреждение";
                    notifyIcon1.BalloonTipText = varFromClient_onlyCommand;
                    notifyIcon1.ShowBalloonTip(4);
                    break;
                default:
                    this.notifyIcon1.Icon = new Icon(Environment.CurrentDirectory + "\\icons\\success.ico");
                    //this.notifyIcon1.Icon = WinFormsTCP_Server.Properties.Resources();
                    //new Icon("success.ico");
                    break;
            }
        }

        public void func()
        {
            MessageBox.Show("dfgg");
        }


        //------------------------Проверка на тип-----------------------------
        public string addTypeOfMessage(string fullVarFromClient, string varFromClient_onlyCommand)
        {
            string cut_type = fullVarFromClient.Remove(0, fullVarFromClient.Length - 3);
            switch (cut_type)
            {
                case "inf":
                    return "Инфо";
                    break;
                case "wrn":
                    return "Предупреждение";
                    break;
                case "err":
                    return "Ошибка";
                    break;
                case "dbg":
                    return "Отладочное сообщение";
                    break;
                case "ftl":
                    return "Фатальная ошибка";
                    break;
                default:
                    return "Неизвестно";
                    break;
            }
        }

        //----------------------------Запись в лог-------------------------------------------
        public void LogWrighter(string fullVarFromClient, string varFromClient_onlyCommand)
        {
            string cut_type = fullVarFromClient.Remove(0, fullVarFromClient.Length - 3);
            switch (cut_type)
            {
                case "inf":
                    cls.Info(varFromClient_onlyCommand);
                    break;
                case "wrn":
                    cls.Warning(varFromClient_onlyCommand);
                    break;
                case "err":
                    cls.Error(varFromClient_onlyCommand);
                    break;
                case "dbg":
                    cls.Debug(varFromClient_onlyCommand);
                    break;
                case "ftl":
                    cls.Fatal(varFromClient_onlyCommand);
                    break;
                default:
                    cls.Info(varFromClient_onlyCommand);
                    break;
            }
        }


        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        public bool checkMark(string type_of_message)
        {
            switch (type_of_message)
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

        public bool checkMarkNew(string type_of_message)
        {
            switch (type_of_message)
            {
                case "Инфо":

                    if (informMessagesToolStripMenuItem.Checked == true)
                        return true;
                    else
                        return false;
                    break;

                case "Предупреждение":

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
            //string path = "C:\\Users\\asu-GainullinRE\\Documents\\Visual Studio 2015\\Projects\\WinFormsTCP_Server\\WinFormsTCP_Server\\bin\\Debug";
            //System.Diagnostics.Process.Start("notepad");

            //UpdateListView();
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

        private void warningsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (warningsToolStripMenuItem.Checked == true)
            {
                warningsToolStripMenuItem.Checked = false;
                listView1.Items.Clear();

                if (informMessagesToolStripMenuItem.Checked == true)
                    UpdateListView2();
                else
                    UpdateListView4();
            }
            else
            {
                warningsToolStripMenuItem.Checked = true;
                listView1.Items.Clear();

                if (informMessagesToolStripMenuItem.Checked == true)
                    UpdateListView1();
                else
                    UpdateListView3();
            }
        }

        private void informMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (informMessagesToolStripMenuItem.Checked == true)
            {
                informMessagesToolStripMenuItem.Checked = false;
                listView1.Items.Clear();

                if (warningsToolStripMenuItem.Checked == true)
                    UpdateListView3();
                else
                    UpdateListView4();
            }
            else
            {
                informMessagesToolStripMenuItem.Checked = true;
                listView1.Items.Clear();

                if (warningsToolStripMenuItem.Checked == true)
                    UpdateListView1();
                else
                    UpdateListView2();
            }
        }

        public void UpdateListView1()
        {
            for (int i = 0; i < incomingMessagesTable.Rows.Count; i++)
            {
                if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка" || incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение" || incomingMessagesTable.Rows[i][3].ToString() == "Инфо")
                {
                    int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());

                    if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                        listView1.Items[index].BackColor = Color.Coral;
                    if (incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                        listView1.Items[index].BackColor = Color.SandyBrown;
                }
            }
        }

        public void UpdateListView2()
        {
            for (int i = 0; i < incomingMessagesTable.Rows.Count; i++)
            {
                if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка" || incomingMessagesTable.Rows[i][3].ToString() == "Инфо")
                {
                    int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());

                    if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                        listView1.Items[index].BackColor = Color.Coral;
                    if (incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                        listView1.Items[index].BackColor = Color.SandyBrown;

                }
            }
        }

        public void UpdateListView3()
        {
            for (int i = 0; i < incomingMessagesTable.Rows.Count; i++)
            {
                if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка" || incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                {
                    int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());

                    if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                        listView1.Items[index].BackColor = Color.Coral;
                    if (incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                        listView1.Items[index].BackColor = Color.SandyBrown;
                }
            }
        }

        public void UpdateListView4()
        {
            for (int i = 0; i < incomingMessagesTable.Rows.Count; i++)
            {
                if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                {
                    int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
                    listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());

                    if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
                        listView1.Items[index].BackColor = Color.Coral;
                    if (incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение")
                        listView1.Items[index].BackColor = Color.SandyBrown;
                }
            }
        }


        //public void UpdateListView(string type_of_message)
        //{
        //    //MessageBox.Show(incomingMessagesTable.Rows.Count.ToString());
           

        //    for (int i = 0; i < incomingMessagesTable.Rows.Count; i++ )
        //    {
        //        if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка" || incomingMessagesTable.Rows[i][3].ToString() == "Предупреждение" || incomingMessagesTable.Rows[i][3].ToString() == "Ошибка")
        //        {

        //        }



        //        if (incomingMessagesTable.Rows[i][3].ToString() == "Ошибка" || incomingMessagesTable.Rows[i][3].ToString() == type_of_message )
        //        {
        //            int index = listView1.Items.Add(incomingMessagesTable.Rows[i][0].ToString()).Index;
        //            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][1].ToString());
        //            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][2].ToString());
        //            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][3].ToString());
        //            listView1.Items[index].SubItems.Add(incomingMessagesTable.Rows[i][4].ToString());
        //        }
        //    }
        //}

        private void someNiceFunc(DataTable incomingMessagesTable)
        {
            listView1.Items.Clear();

            var selected_row = incomingMessagesTable.Select("Type = 'Инфо'");
            foreach (var b in selected_row)
            {
                int index = listView1.Items.Add(b[0].ToString()).Index;
                listView1.Items[index].SubItems.Add(b[1].ToString());
                listView1.Items[index].SubItems.Add(b[2].ToString());
                listView1.Items[index].SubItems.Add(b[3].ToString());
                listView1.Items[index].SubItems.Add(b[4].ToString());
            }
        }

        private void timerCheckStatus_Tick(object sender, EventArgs e)
        {
            string service_name = "Report Manager3";
            ServiceController sc = new ServiceController(service_name);

            //switch (sc.Status.ToString())
            //{
            //    case "Running":
            //        lblConnection.Text = "Служба " + service_name + " работает";
            //        break;
            //    case "Stopped":
            //        lblConnection.Text = "Служба " + service_name + " остановлена";
            //        break;
            //    case "StartPending":
            //        lblConnection.Text = "Служба " + service_name + " запускается";
            //        break;
            //    case "StopPending":
            //        lblConnection.Text = "Служба " + service_name + " останавливается";
            //        break;
            //}
            //GC.Collect();
            sc.Dispose();
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
