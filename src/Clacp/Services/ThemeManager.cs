using System;
using System.Linq;
using System.Windows;

namespace Clacp.Services;

public static class ThemeManager
{
    public static void Apply(bool darkTheme)
    {
        var uri = darkTheme
            ? new Uri("Themes/Colors.Dark.xaml", UriKind.Relative)
            : new Uri("Themes/Colors.Light.xaml", UriKind.Relative);

        var appResources = System.Windows.Application.Current.Resources.MergedDictionaries;

        var existing = appResources
            .Where(d => d.Source != null &&
                        (d.Source.OriginalString.Contains("Colors.Dark.xaml") || d.Source.OriginalString.Contains("Colors.Light.xaml")))
            .ToList();

        foreach (var dict in existing)
            appResources.Remove(dict);

        appResources.Add(new ResourceDictionary { Source = uri });
    }
}
