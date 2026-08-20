using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Clacp.Services;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const int HotkeyId = 0x2A2C;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly IntPtr _handle;
    private bool _registered;

    public event Action? HotkeyPressed;

    public HotkeyService(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _handle = helper.Handle;
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("La fenetre doit deja posseder un handle natif.");

        _source = HwndSource.FromHwnd(_handle) ?? throw new InvalidOperationException("Impossible d'obtenir HwndSource.");
        _source.AddHook(WndProc);
    }

    public bool Register(ModifierKeys modifiers, Key key)
    {
        if (_registered)
            UnregisterHotKey(_handle, HotkeyId);

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var mod = ConvertModifiers(modifiers);
        _registered = RegisterHotKey(_handle, HotkeyId, mod, vk);
        return _registered;
    }

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= MOD_WIN;
        return result;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registered)
            UnregisterHotKey(_handle, HotkeyId);

        _source.RemoveHook(WndProc);
    }
}
