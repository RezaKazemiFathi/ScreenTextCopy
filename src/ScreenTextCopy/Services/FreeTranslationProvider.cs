using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace ScreenTextCopy.Services;

/// <summary>
/// Free translation provider backed by the MyMemory public API. No API key
/// required. Text is chunked to respect the per-request length limit.
/// </summary>
public sealed class FreeTranslationProvider : ITranslationProvider
{
    // MyMemory recommends <= 500 bytes per query for the free tier.
    private const int MaxChunkChars = 480;

    private readonly HttpClient _http;

    public FreeTranslationProvider(HttpClient http)
    {
        _http = http;
    }

    public string Id => "free";

    public bool RequiresApiKey => false;

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string sourceLanguage = "auto",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // MyMemory does not accept "auto"; default unknown source to English.
        string source = string.IsNullOrWhiteSpace(sourceLanguage) || sourceLanguage == "auto"
            ? "en"
            : sourceLanguage;

        var results = new List<string>();
        foreach (string chunk in ChunkByLines(text, MaxChunkChars))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await TranslateChunkAsync(chunk, source, targetLanguage, cancellationToken)
                .ConfigureAwait(false));
        }

        return string.Join("\n", results);
    }

    private async Task<string> TranslateChunkAsync(
        string chunk,
        string source,
        string target,
        CancellationToken ct)
    {
        string url =
            "https://api.mymemory.translated.net/get?q=" +
            Uri.EscapeDataString(chunk) +
            "&langpair=" + Uri.EscapeDataString(source) + "|" + Uri.EscapeDataString(target);

        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("Translation rate limit reached. Try again shortly.");

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct)
            .ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("responseData", out JsonElement data) &&
            data.TryGetProperty("translatedText", out JsonElement translated) &&
            translated.ValueKind == JsonValueKind.String)
        {
            return WebUtility.HtmlDecode(translated.GetString()) ?? string.Empty;
        }

        throw new InvalidOperationException("Unexpected translation response.");
    }

    /// <summary>Splits text into chunks not exceeding <paramref name="maxChars"/>, on line boundaries where possible.</summary>
    private static IEnumerable<string> ChunkByLines(string text, int maxChars)
    {
        var current = new System.Text.StringBuilder();
        foreach (string line in text.Split('\n'))
        {
            if (line.Length > maxChars)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                for (int i = 0; i < line.Length; i += maxChars)
                    yield return line.Substring(i, Math.Min(maxChars, line.Length - i));

                continue;
            }

            if (current.Length + line.Length + 1 > maxChars && current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
                current.Append('\n');
            current.Append(line);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }
}
