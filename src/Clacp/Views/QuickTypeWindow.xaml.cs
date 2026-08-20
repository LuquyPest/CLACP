using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Clacp.Models;

namespace Clacp.Views;

public partial class QuickTypeWindow : Window
{
    private readonly List<VaultEntry> _allEntries;

    public VaultEntry? SelectedEntry { get; private set; }

    public QuickTypeWindow(IEnumerable<VaultEntry> entries)
    {
        InitializeComponent();
        _allEntries = entries.OrderBy(e => e.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
        ResultsList.ItemsSource = _allEntries;

        Loaded += (_, _) => SearchBox.Focus();
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var filter = SearchBox.Text;
        List<VaultEntry> filtered;

        if (string.IsNullOrWhiteSpace(filter))
        {
            filtered = _allEntries;
        }
        else
        {
            filtered = _allEntries
                .Where(entry =>
                    Contains(entry.Title, filter) ||
                    Contains(entry.Username, filter) ||
                    Contains(entry.Url, filter))
                .ToList();
        }

        ResultsList.ItemsSource = filtered;
        if (filtered.Count > 0)
            ResultsList.SelectedIndex = 0;
    }

    private static bool Contains(string source, string value)
        => source.Contains(value, StringComparison.CurrentCultureIgnoreCase);

    private void OnSearchPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                ConfirmSelection();
                e.Handled = true;
                break;
            case Key.Escape:
                DialogResult = false;
                e.Handled = true;
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        if (ResultsList.Items.Count == 0)
            return;

        var next = ResultsList.SelectedIndex + delta;
        if (next < 0) next = 0;
        if (next >= ResultsList.Items.Count) next = ResultsList.Items.Count - 1;

        ResultsList.SelectedIndex = next;
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    private void ConfirmSelection()
    {
        if (ResultsList.SelectedItem is VaultEntry entry)
        {
            SelectedEntry = entry;
            DialogResult = true;
        }
    }

    private void OnResultsDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmSelection();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (IsVisible)
            DialogResult = false;
    }
}
