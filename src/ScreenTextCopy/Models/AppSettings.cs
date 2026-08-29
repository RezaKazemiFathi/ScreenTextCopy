namespace ScreenTextCopy.Models;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum TranslationProviderKind
{
    /// <summary>Free, no API key required (MyMemory).</summary>
    Free,

    /// <summary>
    /// Any OpenAI-compatible chat endpoint (custom base URL + optional key +
    /// model). Works with OpenAI, OpenRouter, Groq, Azure, Ollama, LM Studio, …
    /// </summary>
    CustomAi
}

/// <summary>
/// How outbound HTTP (AI translation, model listing, language-pack downloads)
/// reaches the network. Many AI providers are blocked in some regions, so users
/// route traffic through a local VPN/proxy (e.g. v2rayN on 127.0.0.1:10808).
/// </summary>
public enum NetworkProxyMode
{
    /// <summary>Use the Windows system proxy (the default; matches most browsers).</summary>
    System,

    /// <summary>Ignore any system proxy and connect directly.</summary>
    None,

    /// <summary>Use the explicit proxy in <see cref="AppSettings.ProxyAddress"/>.</summary>
    Manual
}

/// <summary>
/// A configurable global hotkey (Win32 modifiers + virtual-key).
/// </summary>
public sealed class HotkeyConfig
{
    public bool Control { get; set; } = true;
    public bool Shift { get; set; } = true;
    public bool Alt { get; set; }
    public bool Win { get; set; }

    /// <summary>Virtual-key code. Default 0x58 = 'X'.</summary>
    public uint VirtualKey { get; set; } = 0x58;

    /// <summary>Human-readable key label, e.g. "X".</summary>
    public string KeyLabel { get; set; } = "X";

    public string Describe()
    {
        var parts = new List<string>(4);
        if (Control) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");
        if (Win) parts.Add("Win");
        parts.Add(string.IsNullOrWhiteSpace(KeyLabel) ? "?" : KeyLabel);
        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Persisted application settings (stored as JSON under %AppData%).
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>UI language code: "en" or "fa".</summary>
    public string UiLanguage { get; set; } = "en";

    /// <summary>Tesseract language codes to use, e.g. ["fas", "eng"].</summary>
    public List<string> OcrLanguages { get; set; } = new() { "eng", "fas" };

    public HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>
    /// Global shortcut for the in-place translation overlay (game/movie mode):
    /// capture a region, OCR + translate it, and show the result in a floating
    /// popup pinned near the selection instead of the main window. Default
    /// Ctrl+Shift+Z.
    /// </summary>
    public HotkeyConfig OverlayHotkey { get; set; } = new()
    {
        Control = true,
        Shift = true,
        Alt = false,
        Win = false,
        VirtualKey = 0x5A, // 'Z'
        KeyLabel = "Z"
    };

    public bool AutoCopy { get; set; } = true;

    /// <summary>When set, results are automatically translated to this language code.</summary>
    public string? AutoTranslateTo { get; set; }

    /// <summary>Improve OCR by upscaling/contrast before recognition.</summary>
    public bool PreprocessImage { get; set; } = true;

    public TranslationProviderKind TranslationProvider { get; set; } = TranslationProviderKind.Free;

    /// <summary>Base URL of the OpenAI-compatible endpoint, e.g. "https://api.openai.com/v1".</summary>
    public string AiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>API key for the custom AI endpoint (optional for local servers).</summary>
    public string? AiApiKey { get; set; }

    /// <summary>Model name sent to the AI endpoint, e.g. "gpt-4o-mini". This is the
    /// user's chosen default model and the first one tried for every translation.</summary>
    public string AiModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Models discovered from the custom provider's <c>/models</c> route (cached
    /// from the last successful "Test connection"). Used to populate the model
    /// picker without re-probing and as the candidate pool for automatic failover.
    /// </summary>
    public List<string> AiKnownModels { get; set; } = new();

    /// <summary>
    /// When true, a translation that times out (or fails on a model-specific
    /// error) is automatically retried against the other known models until one
    /// succeeds. Auth failures (401/403) never trigger failover.
    /// </summary>
    public bool AiModelFailover { get; set; } = true;

    /// <summary>Per-model request timeout in seconds before failover kicks in.
    /// Kept below the shared HttpClient's 30s ceiling so this budget is what
    /// actually triggers failover rather than the client-wide timeout.</summary>
    public int AiTimeoutSeconds { get; set; } = 20;

    /// <summary>Preferred default target language for on-demand translation.</summary>
    public string DefaultTranslateTarget { get; set; } = "en";

    /// <summary>
    /// How outbound requests reach the network. Defaults to the Windows system
    /// proxy. Switch to <see cref="NetworkProxyMode.Manual"/> to route through a
    /// local VPN/proxy, or <see cref="NetworkProxyMode.None"/> to force a direct
    /// connection when a stale system proxy is blocking requests.
    /// </summary>
    public NetworkProxyMode ProxyMode { get; set; } = NetworkProxyMode.System;

    /// <summary>
    /// Explicit proxy URL used when <see cref="ProxyMode"/> is Manual, e.g.
    /// "socks5://127.0.0.1:10808" or "http://127.0.0.1:10809". SOCKS5 is what
    /// tools like v2rayN/Xray expose by default.
    /// </summary>
    public string? ProxyAddress { get; set; }

    public bool MinimizeToTray { get; set; } = true;
}
