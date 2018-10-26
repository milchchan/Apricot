using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Apricot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool sessionEnding = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Agent agent = null;

            Script.Instance.Load();

            foreach (Character character in Script.Instance.Characters)
            {
                Agent a = new Agent(character.Name);

                if (agent == null)
                {
                    agent = a;
                }
                else
                {
                    a.Owner = agent;
                }

                a.Show();
            }

            Script.Instance.Enabled = true;
            Script.Instance.Tick(DateTime.Today);
            Script.Instance.Update(true);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (!this.sessionEnding)
            {
                Script.Instance.Save();
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            this.MainWindow.Close();

            Script.Instance.Save();

            this.sessionEnding = true;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Exception.ToString());
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
