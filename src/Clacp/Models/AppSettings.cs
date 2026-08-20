using System.Windows.Input;

namespace Clacp.Models;

public class AppSettings
{
    public int RetypeDelayMs { get; set; } = 5000;

    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;
    public Key HotkeyKey { get; set; } = Key.P;

    public ModifierKeys ClipboardHotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;
    public Key ClipboardHotkeyKey { get; set; } = Key.L;

    public bool VaultProtectionEnabled { get; set; } = false;
    public bool VaultEnabled { get; set; } = false;

    public bool DarkThemeEnabled { get; set; } = true;
}
