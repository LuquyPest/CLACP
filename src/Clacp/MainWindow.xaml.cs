using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
    private readonly VaultService _vaultService = new();
    private readonly ObservableCollection<VaultEntry> _entries = new();

    private VaultSession? _session;
    private HotkeyService? _hotkeyService;
    private WinForms.NotifyIcon? _trayIcon;
    private bool _isExiting;

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
        if (!_hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.P))
        {
            System.Windows.MessageBox.Show(this,
                "Impossible d'enregistrer le raccourci Ctrl+Alt+P (deja utilise par une autre application).",
                "Clacp", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
            _ = TypeSelectedEntryAsync(popup.SelectedEntry, targetWindow);
        }
    }

    private static async Task TypeSelectedEntryAsync(VaultEntry entry, IntPtr targetWindow)
    {
        ForegroundWindowHelper.RestoreForegroundWindow(targetWindow);
        await Task.Delay(5000);

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
