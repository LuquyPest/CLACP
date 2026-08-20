using System.Windows;
using System.Windows.Input;
using Clacp.Services;

namespace Clacp.Views;

public partial class UnlockWindow : Window
{
    private readonly VaultService _vaultService;
    private readonly bool _isNewVault;

    public VaultSession? Session { get; private set; }

    public UnlockWindow(VaultService vaultService)
    {
        InitializeComponent();
        _vaultService = vaultService;
        _isNewVault = !_vaultService.VaultExists();

        if (_isNewVault)
        {
            TitleText.Text = "Creer un coffre";
            ActionButton.Content = "Creer le coffre";
            ConfirmLabel.Visibility = Visibility.Visible;
            PasswordBox2.Visibility = Visibility.Visible;
        }

        Loaded += (_, _) => PasswordBox1.Focus();
    }

    private void OnPasswordKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnActionClick(sender, e);
    }

    private void OnActionClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var password = PasswordBox1.Password;

        if (string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "Le mot de passe ne peut pas etre vide.";
            return;
        }

        if (_isNewVault)
        {
            if (password != PasswordBox2.Password)
            {
                ErrorText.Text = "Les mots de passe ne correspondent pas.";
                return;
            }

            Session = _vaultService.CreateVault(password);
            DialogResult = true;
            return;
        }

        var session = _vaultService.Unlock(password);
        if (session == null)
        {
            ErrorText.Text = "Mot de passe incorrect.";
            PasswordBox1.Clear();
            PasswordBox1.Focus();
            return;
        }

        Session = session;
        DialogResult = true;
    }
}
