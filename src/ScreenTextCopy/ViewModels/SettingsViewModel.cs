using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTextCopy.Models;
using ScreenTextCopy.Services;

namespace ScreenTextCopy.ViewModels;

/// <summary>
/// A single OCR language row in the settings list, with an install action for
/// packs that are not yet present on disk.
/// </summary>
public sealed partial class OcrLanguageItem : ObservableObject
{
    private readonly LanguagePackService _packs;

    public OcrLanguageItem(LanguagePack pack, bool isSelected, LanguagePackService packs)
    {
        _packs = packs;
        Code = pack.Code;
        DisplayName = $"{pack.NativeName} ({pack.EnglishName})";
        _isInstalled = pack.IsInstalled;
        _isSelected = isSelected;
    }

    public string Code { get; }
    public string DisplayName { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private double _progress;

    [RelayCommand]
    private async Task InstallAsync()
    {
        if (IsInstalled || IsInstalling)
            return;

        IsInstalling = true;
        try
        {
            var progress = new Progress<double>(p => Progress = p);
            await _packs.DownloadAsync(Code, progress).ConfigureAwait(true);
            IsInstalled = _packs.IsInstalled(Code);
            if (IsInstalled)
                IsSelected = true;
        }
        catch
        {
            // Surface failure by leaving IsInstalled false; the row stays actionable.
        }
        finally
        {
            IsInstalling = false;
        }
    }
}

/// <summary>
/// View model for the settings window. Applies theme/language changes live and
/// persists the full settings object on save.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        AppSettings s = services.Settings.Current;

        _theme = s.Theme;
        _selectedUiLanguage = services.Localization.AvailableLanguages
            .FirstOrDefault(l => l.Code == s.UiLanguage) ?? services.Localization.AvailableLanguages[0];
        _preprocessImage = s.PreprocessImage;
        _autoCopy = s.AutoCopy;
        _minimizeToTray = s.MinimizeToTray;
        _provider = s.TranslationProvider;
        _aiBaseUrl = s.AiBaseUrl;
        _aiApiKey = s.AiApiKey ?? string.Empty;
        _aiModel = s.AiModel;
        _aiModelFailover = s.AiModelFailover;
        _proxyMode = s.ProxyMode;
        _proxyAddress = s.ProxyAddress ?? string.Empty;

        // Seed the model list from the models discovered on the last successful
        // "Test connection" (persisted), plus the chosen default. Without a
        // matching item, an editable ComboBox blanks its Text on load, and the
        // user would have to re-probe before the picker/failover pool is usable.
        foreach (string m in s.AiKnownModels)
        {
            if (!string.IsNullOrWhiteSpace(m) && !AvailableModels.Contains(m))
                AvailableModels.Add(m);
        }
        if (!string.IsNullOrWhiteSpace(s.AiModel) && !AvailableModels.Contains(s.AiModel))
            AvailableModels.Insert(0, s.AiModel);

        _autoTranslate = !string.IsNullOrWhiteSpace(s.AutoTranslateTo);
        _autoTranslateTarget = TranslateTargets
            .FirstOrDefault(l => l.Code == s.AutoTranslateTo)
            ?? TranslateTargets.FirstOrDefault(l => l.Code == s.DefaultTranslateTarget)
            ?? TranslateTargets[0];

        // Snapshot both global hotkeys into editors so they can be rebound and
        // previewed live, sharing the same capture/validation logic.
        HotkeyEditor = new HotkeyEditor(s.Hotkey);
        OverlayHotkeyEditor = new HotkeyEditor(s.OverlayHotkey);

