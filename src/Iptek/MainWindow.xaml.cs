using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Clacp.Services;
using Clacp.Views;
using Iptek.Models;
using Iptek.Services;
using WinForms = System.Windows.Forms;

namespace Iptek;

public partial class MainWindow : Window
{
    private static readonly Key[] ModifierOnlyKeys =
    {
        Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin,
    };

    private readonly SettingsService _settingsService = new();

    private HotkeyService? _hotkeyService;
    private int _clipboardHotkeyId = -1;
    private WinForms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    private AppSettings _settings = new();
    private ModifierKeys _pendingClipboardHotkeyModifiers;
    private Key _pendingClipboardHotkeyKey;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load();

        _pendingClipboardHotkeyModifiers = _settings.ClipboardHotkeyModifiers;
        _pendingClipboardHotkeyKey = _settings.ClipboardHotkeyKey;
        PopulateSettingsUi();

        SetupTrayIcon();
        SetupHotkey();
    }

    private void SetupHotkey()
    {
        _hotkeyService = new HotkeyService(this);
        RegisterClipboardHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);
    }

    private void RegisterClipboardHotkey(ModifierKeys modifiers, Key key)
    {
        if (_hotkeyService == null)
            return;

        _hotkeyService.Unregister(_clipboardHotkeyId);
        _clipboardHotkeyId = _hotkeyService.Register(modifiers, key, OnClipboardHotkeyPressed);

        if (_clipboardHotkeyId < 0)
        {
            System.Windows.MessageBox.Show(this,
                $"Impossible d'enregistrer le raccourci {FormatHotkey(modifiers, key)} (deja utilise par une autre application).",
                "IPTEK", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PopulateSettingsUi()
    {
        DelayBox.Text = _settings.RetypeDelayMs.ToString();
        ClipboardHotkeyBox.Text = FormatHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);

        ThemeToggle.IsChecked = _settings.DarkThemeEnabled;
        ThemeToggle.Content = _settings.DarkThemeEnabled ? "Sombre" : "Claire";

        var startsWithWindows = StartupService.IsEnabled();
        StartWithWindowsToggle.IsChecked = startsWithWindows;
        StartWithWindowsToggle.Content = startsWithWindows ? "Activee" : "Desactivee";
    }

    private static string FormatHotkey(ModifierKeys modifiers, Key key)
    {
        var parts = new StringBuilder();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Append("Ctrl + ");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Append("Alt + ");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Append("Maj + ");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Append("Win + ");
        parts.Append(key);
        return parts.ToString();
    }

    private void OnThemeToggleChanged(object sender, RoutedEventArgs e)
    {
        ThemeToggle.Content = ThemeToggle.IsChecked == true ? "Sombre" : "Claire";
    }

    private void OnStartWithWindowsToggleChanged(object sender, RoutedEventArgs e)
    {
        StartWithWindowsToggle.Content = StartWithWindowsToggle.IsChecked == true ? "Activee" : "Desactivee";
    }

    private void OnClipboardHotkeyBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        e.Handled = true;

        if (Array.IndexOf(ModifierOnlyKeys, key) >= 0)
            return;

        _pendingClipboardHotkeyModifiers = Keyboard.Modifiers;
        _pendingClipboardHotkeyKey = key;
        ClipboardHotkeyBox.Text = FormatHotkey(_pendingClipboardHotkeyModifiers, _pendingClipboardHotkeyKey);
    }

    private void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DelayBox.Text, out var delayMs) || delayMs < 0)
        {
            ShowSettingsError("Le delai doit etre un nombre entier positif.");
            return;
        }

        if (_pendingClipboardHotkeyModifiers == ModifierKeys.None)
        {
            ShowSettingsError("Le raccourci doit inclure au moins une touche de modification (Ctrl, Alt, Maj ou Win).");
            return;
        }

        _settings.DarkThemeEnabled = ThemeToggle.IsChecked == true;
        ThemeManager.Apply(_settings.DarkThemeEnabled);

        _settings.RetypeDelayMs = delayMs;
        _settings.ClipboardHotkeyModifiers = _pendingClipboardHotkeyModifiers;
        _settings.ClipboardHotkeyKey = _pendingClipboardHotkeyKey;
        _settingsService.Save(_settings);

        StartupService.SetEnabled(StartWithWindowsToggle.IsChecked == true);

        RegisterClipboardHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);

        SettingsStatusText.Foreground = System.Windows.Media.Brushes.Green;
        SettingsStatusText.Text = "Parametres enregistres.";
    }

    private void ShowSettingsError(string message)
    {
        SettingsStatusText.Foreground = System.Windows.Media.Brushes.Red;
        SettingsStatusText.Text = message;
    }

    private void OnClipboardHotkeyPressed()
    {
        if (!System.Windows.Clipboard.ContainsText())
            return;

        string text;
        try
        {
            text = System.Windows.Clipboard.GetText();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
            return;

        var targetWindow = ForegroundWindowHelper.GetCurrentForegroundWindow();
        _ = TypeTextAsync(text, targetWindow, _settings.RetypeDelayMs);
    }

    private static async Task TypeTextAsync(string text, IntPtr targetWindow, int delayMs)
    {
        ForegroundWindowHelper.RestoreForegroundWindow(targetWindow);
        await Task.Delay(delayMs);

        await Task.Run(() => TypingService.TypeText(text));

        ToastNotification.Show("Texte tape avec succes");
    }

    private void SetupTrayIcon()
    {
        var iconPath = Process.GetCurrentProcess().MainModule?.FileName;
        var icon = iconPath != null
            ? System.Drawing.Icon.ExtractAssociatedIcon(iconPath)
            : System.Drawing.SystemIcons.Application;

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "IPTEK",
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Ouvrir", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;

        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            _hotkeyService?.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            return;
        }

        e.Cancel = true;
        Hide();
    }
}
