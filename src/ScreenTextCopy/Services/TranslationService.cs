using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// One target language offered in the translation picker.
/// </summary>
public sealed record TranslationLanguage(string Code, string EnglishName, string NativeName);

/// <summary>
/// Chooses a translation provider based on current settings and exposes the set
/// of supported target languages.
/// </summary>
public sealed class TranslationService
{
    private readonly SettingsService _settings;
    private readonly FreeTranslationProvider _free;
    private readonly CustomAiTranslationProvider _customAi;

    public TranslationService(
        SettingsService settings,
        FreeTranslationProvider free,
        CustomAiTranslationProvider customAi)
    {
        _settings = settings;
        _free = free;
        _customAi = customAi;
    }

    /// <summary>Common target languages. Extendable without code changes elsewhere.</summary>
    public static IReadOnlyList<TranslationLanguage> SupportedLanguages { get; } = new[]
    {
        new TranslationLanguage("en", "English", "English"),
        new TranslationLanguage("fa", "Persian", "فارسی"),
        new TranslationLanguage("ar", "Arabic", "العربية"),
        new TranslationLanguage("fr", "French", "Français"),
        new TranslationLanguage("de", "German", "Deutsch"),
        new TranslationLanguage("es", "Spanish", "Español"),
        new TranslationLanguage("it", "Italian", "Italiano"),
        new TranslationLanguage("ru", "Russian", "Русский"),
        new TranslationLanguage("tr", "Turkish", "Türkçe"),
        new TranslationLanguage("zh", "Chinese", "中文"),
        new TranslationLanguage("ja", "Japanese", "日本語"),
        new TranslationLanguage("ko", "Korean", "한국어"),
        new TranslationLanguage("hi", "Hindi", "हिन्दी"),
        new TranslationLanguage("pt", "Portuguese", "Português")
    };

    private ITranslationProvider Provider =>
        _settings.Current.TranslationProvider == TranslationProviderKind.CustomAi
            ? _customAi
            : _free;

    public Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        // Detect the source language from the text itself instead of assuming
        // English. Passing the real source is what makes e.g. Persian -> English
        // actually translate (the free MyMemory backend echoes the input back
        // when told the source is the same as the content but labelled "en").
        string source = TextDirection.DetectLanguage(text);

        // Nothing to do when the text is already in the requested language.
        if (string.Equals(source, targetLanguage, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(text);

        return Provider.TranslateAsync(text, targetLanguage, source, cancellationToken);
    }
}
