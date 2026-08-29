using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ScreenTextCopy.ViewModels;

/// <summary>
/// State for the in-place translation overlay popup (game/movie mode): the
/// recognised source text, its translation, and progress/header text. Kept
/// separate from <see cref="MainViewModel"/> so the floating window can live and
/// update independently of the main shell.
/// </summary>
public sealed partial class TranslationOverlayViewModel : ObservableObject
{
    private readonly Func<Task>? _retry;

    public TranslationOverlayViewModel(Func<Task>? retry = null)
    {
        _retry = retry;
    }

    [ObservableProperty] private string _recognizedText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOriginal))]
    private string _translatedText = string.Empty;

    [ObservableProperty] private string _headerText = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowError))]
    private string _errorText = string.Empty;

    /// <summary>Show the original text section only once we actually have some.</summary>
    public bool ShowOriginal => !string.IsNullOrWhiteSpace(RecognizedText);

    /// <summary>Show the (detailed) error banner only when a translation actually failed.</summary>
    public bool ShowError => !string.IsNullOrWhiteSpace(ErrorText);

    partial void OnRecognizedTextChanged(string value) => OnPropertyChanged(nameof(ShowOriginal));

    /// <summary>
    /// Re-runs the translation for the text currently in the popup. Bound to the
    /// Retry button and re-invoked when the overlay hotkey is pressed again.
    /// </summary>
    [RelayCommand]
    private Task Retry() => _retry?.Invoke() ?? Task.CompletedTask;
}
