using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using System.Windows.Forms;
using System.Configuration;
using System.IO;
using NavMessageCloser;

namespace NavTray
{
    public partial class Form1 : Form
    {
        
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        const int WM_CLOSE = 0x0010;
        const int WM_COMMAND = 0x0111;
        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        const int DLG_NO = 3;
        const int DLG_YES = 6;
        const string RULES_XML_FILENAME = "rules.xml";

        public Form1()
        {
            InitializeComponent();
        }

        private class NavDlg
        {
            public string dt { get; set; }
            public string msg { get; set; }
        }
        

        private static bool WndProc(IntPtr hwnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);
            List<NavDlg> childTitles = gcChildhandlesList.Target as List<NavDlg>;

            StringBuilder className = new StringBuilder(256);
            if (GetClassName(hwnd, className, className.Capacity) != 0)
            {
                if (className.ToString() == "Static")
                {                            
                    Int32 titleSize = SendMessage(hwnd, WM_GETTEXTLENGTH, 0, 0).ToInt32();
                    if (titleSize > 0)
                    {                
                        StringBuilder title = new StringBuilder(titleSize + 1);
                        SendMessage(hwnd, (int)WM_GETTEXT, title.Capacity,  title);

                        NavDlg dlg = new NavDlg();
                        dlg.dt = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"); 
                        dlg.msg = title.ToString();
                
                        childTitles.Add(dlg);                
                    }
                }
            }
            return true;            

        }

       

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBoxActivateTimer.Checked)
            {
                FindAndCloseNavDialog();
            }            
        }

        private void FindAndCloseNavDialog()
        {            
            int wnd = GetNavDialog();
            if (wnd != 0)
            {                         
                EnumWindowsProc childProc = new EnumWindowsProc(WndProc);
                List<NavDlg> wndTitles_ = new List<NavDlg>();
                GCHandle gcChildhandlesList = GCHandle.Alloc(wndTitles_);
                IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);
                try
                {
                    EnumChildWindows((IntPtr)wnd, childProc, pointerChildHandlesList);
                }
                finally
                {
                    gcChildhandlesList.Free();
                }

                uint processId;
                GetWindowThreadProcessId((IntPtr)wnd, out processId);

                foreach (NavDlg dlg in wndTitles_)
                {
                    MessageRule rule = GetMessageRule(dlg);
                    dataGridView1.Rows.Add(new object[] { dlg.dt, dlg.msg, rule.message });
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.RowCount-1].Cells[0];

                    if (rule.closeMessage)
                    {
                        // we don't known it's confirm or not
                        if (rule.onConfirm)
                        {
                            SendMessage((IntPtr)wnd, WM_COMMAND, DLG_YES, 0);
                        }
                        else
                        {
                            SendMessage((IntPtr)wnd, WM_COMMAND, DLG_NO, 0);
                        }

                        SendMessage((IntPtr)wnd, WM_CLOSE, 0, 0);
                    }

                    if (rule.startProgramm.Length > 0)
                    {
                        StartProgramms(rule.startProgramm, dlg, processId);
                    }
                    
                    
                }
                
            }
            
            
        }

        private void StartProgramms(string programCmds, NavDlg dlg, uint processId)
        {
            string[] cmdList = programCmds.Split('|');
            foreach(string cmd in cmdList)
            {
                Exec(cmd, dlg, processId);
            }
        }

        private void Exec(string cmd, NavDlg dlg, uint processId)
        {
            cmd = cmd.Replace("%datetime%", dlg.dt);
            cmd = cmd.Replace("%message%", dlg.msg);
            cmd = cmd.Replace("%processId%", processId.ToString());

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + cmd;
            process.StartInfo = startInfo;
            process.Start();
        }

        private MessageRule GetMessageRule(NavDlg dlg)
        {
            List<MessageRule> rules = GetMessageRules();
            MessageRule defaultRule = rules.Where(r => r.message == "*").First();
            rules = rules.Where(r => r.message != "*").ToList();
            MessageRule currentRule = rules.Where(r => dlg.msg.Contains(r.message)).FirstOrDefault();
            return currentRule ?? defaultRule;
        }

        private List<MessageRule> GetMessageRules()
        {
            List<MessageRule> rules = new List<MessageRule>();
            foreach(DataGridViewRow row in dataGridView2.Rows)
            {
                if (!row.IsNewRow)
                {
                    MessageRule rule = new MessageRule();
                    rule.message = (string)row.Cells["MessageText"].Value;
                    rule.onConfirm = ((string)row.Cells["OnConfirm"].Value ?? "Yes").Contains("Yes");
                    rule.closeMessage = (bool)(row.Cells["CloseMessage"].Value ?? false);
                    rule.startProgramm = (string)row.Cells["StartProgramm"].Value ?? "";
                    rules.Add(rule);
                }                
            }
            return rules;
        }

        private int GetNavDialog()
        {
            //return (int)FindWindow("#32770", "Microsoft Dynamics NAV Classic");
            return (int)FindWindow("#32770", "Microsoft Dynamics NAV");
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //comboBoxOnCofirm.SelectedIndex = 0;
            //textBox1.Text = ConfigurationManager.AppSettings.Get("skipMessagesCommaSeparated").ToString();
            LoadSettings();
        }

        private void LoadSettings()
        {
            CheckSettingsFile();
            
            DataSet ds1 = new DataSet();
            ds1.ReadXml(RULES_XML_FILENAME);
            DataTable table = ds1.Tables[0];
            //MessageBox.Show(table.Rows.Count.ToString());
            foreach (DataRow row in table.Rows)
            {                                                
                var rowS = dataGridView2.Rows[dataGridView2.Rows.Add()];
                rowS.Cells["MessageText"].Value = row.Field<String>("message");
                rowS.Cells["OnConfirm"].Value = row.Field<String>("onConfirm");
                rowS.Cells["CloseMessage"].Value = row.Field<String>("closeMessage").Contains("Yes");
                rowS.Cells["StartProgramm"].Value = row.Field<String>("startProgramm");
            }
            
        }

        private void CheckSettingsFile()
        {
            if (!File.Exists(RULES_XML_FILENAME))
            {
                var sw = File.CreateText(RULES_XML_FILENAME);
                sw.WriteLine("<?xml version='1.0' encoding='utf-8' ?>");
                sw.WriteLine("<rules>");
                sw.WriteLine("  <rule>");
                sw.WriteLine("    <message>*</message>");
                sw.WriteLine("    <onConfirm>Yes</onConfirm>");
                sw.WriteLine("    <closeMessage>Yes</closeMessage>");
                sw.WriteLine("    <startProgramm>echo %datetime% %processId% %message% >>log.txt</startProgramm>");
                sw.WriteLine("  </rule>");
                sw.WriteLine("  <rule>");
                sw.WriteLine("    <message>text containing message</message>");
                sw.WriteLine("    <onConfirm>Yes</onConfirm>");
                sw.WriteLine("    <closeMessage>No</closeMessage>");
                sw.WriteLine("    <startProgramm>echo %datetime% %message% >>log.txt|taskkill /PID %processId% /F</startProgramm>");
                sw.WriteLine("  </rule>");
                sw.WriteLine("</rules>");
                sw.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            var rules = GetMessageRules();

            if (File.Exists(RULES_XML_FILENAME))
            {
                File.Delete(RULES_XML_FILENAME);
            }

            var sw = File.CreateText(RULES_XML_FILENAME);
            sw.WriteLine("<?xml version='1.0' encoding='utf-8' ?>");
            sw.WriteLine("<rules>");

            foreach(MessageRule rule in rules)
            {
                sw.WriteLine("  <rule>");
                sw.WriteLine($"    <message>{rule.message}</message>");
                string onConfirm = rule.onConfirm ? "Yes" : "No";
                sw.WriteLine($"    <onConfirm>{onConfirm}</onConfirm>");
                string closeMessage = rule.closeMessage ? "Yes" : "No";
                sw.WriteLine($"    <closeMessage>{closeMessage}</closeMessage>");
                sw.WriteLine($"    <startProgramm>{rule.startProgramm}</startProgramm>");
                sw.WriteLine("  </rule>");
            }            

            sw.WriteLine("</rules>");
            sw.Close();
            

        }

        private void button2_Click(object sender, EventArgs e)
        {
            FindAndCloseNavDialog();
        }
    }
}
