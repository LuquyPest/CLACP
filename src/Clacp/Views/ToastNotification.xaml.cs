using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Clacp.Views;

public partial class ToastNotification : Window
{
    private ToastNotification(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Opacity = 0;
    }

    public static void Show(string message)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        dispatcher.Invoke(() => new ToastNotification(message).Show());
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        PositionBottomRight();
        AnimateAndClose();
    }

    private void PositionBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 24;
        Top = workArea.Bottom - ActualHeight - 24;
    }

    private void AnimateAndClose()
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2200) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        };
        timer.Start();
    }
}
