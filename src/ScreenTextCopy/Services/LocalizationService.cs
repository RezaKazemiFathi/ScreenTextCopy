using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ScreenTextCopy.Services;

/// <summary>
/// Describes a UI language that the app ships with.
/// </summary>
public sealed record UiLanguage(string Code, string Name, string NativeName, FlowDirection FlowDirection);

/// <summary>
/// Loads JSON localization files and exposes strings through an indexer that
/// can be bound in XAML. Raises change notifications on the empty property name
/// so every binding refreshes when the language switches at runtime.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private readonly string _localizationDir;
    private Dictionary<string, string> _strings = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        _localizationDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
    }

    public IReadOnlyList<UiLanguage> AvailableLanguages { get; } = new[]
    {
        new UiLanguage("en", "English", "English", FlowDirection.LeftToRight),
        new UiLanguage("fa", "Persian", "فارسی", FlowDirection.RightToLeft)
    };

    public string CurrentLanguage { get; private set; } = "en";

    public FlowDirection FlowDirection { get; private set; } = FlowDirection.LeftToRight;

    /// <summary>XAML-bindable indexer: {Binding [key], Source={x:Static ...}}.</summary>
    public string this[string key] => Get(key);

    public string Get(string key) =>
        _strings.TryGetValue(key, out string? value) ? value : key;

    /// <summary>
    /// Loads a language by code. Falls back to English if the requested file is
    /// missing or invalid. Safe to call at runtime to switch languages live.
    /// </summary>
    public void SetLanguage(string code)
    {
        Dictionary<string, string>? loaded = TryLoad(code);
        if (loaded is null && code != "en")
        {
            code = "en";
            loaded = TryLoad("en");
        }

        _strings = loaded ?? new Dictionary<string, string>();
        CurrentLanguage = code;
        FlowDirection = code == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        // Empty string => refresh all bindings on this source, including the indexer.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    private Dictionary<string, string>? TryLoad(string code)
    {
        try
        {
            string path = Path.Combine(_localizationDir, code + ".json");
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            using JsonDocument doc = JsonDocument.Parse(json);
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.StartsWith('_'))
                    continue; // skip _meta and reserved keys
                if (prop.Value.ValueKind == JsonValueKind.String)
                    map[prop.Name] = prop.Value.GetString() ?? prop.Name;
            }

            return map;
        }
        catch
        {
            return null;
        }
    }
}
