using System.Windows;
using Clacp.Services;

namespace Clacp;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new SettingsService().Load();
        ThemeManager.Apply(settings.DarkThemeEnabled);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
