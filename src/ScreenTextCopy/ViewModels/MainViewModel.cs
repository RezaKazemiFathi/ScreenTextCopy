using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTextCopy.Models;
using ScreenTextCopy.Services;
using ScreenTextCopy.Views;

namespace ScreenTextCopy.ViewModels;

/// <summary>
/// View model for the main window: drives the capture -> OCR -> copy/translate
/// pipeline and exposes state for the UI to bind to.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private Window? _window;
    private CancellationTokenSource? _cts;

    // --- In-place overlay (game/movie mode) state ---
    // Tracked so the overlay hotkey can "retry" an open popup instead of starting
    // a fresh capture, and so retries can be cancelled/superseded independently.
    private TranslationOverlayWindow? _activeOverlay;
    private TranslationOverlayViewModel? _activeOverlayVm;
    private Int32Rect _activeOverlayRegion;
    private CancellationTokenSource? _overlayCts;

    public MainViewModel(AppServices services)
    {
        _services = services;
        _statusText = services.Localization.Get("status.ready");
        SelectedTranslateTarget = TranslateTargets
            .FirstOrDefault(l => l.Code == services.Settings.Current.DefaultTranslateTarget)
            ?? TranslateTargets[0];
    }

    public LocalizationService Loc => _services.Localization;

    /// <summary>Human-readable current global shortcut, e.g. "Ctrl + Shift + X", shown so new users know how to trigger capture.</summary>
    public string HotkeyDisplay => _services.Settings.Current.Hotkey.Describe();

    /// <summary>Re-reads the hotkey label after settings change so the header reflects a rebind.</summary>
    public void RefreshHotkeyDisplay() => OnPropertyChanged(nameof(HotkeyDisplay));

    /// <summary>Exposed so the window can wire the global hotkey and tray to shared services.</summary>
    public AppServices Services => _services;

    public IReadOnlyList<TranslationLanguage> TranslateTargets => TranslationService.SupportedLanguages;

    [ObservableProperty] private string _recognizedText = string.Empty;
    [ObservableProperty] private string _translatedText = string.Empty;
    [ObservableProperty] private string _statusText;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    private bool _hasRecognized;

    [ObservableProperty] private double? _confidence;
    [ObservableProperty] private long _elapsedMs;
    [ObservableProperty] private bool _showTranslation;
    [ObservableProperty] private TranslationLanguage _selectedTranslateTarget;

    // --- Toast (copy-success / empty-result feedback) ---
    [ObservableProperty] private bool _isToastVisible;
    [ObservableProperty] private string _toastText = string.Empty;
    [ObservableProperty] private bool _toastIsError;
    private CancellationTokenSource? _toastCts;

    /// <summary>
    /// Shows a transient toast message. Auto-hides after a short delay. Safe to
    /// call repeatedly; each call resets the timer.
    /// </summary>
    private void ShowToast(string message, bool isError)
    {
        ToastText = message;
        ToastIsError = isError;
        IsToastVisible = true;

        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        CancellationToken token = _toastCts.Token;

        _ = Task.Delay(2200, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                IsToastVisible = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public bool HasResult => HasRecognized && !string.IsNullOrWhiteSpace(RecognizedText);

    public int CharacterCount => RecognizedText.Length;

    public int WordCount =>
        string.IsNullOrWhiteSpace(RecognizedText)
            ? 0
            : RecognizedText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public void AttachWindow(Window window) => _window = window;

    partial void OnRecognizedTextChanged(string value)
    {
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(WordCount));
        OnPropertyChanged(nameof(HasResult));
    }

    /// <summary>
    /// Hides the main window, shows the selection overlay, captures the chosen
    /// region, runs OCR, then optionally copies and translates.
    /// </summary>
    [RelayCommand]
    private async Task CaptureAsync()
    {
        if (IsBusy)
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CancellationToken ct = _cts.Token;

        // Let the main window get out of the way before overlaying the screen,
        // otherwise it would appear in the captured pixels.
        WindowState previous = _window?.WindowState ?? WindowState.Normal;
        if (_window is not null)
            _window.Hide();

        // Give the compositor time to actually remove the window from the screen
        // before the overlay appears.
        await Task.Delay(120, ct);

        Int32Rect? region = SelectionOverlay.PickRegion();

        string? capturePath = null;
        if (region is { Width: > 0, Height: > 0 })
        {
            // CRITICAL: capture the pixels while the main window is still hidden
            // and the overlay has closed, so the screenshot contains exactly the
            // region the user selected — not the reappeared app window.
            try
            {
                // Let the overlay finish tearing down (its scrim must be gone).
                await Task.Delay(80, ct);
                Int32Rect r = region.Value;
                capturePath = _services.Capture.CaptureToFile(
                    r.X, r.Y, r.X + r.Width, r.Y + r.Height);
            }
            catch (Exception)
            {
                capturePath = null;
            }
        }

        // Now it is safe to bring the window back.
        if (_window is not null)
        {
            _window.Show();
            _window.WindowState = previous;
            _window.Activate();
        }

        if (capturePath is null)
        {
            StatusText = Loc.Get("status.ready");
            return;
        }

        await RunPipelineAsync(capturePath, ct);
    }

    /// <summary>
    /// In-place overlay ("game / movie" mode): pick a region, OCR + translate it,
    /// and show the result in a floating popup pinned near the selection instead
    /// of surfacing the main window. The main window stays hidden the whole time.
    /// </summary>
    [RelayCommand]
    private async Task CaptureOverlayAsync()
    {
        // If a popup is already open, pressing the overlay hotkey again means
        // "retry this translation" rather than starting a brand-new capture.
        if (_activeOverlay is not null && _activeOverlayVm is not null)
        {
            await RetryOverlayTranslationAsync().ConfigureAwait(true);
            return;
        }

        if (IsBusy)
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CancellationToken ct = _cts.Token;

        // Keep the main window out of the capture, but — unlike normal capture —
        // do NOT bring it back afterwards; the whole point is a non-intrusive popup.
        bool wasVisible = _window?.IsVisible ?? false;
        WindowState previous = _window?.WindowState ?? WindowState.Normal;
        if (_window is not null && wasVisible)
            _window.Hide();

        await Task.Delay(120, ct);

        Int32Rect? region = SelectionOverlay.PickRegion();

        string? capturePath = null;
        if (region is { Width: > 0, Height: > 0 })
        {
            try
            {
                await Task.Delay(80, ct);
                Int32Rect r = region.Value;
                capturePath = _services.Capture.CaptureToFile(
                    r.X, r.Y, r.X + r.Width, r.Y + r.Height);
            }
            catch (Exception)
            {
                capturePath = null;
            }
        }

        if (capturePath is null)
        {
            // Nothing captured (cancelled): restore the window if it had been shown.
            if (_window is not null && wasVisible)
            {
                _window.Show();
                _window.WindowState = previous;
                _window.Activate();
            }
            return;
        }

        await RunOverlayPipelineAsync(capturePath, region!.Value, ct);
    }

    private async Task RunOverlayPipelineAsync(string capturePath, Int32Rect regionPx, CancellationToken ct)
    {
        string? enhancedPath = null;

        // Spin up the floating popup immediately so the user sees progress. The
        // retry callback re-translates the recognised text already in the popup.
        var overlayVm = new TranslationOverlayViewModel(RetryOverlayTranslationAsync)
        {
            HeaderText = Loc.Get("overlay.recognizing"),
            IsBusy = true
        };
        var overlay = new TranslationOverlayWindow(overlayVm);
        TrackOverlay(overlay, overlayVm, regionPx);
        overlay.Show();
        overlay.PositionNear(regionPx);

        try
        {
            AppSettings settings = _services.Settings.Current;
            string ocrInput = capturePath;
            if (settings.PreprocessImage)
            {
                enhancedPath = ImagePreprocessor.Enhance(capturePath);
                ocrInput = enhancedPath;
            }

            OcrResult result = await _services.Ocr
                .RecognizeAsync(ocrInput, settings.OcrLanguages, ct)
                .ConfigureAwait(true);

            overlayVm.RecognizedText = result.Text;

            if (result.IsEmpty)
            {
                overlayVm.IsBusy = false;
                overlayVm.HeaderText = Loc.Get("overlay.empty");
                overlay.PositionNear(regionPx);
                return;
            }

            // Translate to the user's preferred target and show it in place.
            await TranslateOverlayAsync(overlayVm, regionPx, ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            overlay.Close();
        }
        catch (Exception ex)
        {
            // Surface the real reason (bad model, HTTP status, timeout, …) instead
            // of a misleading "add your API key" so the user can actually fix it.
            overlayVm.IsBusy = false;
            overlayVm.HeaderText = Loc.Get("overlay.error");
            overlayVm.ErrorText = DescribeTranslationError(ex);
            overlay.PositionNear(regionPx);
        }
        finally
        {
            TryDelete(capturePath);
            if (enhancedPath is not null && enhancedPath != capturePath)
                TryDelete(enhancedPath);
        }
    }

    /// <summary>
    /// Runs (or re-runs) the translation for the text currently shown in the
    /// overlay popup. Throws on failure so the caller can render the error.
    /// </summary>
    private async Task TranslateOverlayAsync(
        TranslationOverlayViewModel overlayVm, Int32Rect regionPx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(overlayVm.RecognizedText))
            return;

        AppSettings settings = _services.Settings.Current;
        overlayVm.ErrorText = string.Empty;
        overlayVm.IsBusy = true;
        overlayVm.HeaderText = Loc.Get("overlay.translating");

        string target = string.IsNullOrWhiteSpace(settings.AutoTranslateTo)
            ? settings.DefaultTranslateTarget
            : settings.AutoTranslateTo!;

        string translated = await _services.Translation
            .TranslateAsync(overlayVm.RecognizedText, target, ct)
            .ConfigureAwait(true);

        overlayVm.TranslatedText = string.IsNullOrWhiteSpace(translated)
            ? overlayVm.RecognizedText
            : translated;
        overlayVm.HeaderText = Loc.Get("overlay.done");
        overlayVm.IsBusy = false;
        _activeOverlay?.PositionNear(regionPx);
    }

    /// <summary>
    /// Retry/refresh entry point: re-translates the active overlay's text. Wired
    /// to the popup's Retry button and to a second press of the overlay hotkey.
    /// </summary>
    private async Task RetryOverlayTranslationAsync()
    {
        TranslationOverlayViewModel? vm = _activeOverlayVm;
        if (_activeOverlay is null || vm is null || vm.IsBusy ||
            string.IsNullOrWhiteSpace(vm.RecognizedText))
        {
            return;
        }

        // A fresh, independent token so a retry can supersede a previous slow one
        // without being tied to the (already-finished) capture pipeline.
        _overlayCts?.Cancel();
        _overlayCts = new CancellationTokenSource();
        CancellationToken ct = _overlayCts.Token;

        try
        {
            await TranslateOverlayAsync(vm, _activeOverlayRegion, ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer retry; leave the popup as-is.
        }
        catch (Exception ex)
        {
            vm.IsBusy = false;
            vm.HeaderText = Loc.Get("overlay.error");
            vm.ErrorText = DescribeTranslationError(ex);
        }
    }

    /// <summary>Remembers the live popup so the hotkey/retry can target it, and clears it on close.</summary>
    private void TrackOverlay(TranslationOverlayWindow overlay, TranslationOverlayViewModel vm, Int32Rect regionPx)
    {
        _activeOverlay = overlay;
        _activeOverlayVm = vm;
        _activeOverlayRegion = regionPx;
        overlay.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activeOverlay, overlay))
            {
                _activeOverlay = null;
                _activeOverlayVm = null;
            }
        };
    }

    /// <summary>
    /// Maps a translation failure to a user-facing message. AI provider errors
    /// keep their real detail (HTTP status, model name, timeout) so the cause is
    /// visible; auth failures point at the API key; everything else is generic.
    /// </summary>
    private string DescribeTranslationError(Exception ex) => ex switch
    {
        AiRequestException { IsTimeout: true } => Loc.Get("error.timeout"),
        AiRequestException { IsAuthFailure: true } => Loc.Get("error.noApiKey"),
        AiRequestException aiEx => aiEx.Message,
        InvalidOperationException ioEx => ioEx.Message,
        _ => Loc.Get("error.translate")
    };

    private async Task RunPipelineAsync(string capturePath, CancellationToken ct)
    {
        IsBusy = true;
        ShowTranslation = false;
        TranslatedText = string.Empty;
        string? enhancedPath = null;

        try
        {
            StatusText = Loc.Get("status.recognizing");

            AppSettings settings = _services.Settings.Current;
            string ocrInput = capturePath;
            if (settings.PreprocessImage)
            {
                enhancedPath = ImagePreprocessor.Enhance(capturePath);
                ocrInput = enhancedPath;
            }

            OcrResult result = await _services.Ocr
                .RecognizeAsync(ocrInput, settings.OcrLanguages, ct)
                .ConfigureAwait(true);

            RecognizedText = result.Text;
            Confidence = result.MeanConfidence;
            ElapsedMs = result.ElapsedMilliseconds;
            HasRecognized = true;

            if (result.IsEmpty)
            {
                StatusText = Loc.Get("result.empty");
                ShowToast(Loc.Get("result.empty"), isError: true);
                return;
            }

            if (settings.AutoCopy)
            {
                if (TrySetClipboard(RecognizedText))
                    ShowToast(Loc.Get("toast.copied"), isError: false);
            }

            // Be honest: Tesseract cannot read emoji/pictographs, so any that
            // were present have been dropped. Tell the user rather than pretend.
            if (result.HasUnrecognizableGlyphs)
                ShowToast(Loc.Get("toast.emojiDropped"), isError: false);

            if (!string.IsNullOrWhiteSpace(settings.AutoTranslateTo))
            {
                await TranslateToAsync(settings.AutoTranslateTo!, ct).ConfigureAwait(true);
            }

            StatusText = Loc.Get("status.done");
        }
        catch (OperationCanceledException)
        {
            StatusText = Loc.Get("status.ready");
        }
        catch (Exception)
        {
            StatusText = Loc.Get("error.ocr");
        }
        finally
        {
            TryDelete(capturePath);
            if (enhancedPath is not null && enhancedPath != capturePath)
                TryDelete(enhancedPath);
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Copy()
    {
        if (string.IsNullOrEmpty(RecognizedText))
            return;
        if (TrySetClipboard(RecognizedText))
        {
            StatusText = Loc.Get("action.copied");
            ShowToast(Loc.Get("toast.copied"), isError: false);
        }
        else
        {
            ShowToast(Loc.Get("toast.copyFailed"), isError: true);
        }
    }

    [RelayCommand]
    private void CopyTranslation()
    {
        if (string.IsNullOrEmpty(TranslatedText))
            return;
        if (TrySetClipboard(TranslatedText))
        {
            StatusText = Loc.Get("action.copied");
            ShowToast(Loc.Get("toast.copied"), isError: false);
        }
        else
        {
            ShowToast(Loc.Get("toast.copyFailed"), isError: true);
        }
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (!HasResult || IsBusy)
            return;

        _cts ??= new CancellationTokenSource();
        await TranslateToAsync(SelectedTranslateTarget.Code, _cts.Token);
    }

    private async Task TranslateToAsync(string targetCode, CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            StatusText = Loc.Get("status.translating");
            string translated = await _services.Translation
                .TranslateAsync(RecognizedText, targetCode, ct)
                .ConfigureAwait(true);

            TranslatedText = translated;
            ShowTranslation = !string.IsNullOrWhiteSpace(translated);
            StatusText = Loc.Get("status.done");
        }
        catch (OperationCanceledException)
        {
            StatusText = Loc.Get("status.ready");
        }
        catch (Exception ex)
        {
            // Show the real reason (HTTP status / bad model / timeout) rather than
            // always blaming a missing API key.
            StatusText = DescribeTranslationError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SendToMobile()
    {
        if (!HasResult)
            return;

        try
        {
            BitmapSource qr = _services.Qr.Generate(RecognizedText);
            bool tooLong = _services.Qr.ExceedsRecommended(RecognizedText);
            var window = new QrWindow(qr, Loc, tooLong) { Owner = _window };
            window.ShowDialog();
        }
        catch (Exception)
        {
            StatusText = Loc.Get("status.error");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        RecognizedText = string.Empty;
        TranslatedText = string.Empty;
        ShowTranslation = false;
        HasRecognized = false;
        Confidence = null;
        ElapsedMs = 0;
        StatusText = Loc.Get("status.ready");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_services);
        var window = new SettingsWindow { DataContext = vm, Owner = _window };
        bool? saved = window.ShowDialog();

        if (saved == true)
        {
            // A rebind may have changed the global shortcut: re-register it and
            // refresh the on-screen hint so the header stays accurate.
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
            RefreshHotkeyDisplay();
        }

        // Reflect any language/theme change immediately.
        StatusText = Loc.Get("status.ready");
    }

    /// <summary>Raised after settings are saved so the view can re-register the global hotkey.</summary>
    public event EventHandler? HotkeyChanged;

    private static bool TrySetClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best effort
        }
    }
}
