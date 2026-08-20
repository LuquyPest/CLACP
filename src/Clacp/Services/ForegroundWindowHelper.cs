using System;
using System.Runtime.InteropServices;

namespace Clacp.Services;

public static class ForegroundWindowHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    public static IntPtr GetCurrentForegroundWindow() => GetForegroundWindow();

    public static void RestoreForegroundWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return;

        var targetThread = GetWindowThreadProcessId(hWnd, out _);
        var currentThread = GetCurrentThreadId();

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, true);

        SetForegroundWindow(hWnd);

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, false);
    }
}
