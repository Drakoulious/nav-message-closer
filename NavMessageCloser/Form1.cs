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
using System.Threading.Tasks;
using System.Windows.Forms;

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

        const int WM_CLOSE = 0x0010;
        const int WM_COMMAND = 0x0111;
        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        const int DLG_NO = 3;
        const int DLG_YES = 6;

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
            FindAndCloseNavDialog();
        }

        private void FindAndCloseNavDialog()
        {
            int wnd = GetNavDialog();
            while (wnd != 0)
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

                foreach (NavDlg dlg in wndTitles_)
                {
                    dataGridView1.Rows.Add(new object[] { dlg.dt, dlg.msg });
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.RowCount-1].Cells[0];
                }

                if (comboBoxOnCofirm.SelectedIndex == 0)
                {
                    SendMessage((IntPtr)wnd, WM_COMMAND, DLG_YES, 0);
                }
                else
                {
                    SendMessage((IntPtr)wnd, WM_COMMAND, DLG_NO, 0);
                }
                
                SendMessage((IntPtr)wnd, WM_CLOSE, 0, 0);
                Thread.Sleep(1000);
                wnd = GetNavDialog();
            }
            
        }

        private int GetNavDialog()
        {
            return (int)FindWindow("#32770", "Microsoft Dynamics NAV Classic");
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxOnCofirm.SelectedIndex = 0;
        }
    }
}
