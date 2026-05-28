using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WallpaperPlatform;

internal static class DesktopHelper
{
    [DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string? wnd);
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc fn, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? cls, string? wnd);
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder buf, int max);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    const int GWL_STYLE         = -16;
    const int WS_CHILD          = 0x40000000;
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_NOACTIVATE   = 0x0010;
    const uint SWP_SHOWWINDOW   = 0x0040;

    public static bool AttachToDesktop(IntPtr hwnd, int physWidth, int physHeight)
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
            return false;

        SendMessageTimeout(progman, 0x052C, (UIntPtr)0xD, (IntPtr)0x1, 0, 1000, out _);
        SendMessageTimeout(progman, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        var workerW = IntPtr.Zero;

        // Strategy 1 (Win11): WorkerW is a direct child of Progman
        workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);

        // Strategy 2 (Win10): WorkerW is a top-level window below the icon-layer WorkerW
        if (workerW == IntPtr.Zero)
        {
            EnumWindows((win, _) =>
            {
                if (FindWindowEx(win, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    workerW = FindWindowEx(IntPtr.Zero, win, "WorkerW", null);
                return true;
            }, IntPtr.Zero);
        }

        // Strategy 3: parent to Progman itself as last resort
        if (workerW == IntPtr.Zero)
            workerW = progman;

        WriteDiagnostic(progman, workerW);

        var style = GetWindowLong(hwnd, GWL_STYLE);
        SetWindowLong(hwnd, GWL_STYLE, style | WS_CHILD);
        SetParent(hwnd, workerW);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, physWidth, physHeight,
            SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_SHOWWINDOW);

        return true;
    }

    private static void WriteDiagnostic(IntPtr progman, IntPtr chosenWorkerW)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Progman:        {progman:X8}");
        sb.AppendLine($"Chosen WorkerW: {chosenWorkerW:X8}");
        GetWindowRect(chosenWorkerW, out var r);
        sb.AppendLine($"WorkerW rect:   [{r.Left},{r.Top},{r.Right},{r.Bottom}]");
        sb.AppendLine();

        sb.AppendLine("Progman direct children:");
        var child = FindWindowEx(progman, IntPtr.Zero, null, null);
        while (child != IntPtr.Zero)
        {
            var cls = new StringBuilder(256);
            GetClassName(child, cls, 256);
            GetWindowRect(child, out var cr);
            sb.AppendLine($"  {child:X8} [{cls}]  rect=[{cr.Left},{cr.Top},{cr.Right},{cr.Bottom}]");
            child = FindWindowEx(progman, child, null, null);
        }

        var path = Path.Combine(Path.GetTempPath(), "WallpaperPlatform_diag.txt");
        File.WriteAllText(path, sb.ToString());
    }
}
