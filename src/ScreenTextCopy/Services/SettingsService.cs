using System.IO;
using System.Text.Json;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> as JSON under
/// %AppData%\ScreenTextCopy\settings.json.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dir;
    private readonly string _path;

    public SettingsService()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenTextCopy");
        _path = Path.Combine(_dir, "settings.json");
    }

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                {
                    Current = loaded;
                }
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings fall back to defaults rather than crashing.
            Current = new AppSettings();
        }

        return Current;
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        try
        {
            Directory.CreateDirectory(_dir);
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception)
        {
            // Persisting settings must never take down the app.
        }
    }

    public void Save() => Save(Current);
}
