using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Clacp.Models;
using Clacp.Services;
using Clacp.Views;
using WinForms = System.Windows.Forms;

namespace Clacp;

public partial class MainWindow : Window
{
    private static readonly Key[] ModifierOnlyKeys =
    {
        Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin,
    };

    private readonly VaultService _vaultService = new();
    private readonly SettingsService _settingsService = new();
    private readonly ObservableCollection<VaultEntry> _entries = new();

    private VaultSession? _session;
    private HotkeyService? _hotkeyService;
    private int _quickTypeHotkeyId = -1;
    private int _clipboardHotkeyId = -1;
    private WinForms.NotifyIcon? _trayIcon;
    private DispatcherTimer? _autoLockTimer;
    private bool _isExiting;

    private AppSettings _settings = new();
    private ModifierKeys _pendingHotkeyModifiers;
    private Key _pendingHotkeyKey;
    private ModifierKeys _pendingClipboardHotkeyModifiers;
    private Key _pendingClipboardHotkeyKey;

    public MainWindow()
    {
        InitializeComponent();
        EntriesGrid.ItemsSource = _entries;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load();

        if (_settings.VaultEnabled)
        {
            if (_settings.VaultProtectionEnabled)
            {
                if (!UnlockOrCreateVault())
                {
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                _session = _vaultService.LoadOrCreateLocalVault();
                RefreshEntries();
            }
        }

        _pendingHotkeyModifiers = _settings.HotkeyModifiers;
        _pendingHotkeyKey = _settings.HotkeyKey;
        _pendingClipboardHotkeyModifiers = _settings.ClipboardHotkeyModifiers;
        _pendingClipboardHotkeyKey = _settings.ClipboardHotkeyKey;
        PopulateSettingsUi();

        SetupTrayIcon();
        SetupHotkeys();
        SetupAutoLock();
    }

    private void SetupAutoLock()
    {
        _autoLockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoLockTimer.Tick += (_, _) => CheckAutoLock();
        _autoLockTimer.Start();
    }

    private void CheckAutoLock()
    {
        if (!_settings.VaultEnabled || !_settings.VaultProtectionEnabled || _session == null || _settings.AutoLockMinutes <= 0)
            return;

        if (GetIdleTimeMs() < _settings.AutoLockMinutes * 60_000L)
            return;

        LockVault();

        if (IsVisible && WindowState != WindowState.Minimized)
        {
            if (!UnlockOrCreateVault())
                System.Windows.Application.Current.Shutdown();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    private static long GetIdleTimeMs()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return 0;

        return unchecked((uint)Environment.TickCount64) - info.dwTime;
    }

    private bool UnlockOrCreateVault()
    {
        var dialog = new UnlockWindow(_vaultService) { Owner = this };
        var result = dialog.ShowDialog();

        if (result != true || dialog.Session == null)
            return false;

        _session = dialog.Session;
        RefreshEntries();
        return true;
    }

    private void RefreshEntries()
    {
        _entries.Clear();
        if (_session == null)
            return;

        foreach (var entry in _session.Data.Entries.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase))
            _entries.Add(entry);
    }

    private void SetupHotkeys()
    {
        _hotkeyService = new HotkeyService(this);

        if (_settings.VaultEnabled)
            RegisterQuickTypeHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);

        RegisterClipboardHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);
    }

    private void RegisterQuickTypeHotkey(ModifierKeys modifiers, Key key)
    {
        if (_hotkeyService == null)
            return;

        _hotkeyService.Unregister(_quickTypeHotkeyId);
        _quickTypeHotkeyId = _hotkeyService.Register(modifiers, key, OnHotkeyPressed);

        if (_quickTypeHotkeyId < 0)
        {
            System.Windows.MessageBox.Show(this,
                $"Impossible d'enregistrer le raccourci {FormatHotkey(modifiers, key)} (deja utilise par une autre application).",
                "Clacp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
                "Clacp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PopulateSettingsUi()
    {
        DelayBox.Text = _settings.RetypeDelayMs.ToString();
        HotkeyBox.Text = FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
        ClipboardHotkeyBox.Text = FormatHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);

        ThemeToggle.IsChecked = _settings.DarkThemeEnabled;
        ThemeToggle.Content = _settings.DarkThemeEnabled ? "Sombre" : "Claire";

        VaultEnabledToggle.IsChecked = _settings.VaultEnabled;
        VaultEnabledToggle.Content = _settings.VaultEnabled ? "Activee" : "Desactivee";

        VaultProtectionToggle.IsChecked = _settings.VaultProtectionEnabled;
        VaultProtectionToggle.Content = _settings.VaultProtectionEnabled ? "Activee" : "Desactivee";

        AutoLockBox.Text = _settings.AutoLockMinutes.ToString();

        var startsWithWindows = StartupService.IsEnabled();
        StartWithWindowsToggle.IsChecked = startsWithWindows;
        StartWithWindowsToggle.Content = startsWithWindows ? "Activee" : "Desactivee";

        UpdateVaultFeatureVisibility();
    }

    private void UpdateVaultFeatureVisibility()
    {
        CoffreTab.Visibility = _settings.VaultEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (!_settings.VaultEnabled && MainTabControl.SelectedItem == CoffreTab)
            MainTabControl.SelectedItem = ParametresTab;

        LockButton.Visibility = (_settings.VaultEnabled && _settings.VaultProtectionEnabled)
            ? Visibility.Visible
            : Visibility.Collapsed;
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

    private void OnTabControlSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.OriginalSource == MainTabControl)
            SettingsScrollViewer.ScrollToTop();
    }

    private void OnThemeToggleChanged(object sender, RoutedEventArgs e)
    {
        ThemeToggle.Content = ThemeToggle.IsChecked == true ? "Sombre" : "Claire";
    }

    private void OnVaultEnabledToggleChanged(object sender, RoutedEventArgs e)
    {
        VaultEnabledToggle.Content = VaultEnabledToggle.IsChecked == true ? "Activee" : "Desactivee";
    }

    private void OnVaultProtectionToggleChanged(object sender, RoutedEventArgs e)
    {
        VaultProtectionToggle.Content = VaultProtectionToggle.IsChecked == true ? "Activee" : "Desactivee";
    }

    private void OnStartWithWindowsToggleChanged(object sender, RoutedEventArgs e)
    {
        StartWithWindowsToggle.Content = StartWithWindowsToggle.IsChecked == true ? "Activee" : "Desactivee";
    }

    private void OnHotkeyBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        e.Handled = true;

        if (Array.IndexOf(ModifierOnlyKeys, key) >= 0)
            return;

        _pendingHotkeyModifiers = Keyboard.Modifiers;
        _pendingHotkeyKey = key;
        HotkeyBox.Text = FormatHotkey(_pendingHotkeyModifiers, _pendingHotkeyKey);
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

        if (!int.TryParse(AutoLockBox.Text, out var autoLockMinutes) || autoLockMinutes < 0)
        {
            ShowSettingsError("Le verrouillage automatique doit etre un nombre entier positif (0 pour desactiver).");
            return;
        }

        if (_pendingHotkeyModifiers == ModifierKeys.None)
        {
            ShowSettingsError("Le raccourci de recherche doit inclure au moins une touche de modification (Ctrl, Alt, Maj ou Win).");
            return;
        }

        if (_pendingClipboardHotkeyModifiers == ModifierKeys.None)
        {
            ShowSettingsError("Le raccourci presse-papiers doit inclure au moins une touche de modification (Ctrl, Alt, Maj ou Win).");
            return;
        }

        if (_pendingHotkeyModifiers == _pendingClipboardHotkeyModifiers && _pendingHotkeyKey == _pendingClipboardHotkeyKey)
        {
            ShowSettingsError("Les deux raccourcis ne peuvent pas etre identiques.");
            return;
        }

        _settings.DarkThemeEnabled = ThemeToggle.IsChecked == true;
        ThemeManager.Apply(_settings.DarkThemeEnabled);

        var vaultEnabled = VaultEnabledToggle.IsChecked == true;
        var protectionEnabled = VaultProtectionToggle.IsChecked == true;

        if (vaultEnabled)
        {
            var needsSessionSwitch = protectionEnabled != _settings.VaultProtectionEnabled || _session == null;
            if (needsSessionSwitch && !SwitchVaultProtection(protectionEnabled))
                return;
        }
        else
        {
            _session?.Lock();
            _session = null;
            _entries.Clear();
        }

        _settings.VaultEnabled = vaultEnabled;
        _settings.VaultProtectionEnabled = protectionEnabled;
        _settings.RetypeDelayMs = delayMs;
        _settings.AutoLockMinutes = autoLockMinutes;
        _settings.HotkeyModifiers = _pendingHotkeyModifiers;
        _settings.HotkeyKey = _pendingHotkeyKey;
        _settings.ClipboardHotkeyModifiers = _pendingClipboardHotkeyModifiers;
        _settings.ClipboardHotkeyKey = _pendingClipboardHotkeyKey;
        _settingsService.Save(_settings);

        StartupService.SetEnabled(StartWithWindowsToggle.IsChecked == true);

        if (_settings.VaultEnabled)
            RegisterQuickTypeHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
        else
            _hotkeyService?.Unregister(_quickTypeHotkeyId);

        RegisterClipboardHotkey(_settings.ClipboardHotkeyModifiers, _settings.ClipboardHotkeyKey);
        UpdateVaultFeatureVisibility();

        SettingsStatusText.Foreground = System.Windows.Media.Brushes.Green;
        SettingsStatusText.Text = "Parametres enregistres.";
    }

    private void ShowSettingsError(string message)
    {
        SettingsStatusText.Foreground = System.Windows.Media.Brushes.Red;
        SettingsStatusText.Text = message;
    }

    /// <summary>Switches between the master-password-protected vault and the unprotected (DPAPI) one,
    /// carrying the currently loaded entries over into the newly selected storage.</summary>
    private bool SwitchVaultProtection(bool enable)
    {
        var currentEntries = _session?.Data.Entries.ToList() ?? new System.Collections.Generic.List<VaultEntry>();

        if (enable)
        {
            var dialog = new UnlockWindow(_vaultService) { Owner = this };
            if (dialog.ShowDialog() != true || dialog.Session == null)
            {
                VaultProtectionToggle.IsChecked = false;
                return false;
            }

            dialog.Session.Data.Entries.AddRange(currentEntries);
            dialog.Session.Save();
            _session = dialog.Session;
        }
        else
        {
            var localSession = _vaultService.LoadOrCreateLocalVault();
            localSession.Data.Entries.AddRange(currentEntries);
            localSession.Save();
            _session = localSession;
        }

        _settings.VaultProtectionEnabled = enable;
        RefreshEntries();
        return true;
    }

    private void OnHotkeyPressed()
    {
        if (!_settings.VaultEnabled || _session == null || _entries.Count == 0)
            return;

        var targetWindow = ForegroundWindowHelper.GetCurrentForegroundWindow();

        var popup = new QuickTypeWindow(_entries);
        var result = popup.ShowDialog();

        if (result == true && popup.SelectedEntry != null)
        {
            var entry = popup.SelectedEntry;
            var sequence = entry.AutoType == AutoTypeMode.PasswordOnly
                ? entry.Password
                : $"{entry.Username}\t{entry.Password}";

            _ = TypeTextAsync(sequence, targetWindow, _settings.RetypeDelayMs, notify: false);
        }
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
        _ = TypeTextAsync(text, targetWindow, _settings.RetypeDelayMs, notify: true);
    }

    private static async Task TypeTextAsync(string text, IntPtr targetWindow, int delayMs, bool notify)
    {
        await Task.Delay(delayMs);

        ForegroundWindowHelper.RestoreForegroundWindow(targetWindow);
        await Task.Delay(150);

        if (ForegroundWindowHelper.GetCurrentForegroundWindow() != targetWindow)
            return;

        await Task.Run(() => TypingService.TypeText(text));

        if (notify)
            ToastNotification.Show("Mot de passe tape avec succes");
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        var dialog = new EntryEditWindow(null) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _session.Data.Entries.Add(dialog.Entry);
            _session.Save();
            RefreshEntries();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_session == null || EntriesGrid.SelectedItem is not VaultEntry selected)
            return;

        var dialog = new EntryEditWindow(selected) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _session.Save();
            RefreshEntries();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_session == null || EntriesGrid.SelectedItem is not VaultEntry selected)
            return;

        var confirm = System.Windows.MessageBox.Show(this,
            $"Supprimer l'entree \"{selected.Title}\" ?",
            "Clacp", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        _session.Data.Entries.RemoveAll(x => x.Id == selected.Id);
        _session.Save();
        RefreshEntries();
    }

    private void OnLockClick(object sender, RoutedEventArgs e)
    {
        if (!_settings.VaultEnabled || !_settings.VaultProtectionEnabled)
            return;

        LockVault();

        if (!UnlockOrCreateVault())
            System.Windows.Application.Current.Shutdown();
    }

    private void LockVault()
    {
        _session?.Lock();
        _session = null;
        _entries.Clear();
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
            Text = "Clacp",
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Ouvrir", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Verrouiller", null, (_, _) => { if (_settings.VaultEnabled && _settings.VaultProtectionEnabled) LockVault(); });
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

        if (_settings.VaultEnabled && _session == null)
        {
            if (_settings.VaultProtectionEnabled)
            {
                if (!UnlockOrCreateVault())
                    ExitApplication();
            }
            else
            {
                _session = _vaultService.LoadOrCreateLocalVault();
                RefreshEntries();
            }
        }
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
            _autoLockTimer?.Stop();
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
