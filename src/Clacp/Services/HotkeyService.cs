using System;
using System.Collections.Generic;
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

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly IntPtr _handle;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 0xA000;

    public HotkeyService(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _handle = helper.Handle;
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("La fenetre doit deja posseder un handle natif.");

        _source = HwndSource.FromHwnd(_handle) ?? throw new InvalidOperationException("Impossible d'obtenir HwndSource.");
        _source.AddHook(WndProc);
    }

    /// <summary>Registers a new global hotkey and returns its id, or -1 if registration failed.</summary>
    public int Register(ModifierKeys modifiers, Key key, Action onPressed)
    {
        var id = _nextId++;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var mod = ConvertModifiers(modifiers);

        if (!RegisterHotKey(_handle, id, mod, vk))
            return -1;

        _handlers[id] = onPressed;
        return id;
    }

    public void Unregister(int id)
    {
        if (id < 0)
            return;

        UnregisterHotKey(_handle, id);
        _handlers.Remove(id);
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
        if (msg == WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var onPressed))
        {
            onPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys)
            UnregisterHotKey(_handle, id);

        _handlers.Clear();
        _source.RemoveHook(WndProc);
    }
}
