using System.Windows;
using Microsoft.Win32;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// Applies the light/dark palette at runtime by swapping a merged resource
/// dictionary on the application, and can follow the Windows system theme.
/// </summary>
public sealed class ThemeService
{
    private const string LightPalette = "Themes/Palette.Light.xaml";
    private const string DarkPalette = "Themes/Palette.Dark.xaml";

    private ResourceDictionary? _current;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    /// <summary>True when the effective (resolved) theme is dark.</summary>
    public bool IsDark { get; private set; }

    public event EventHandler? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;
        bool dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDark()
        };

        ApplyResolved(dark);
    }

    /// <summary>Re-evaluates the system theme when in System mode.</summary>
    public void ReevaluateSystem()
    {
        if (CurrentTheme == AppTheme.System)
            ApplyResolved(IsSystemDark());
    }

    private void ApplyResolved(bool dark)
    {
        Application app = Application.Current;
        if (app is null)
            return;

        var uri = new Uri(dark ? DarkPalette : LightPalette, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        if (_current is not null)
            app.Resources.MergedDictionaries.Remove(_current);

        // Insert the palette first so Controls.xaml (added later) can resolve it.
        app.Resources.MergedDictionaries.Insert(0, dict);
        _current = dict;

        IsDark = dark;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsSystemDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is int i)
                return i == 0;
        }
        catch
        {
            // Registry unavailable => assume light.
        }

        return false;
    }
}