        var selected = new HashSet<string>(s.OcrLanguages, StringComparer.Ordinal);
        OcrLanguages = new ObservableCollection<OcrLanguageItem>(
            services.LanguagePacks.GetPacks()
                .Select(p => new OcrLanguageItem(p, selected.Contains(p.Code), services.LanguagePacks)));
    }

    public LocalizationService Loc => _services.Localization;

    public IReadOnlyList<UiLanguage> UiLanguages => _services.Localization.AvailableLanguages;
    public IReadOnlyList<TranslationLanguage> TranslateTargets => TranslationService.SupportedLanguages;
    public Array Themes => Enum.GetValues(typeof(AppTheme));
    public ObservableCollection<OcrLanguageItem> OcrLanguages { get; }

    [ObservableProperty] private AppTheme _theme;
    [ObservableProperty] private UiLanguage _selectedUiLanguage;
    [ObservableProperty] private bool _preprocessImage;
    [ObservableProperty] private bool _autoCopy;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _autoTranslate;
    [ObservableProperty] private TranslationLanguage _autoTranslateTarget;
    [ObservableProperty] private TranslationProviderKind _provider;
    [ObservableProperty] private string _aiBaseUrl;
    [ObservableProperty] private string _aiApiKey;
    [ObservableProperty] private string _aiModel;
    [ObservableProperty] private bool _aiModelFailover;

    // --- Network proxy ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualProxy))]
    private NetworkProxyMode _proxyMode;

    [ObservableProperty] private string _proxyAddress;

    /// <summary>Show the manual proxy address field only when Manual is selected.</summary>
    public bool IsManualProxy => ProxyMode == NetworkProxyMode.Manual;

    // --- Global hotkeys (editable / rebindable) ---
    /// <summary>Editor for the main capture shortcut.</summary>
    public HotkeyEditor HotkeyEditor { get; }

    /// <summary>Editor for the in-place translation overlay shortcut.</summary>
    public HotkeyEditor OverlayHotkeyEditor { get; }
    [ObservableProperty] private bool _isProbing;
    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private bool _connectionOk;
    [ObservableProperty] private bool _connectionChecked;
    public ObservableCollection<string> AvailableModels { get; } = new();

    public bool IsCustomAi => Provider == TranslationProviderKind.CustomAi;

    private CustomAiConfig CurrentAiConfig =>
        new(string.IsNullOrWhiteSpace(AiBaseUrl) ? "https://api.openai.com/v1" : AiBaseUrl.Trim(),
            string.IsNullOrWhiteSpace(AiApiKey) ? null : AiApiKey.Trim(),
            string.IsNullOrWhiteSpace(AiModel) ? "gpt-4o-mini" : AiModel.Trim());

    /// <summary>
    /// Probes the configured endpoint for reachability + latency and, on
    /// success, pulls the list of available models so the user can pick one.
    /// Works with any OpenAI-compatible provider.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsProbing)
            return;

        IsProbing = true;
        ConnectionChecked = true;
        ConnectionStatus = _services.Localization.Get("settings.testing");
        try
        {
            CustomAiConfig config = CurrentAiConfig;
            ProviderProbeResult probe = await _services.CustomAi
                .TestConnectionAsync(config)
                .ConfigureAwait(true);

            ConnectionOk = probe.Success;
            ConnectionStatus = probe.Success
                ? $"{_services.Localization.Get("settings.connected")} · {probe.LatencyMs} ms"
                : $"{_services.Localization.Get("settings.connectFailed")} · {probe.Message}";

            if (probe.Success)
            {
                int count = await LoadModelsAsync(config).ConfigureAwait(true);
                // Tell the user whether the model list actually came back — an empty
                // list is why the picker looks broken even on a green connection.
                ConnectionStatus += count > 0
                    ? $" · {count} models"
                    : " · no model list";
            }
        }
        finally
        {
            IsProbing = false;
        }
    }

    /// <summary>
    /// Explicitly re-queries the provider's /models route and refreshes the
    /// picker without running a full connection probe. Bound to the
    /// "Refresh model list" button.
    /// </summary>
    [RelayCommand]
    private async Task RefreshModelsAsync()
    {
        if (IsProbing)
            return;

        IsProbing = true;
        try
        {
            int count = await LoadModelsAsync(CurrentAiConfig).ConfigureAwait(true);
            ConnectionChecked = true;
            ConnectionStatus = count > 0
                ? $"{_services.Localization.Get("settings.connected")} · {count} models"
                : $"{_services.Localization.Get("settings.connectFailed")} · no model list";
            ConnectionOk = count > 0;
        }
        finally
        {
            IsProbing = false;
        }
    }

    /// <summary>
    /// Pulls the available models from the endpoint, repopulates the picker
    /// (keeping the currently chosen model even if the route omits it), and
    /// returns how many were found. Never throws.
    /// </summary>
    private async Task<int> LoadModelsAsync(CustomAiConfig config)
    {
        try
        {
            IReadOnlyList<string> models = await _services.CustomAi
                .ListModelsAsync(config)
                .ConfigureAwait(true);

            string? previous = AiModel;
            AvailableModels.Clear();
            foreach (string m in models)
                AvailableModels.Add(m);

            if (!string.IsNullOrWhiteSpace(previous) && !AvailableModels.Contains(previous))
                AvailableModels.Insert(0, previous);

            // Clearing the collection blanks the editable ComboBox's bound Text.
            // Re-assert the chosen model so the picker keeps showing it after a
            // refresh instead of going empty.
            if (!string.IsNullOrWhiteSpace(previous))
                AiModel = previous;

            return models.Count;
        }
        catch
        {
            // Endpoint reachable but no /models route: leave the field free-text.
            return 0;
        }
    }

    partial void OnThemeChanged(AppTheme value) => _services.Theme.Apply(value);

    partial void OnSelectedUiLanguageChanged(UiLanguage value)
    {
        if (value is not null)
            _services.Localization.SetLanguage(value.Code);
    }

    partial void OnProviderChanged(TranslationProviderKind value) => OnPropertyChanged(nameof(IsCustomAi));

    /// <summary>Collects the current UI state into an <see cref="AppSettings"/> and persists it.</summary>
    public void Persist()
    {
        AppSettings s = _services.Settings.Current;
        s.Theme = Theme;
        s.UiLanguage = SelectedUiLanguage.Code;
        s.PreprocessImage = PreprocessImage;
        s.AutoCopy = AutoCopy;
        s.MinimizeToTray = MinimizeToTray;
        s.TranslationProvider = Provider;
        s.AiBaseUrl = string.IsNullOrWhiteSpace(AiBaseUrl)
            ? "https://api.openai.com/v1"
            : AiBaseUrl.Trim();
        s.AiApiKey = string.IsNullOrWhiteSpace(AiApiKey) ? null : AiApiKey.Trim();
        s.AiModel = string.IsNullOrWhiteSpace(AiModel) ? "gpt-4o-mini" : AiModel.Trim();
        s.AiModelFailover = AiModelFailover;

        s.ProxyMode = ProxyMode;
        s.ProxyAddress = string.IsNullOrWhiteSpace(ProxyAddress) ? null : ProxyAddress.Trim();

        // Persist the discovered models so the picker and the failover candidate
        // pool are available on the next launch without re-probing.
        s.AiKnownModels = AvailableModels
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        s.AutoTranslateTo = AutoTranslate ? AutoTranslateTarget.Code : null;
        s.DefaultTranslateTarget = AutoTranslateTarget.Code;

        HotkeyEditor.ApplyTo(s.Hotkey);
        OverlayHotkeyEditor.ApplyTo(s.OverlayHotkey);

        var chosen = OcrLanguages
            .Where(l => l.IsSelected && l.IsInstalled)
            .Select(l => l.Code)
            .ToList();
        if (chosen.Count > 0)
            s.OcrLanguages = chosen;

        _services.Settings.Save(s);
    }
}
