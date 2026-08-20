using System.Windows;
using Clacp.Models;
using Clacp.Services;

namespace Clacp.Views;

public partial class EntryEditWindow : Window
{
    public VaultEntry Entry { get; }
    public bool Saved { get; private set; }

    public EntryEditWindow(VaultEntry? entry)
    {
        InitializeComponent();
        Entry = entry ?? new VaultEntry();
        Title = entry == null ? "Nouvelle entree" : "Modifier l'entree";

        TitleBox.Text = Entry.Title;
        UsernameBox.Text = Entry.Username;
        PasswordBoxHidden.Password = Entry.Password;
        PasswordBoxVisible.Text = Entry.Password;
        UrlBox.Text = Entry.Url;
        NotesBox.Text = Entry.Notes;
        AutoTypeCombo.SelectedIndex = Entry.AutoType == AutoTypeMode.PasswordOnly ? 1 : 0;
    }

    private void OnShowPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ShowPasswordToggle.IsChecked == true)
        {
            PasswordBoxVisible.Text = PasswordBoxHidden.Password;
            PasswordBoxVisible.Visibility = Visibility.Visible;
            PasswordBoxHidden.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordBoxHidden.Password = PasswordBoxVisible.Text;
            PasswordBoxHidden.Visibility = Visibility.Visible;
            PasswordBoxVisible.Visibility = Visibility.Collapsed;
        }
    }

    private void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        var generated = PasswordGenerator.Generate();
        PasswordBoxHidden.Password = generated;
        PasswordBoxVisible.Text = generated;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Saved = false;
        DialogResult = false;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var password = ShowPasswordToggle.IsChecked == true ? PasswordBoxVisible.Text : PasswordBoxHidden.Password;

        Entry.Title = TitleBox.Text.Trim();
        Entry.Username = UsernameBox.Text.Trim();
        Entry.Password = password;
        Entry.Url = UrlBox.Text.Trim();
        Entry.Notes = NotesBox.Text;
        Entry.AutoType = AutoTypeCombo.SelectedIndex == 1 ? AutoTypeMode.PasswordOnly : AutoTypeMode.UsernameTabPassword;
        Entry.UpdatedUtc = System.DateTime.UtcNow;

        Saved = true;
        DialogResult = true;
    }
}
