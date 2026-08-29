using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using ScreenTextCopy.Localization;
using ScreenTextCopy.Services;
using ScreenTextCopy.ViewModels;
using ScreenTextCopy.Views;

namespace ScreenTextCopy;

/// <summary>
/// Application composition root. Wires the service layer together (manual DI —
/// no external container needed), bootstraps theme + locale, and shows the main
/// window. Also owns the shared <see cref="HttpClient"/>.
/// </summary>
public partial class App : System.Windows.Application
{
    private HttpClient? _http;
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnUnhandledException;

        // --- Settings ---
        var settingsService = new SettingsService();
        Models.AppSettings settings = settingsService.Load();

        // --- Localization + theme (must happen before any window is created) ---
        var localization = new LocalizationService();
        localization.SetLanguage(settings.UiLanguage);
        LocalizationHub.Initialize(localization);

        var theme = new ThemeService();
        theme.Apply(settings.Theme);

        // --- HTTP (shared) ---
        // A custom proxy that reads settings live, so the user can route AI
        // traffic through a local VPN/proxy (e.g. socks5://127.0.0.1:10808) or
        // force a direct connection without restarting the app.
        var handler = new HttpClientHandler
        {
            UseProxy = true,
            Proxy = new SettingsWebProxy(settingsService)
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ScreenTextCopy/2.0");

        // --- Core services ---
        var capture = new ScreenCaptureService();
        var ocr = new TesseractOcrEngine();
        var languagePacks = new LanguagePackService(_http, ocr);

        var free = new FreeTranslationProvider(_http);
        var customAi = new CustomAiTranslationProvider(
            _http,
            () =>
            {
                Models.AppSettings s = settingsService.Current;
                return new CustomAiConfig(
                    s.AiBaseUrl,
                    s.AiApiKey,
                    s.AiModel,
                    FallbackModels: s.AiKnownModels,
                    TimeoutSeconds: s.AiTimeoutSeconds,
                    EnableFailover: s.AiModelFailover);
            });
        var translation = new TranslationService(settingsService, free, customAi);

        var qr = new QrCodeService();

        _services = new AppServices(
            settingsService, localization, theme, capture, ocr,
            languagePacks, translation, qr, customAi);

        // --- Main window + view model ---
        var mainViewModel = new MainViewModel(_services);
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainViewModel.AttachWindow(mainWindow);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.Message,
            "ScreenTextCopy",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _http?.Dispose();
        base.OnExit(e);
    }
}

/// <summary>
/// Simple service aggregate passed to view models (manual dependency injection).
/// </summary>
public sealed class AppServices : IDisposable
{
    public AppServices(
        SettingsService settings,
        LocalizationService localization,
        ThemeService theme,
        ScreenCaptureService capture,
        TesseractOcrEngine ocr,
        LanguagePackService languagePacks,
        TranslationService translation,
        QrCodeService qr,
        CustomAiTranslationProvider customAi)
    {
        Settings = settings;
        Localization = localization;
        Theme = theme;
        Capture = capture;
        Ocr = ocr;
        LanguagePacks = languagePacks;
        Translation = translation;
        Qr = qr;
        CustomAi = customAi;
    }

    public SettingsService Settings { get; }
    public LocalizationService Localization { get; }
    public ThemeService Theme { get; }
    public ScreenCaptureService Capture { get; }
    public TesseractOcrEngine Ocr { get; }
    public LanguagePackService LanguagePacks { get; }
    public TranslationService Translation { get; }
    public QrCodeService Qr { get; }

    /// <summary>Exposed directly so the settings UI can probe/list models.</summary>
    public CustomAiTranslationProvider CustomAi { get; }

    public void Dispose()
    {
        // Nothing owns unmanaged handles here yet; kept for future services.
    }
}
