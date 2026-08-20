using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
    private WinForms.NotifyIcon? _trayIcon;
    private bool _isExiting;

    private AppSettings _settings = new();
    private ModifierKeys _pendingHotkeyModifiers;
    private Key _pendingHotkeyKey;

    public MainWindow()
    {
        InitializeComponent();
        EntriesGrid.ItemsSource = _entries;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!UnlockOrCreateVault())
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        _settings = _settingsService.Load();
        _pendingHotkeyModifiers = _settings.HotkeyModifiers;
        _pendingHotkeyKey = _settings.HotkeyKey;
        PopulateSettingsUi();

        SetupTrayIcon();
        SetupHotkey();
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

    private void SetupHotkey()
    {
        _hotkeyService = new HotkeyService(this);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        RegisterHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
    }

    private bool RegisterHotkey(ModifierKeys modifiers, Key key)
    {
        if (_hotkeyService == null)
            return false;

        var ok = _hotkeyService.Register(modifiers, key);
        if (!ok)
        {
            System.Windows.MessageBox.Show(this,
                $"Impossible d'enregistrer le raccourci {FormatHotkey(modifiers, key)} (deja utilise par une autre application).",
                "Clacp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        RefreshHotkeyHint();
        return ok;
    }

    private void RefreshHotkeyHint()
    {
        HotkeyHint.Text = $"Raccourci global : {FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey)} pour rechercher et taper une entree";
    }

    private void PopulateSettingsUi()
    {
        DelayBox.Text = _settings.RetypeDelayMs.ToString();
        HotkeyBox.Text = FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
        RefreshHotkeyHint();
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

    private void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DelayBox.Text, out var delayMs) || delayMs < 0)
        {
            SettingsStatusText.Foreground = System.Windows.Media.Brushes.Red;
            SettingsStatusText.Text = "Le delai doit etre un nombre entier positif.";
            return;
        }

        if (_pendingHotkeyModifiers == ModifierKeys.None)
        {
            SettingsStatusText.Foreground = System.Windows.Media.Brushes.Red;
            SettingsStatusText.Text = "Le raccourci doit inclure au moins une touche de modification (Ctrl, Alt, Maj ou Win).";
            return;
        }

        _settings.RetypeDelayMs = delayMs;
        _settings.HotkeyModifiers = _pendingHotkeyModifiers;
        _settings.HotkeyKey = _pendingHotkeyKey;
        _settingsService.Save(_settings);

        RegisterHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);

        SettingsStatusText.Foreground = System.Windows.Media.Brushes.Green;
        SettingsStatusText.Text = "Parametres enregistres.";
    }

    private void OnHotkeyPressed()
    {
        if (_session == null || _entries.Count == 0)
            return;

        var targetWindow = ForegroundWindowHelper.GetCurrentForegroundWindow();

        var popup = new QuickTypeWindow(_entries);
        var result = popup.ShowDialog();

        if (result == true && popup.SelectedEntry != null)
        {
            _ = TypeSelectedEntryAsync(popup.SelectedEntry, targetWindow, _settings.RetypeDelayMs);
        }
    }

    private static async Task TypeSelectedEntryAsync(VaultEntry entry, IntPtr targetWindow, int delayMs)
    {
        ForegroundWindowHelper.RestoreForegroundWindow(targetWindow);
        await Task.Delay(delayMs);

        var sequence = entry.AutoType == AutoTypeMode.PasswordOnly
            ? entry.Password
            : $"{entry.Username}\t{entry.Password}";

        await Task.Run(() => TypingService.TypeText(sequence));
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
        menu.Items.Add("Verrouiller", null, (_, _) => LockVault());
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

        if (_session == null)
        {
            if (!UnlockOrCreateVault())
                ExitApplication();
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
