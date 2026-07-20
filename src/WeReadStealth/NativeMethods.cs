using System;
using System.Runtime.InteropServices;

namespace WeReadStealth
{
    internal static class NativeMethods
    {
        internal const int WsThickFrame = 0x00040000;
        internal const int WmHotkey = 0x0312;
        internal const int WmNcHitTest = 0x0084;
        internal const int WmNcLButtonDown = 0x00A1;

        internal const int HtClient = 1;
        internal const int HtCaption = 2;
        internal const int HtLeft = 10;
        internal const int HtRight = 11;
        internal const int HtTop = 12;
        internal const int HtTopLeft = 13;
        internal const int HtTopRight = 14;
        internal const int HtBottom = 15;
        internal const int HtBottomLeft = 16;
        internal const int HtBottomRight = 17;

        internal const uint ModControl = 0x0002;
        internal const uint ModShift = 0x0004;
        internal const uint ModNoRepeat = 0x4000;

        internal const int DwmwaUseImmersiveDarkMode = 20;
        internal const int DwmwaBorderColor = 34;
        internal const int DwmColorNone = unchecked((int)0xFFFFFFFE);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr iconHandle);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);
    }

    internal static class WindowStyles
    {
        internal static int AddResizable(int style)
        {
            return style | NativeMethods.WsThickFrame;
        }
    }
}
