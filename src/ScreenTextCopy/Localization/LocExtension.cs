using System.Windows.Data;
using System.Windows.Markup;
using ScreenTextCopy.Services;

namespace ScreenTextCopy.Localization;

/// <summary>
/// XAML markup extension for localized strings: <c>Text="{loc:Loc action.copy}"</c>.
/// Binds to the shared <see cref="LocalizationService"/> indexer so the text
/// updates live when the language changes.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension() { }

    public LocExtension(string key) => Key = key;

    /// <summary>The localization key to resolve.</summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationHub.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Static access point to the app-wide <see cref="LocalizationService"/> so the
/// <see cref="LocExtension"/> markup extension can reach it without DI plumbing
/// inside XAML. Set once during startup.
/// </summary>
public static class LocalizationHub
{
    public static LocalizationService Instance { get; private set; } = new();

    public static void Initialize(LocalizationService service) => Instance = service;
}
