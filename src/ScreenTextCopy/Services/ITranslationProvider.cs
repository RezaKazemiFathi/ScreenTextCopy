namespace ScreenTextCopy.Services;

/// <summary>
/// A translation backend. Implementations may be free (no key) or require an
/// API key. Only text explicitly submitted here leaves the machine.
/// </summary>
public interface ITranslationProvider
{
    /// <summary>Stable identifier, e.g. "free" or "openai".</summary>
    string Id { get; }

    /// <summary>True if the provider needs a user-supplied API key to work.</summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Translates <paramref name="text"/> into <paramref name="targetLanguage"/>
    /// (ISO code such as "en", "fa", "de"). <paramref name="sourceLanguage"/> may
    /// be "auto" for automatic detection.
    /// </summary>
    Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string sourceLanguage = "auto",
        CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a provider connectivity probe.</summary>
public sealed record ProviderProbeResult(bool Success, long LatencyMs, string Message);
