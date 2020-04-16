using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wsplit
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) return;

            Proc = new NativeMethods.WinEventProc(WindowEventCallback);

            NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_MOVESIZEEND, NativeMethods.EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero, Proc, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

            Application.Run();
        }

        static NativeMethods.WinEventProc Proc;

        static void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (NativeMethods.IsZoomed(hwnd) != 0) return;

            if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT window, Marshal.SizeOf(typeof(NativeMethods.RECT))) != 0) return;

            if (NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT frame) == false) return;

            var windowWidth = window.right - window.left;
            var windowHeight = window.bottom - window.top;
            var workingArea = Screen.FromHandle(hwnd).WorkingArea;

            var border = new NativeMethods.RECT()
            {
                left = window.left - frame.left,
                top = window.top - frame.top,
                right = frame.right - window.right,
                bottom = frame.bottom - window.bottom,
            };

            var hsplit1 = new Rectangle()
            {
                X = workingArea.Left,
                Y = workingArea.Top,
                Width = workingArea.Width / 2,
                Height = workingArea.Height,
            };

            var hsplit2 = new Rectangle()
            {
                X = hsplit1.Right,
                Y = hsplit1.Y,
                Width = workingArea.Width - hsplit1.Width,
                Height = workingArea.Height,
            };

            var vsplit1 = new Rectangle()
            {
                X = workingArea.Left,
                Y = workingArea.Top,
                Width = workingArea.Width,
                Height = workingArea.Height / 2,
            };

            var vsplit2 = new Rectangle()
            {
                X = vsplit1.X,
                Y = vsplit1.Bottom,
                Width = workingArea.Width,
                Height = workingArea.Height - vsplit1.Height,
            };

            if (windowWidth < workingArea.Width && windowHeight < workingArea.Height)
            {
                var resized = (Rectangle?)null;

                var horizontal = workingArea.Height < workingArea.Width;

                if (workingArea.Left == window.left && workingArea.Top == window.top)
                {
                    resized = horizontal ? hsplit1 : vsplit1;
                }

                if (workingArea.Right == window.right && workingArea.Top == window.top)
                {
                    resized = horizontal ? hsplit2 : vsplit1;
                }

                if (workingArea.Left == window.left && workingArea.Bottom == window.bottom)
                {
                    resized = horizontal ? hsplit1 : vsplit2;
                }

                if (workingArea.Right == window.right && workingArea.Bottom == window.bottom)
                {
                    resized = horizontal ? hsplit2 : vsplit2;
                }

                if (resized is Rectangle r)
                {
                    var x = r.X - border.left;
                    var y = r.Y - border.top;
                    var w = r.Width + border.left + border.right;
                    var h = r.Height + border.top + border.bottom;

                    NativeMethods.MoveWindow(hwnd, x, y, w, h, 0);
                }
            }
        }
    }

    internal class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public const int EVENT_SYSTEM_MOVESIZEEND = 0x0000000b;

        public const int WINEVENT_OUTOFCONTEXT = 0;

        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        public static extern int IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int MoveWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, int bRepaint);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, int dwflags);

        [DllImport("user32.dll")]
        public static extern int UnhookWinEvent(IntPtr hWinEventHook);
    }
}
