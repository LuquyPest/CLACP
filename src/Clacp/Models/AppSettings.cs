using System.Windows.Input;

namespace Clacp.Models;

public class AppSettings
{
    public int RetypeDelayMs { get; set; } = 5000;
    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;
    public Key HotkeyKey { get; set; } = Key.P;
}
