using System.Windows.Input;

namespace Iptek.Models;

public class AppSettings
{
    public int RetypeDelayMs { get; set; } = 5000;

    public ModifierKeys ClipboardHotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;
    public Key ClipboardHotkeyKey { get; set; } = Key.L;

    public bool DarkThemeEnabled { get; set; } = true;
}
