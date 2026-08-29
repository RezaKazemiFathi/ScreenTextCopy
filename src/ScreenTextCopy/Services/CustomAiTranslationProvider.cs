using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScreenTextCopy.Services;

/// <summary>
/// Accessor for the custom AI provider configuration, resolved lazily from
/// current settings so changes take effect without rebuilding the provider.
/// </summary>
/// <param name="BaseUrl">OpenAI-compatible base URL (e.g. ".../v1").</param>
/// <param name="ApiKey">Optional bearer key (local servers may need none).</param>
/// <param name="Model">The user's default/primary model, tried first.</param>
/// <param name="FallbackModels">Other known models to try if the primary times
/// out or fails with a model-specific error. Ignored when <paramref name="EnableFailover"/> is false.</param>
/// <param name="TimeoutSeconds">Per-model request timeout before failover.</param>
/// <param name="EnableFailover">Whether to fail over to other models on timeout/model error.</param>
public sealed record CustomAiConfig(
    string BaseUrl,
    string? ApiKey,
    string Model,
    IReadOnlyList<string>? FallbackModels = null,
    int TimeoutSeconds = 30,
    bool EnableFailover = true);

/// <summary>
/// Error from the custom AI endpoint, carrying enough context for callers to
/// show a real message and decide whether failover makes sense. Never contains
/// the API key.
/// </summary>
public sealed class AiRequestException : Exception
{
    public AiRequestException(
        string message, int? statusCode = null, bool isTimeout = false,
        bool isNetwork = false, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        IsTimeout = isTimeout;
        IsNetwork = isNetwork;
    }

    /// <summary>HTTP status code when the failure was an HTTP error, else null.</summary>
    public int? StatusCode { get; }

    /// <summary>True when the request exceeded the per-model timeout.</summary>
    public bool IsTimeout { get; }

    /// <summary>
    /// True when the endpoint could not be reached at all (DNS/TLS/connection
    /// refused). Trying another model against the same host would fail the same
    /// way, so this never triggers failover.
    /// </summary>
    public bool IsNetwork { get; }

    /// <summary>Auth failures cannot be fixed by trying another model with the same key.</summary>
    public bool IsAuthFailure => StatusCode is 401 or 403;
}

/// <summary>
/// Translation provider that talks to any OpenAI-compatible chat-completions
/// endpoint. The user supplies a base URL, an optional API key and a model
/// name, so it works with OpenAI, Azure OpenAI, OpenRouter, Groq, local
/// llama.cpp / LM Studio / Ollama (OpenAI mode) and similar services.
/// </summary>
public sealed class CustomAiTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _http;
    private readonly Func<CustomAiConfig> _configAccessor;

    public CustomAiTranslationProvider(HttpClient http, Func<CustomAiConfig> configAccessor)
    {
        _http = http;
        _configAccessor = configAccessor;
    }

    public string Id => "custom";

    public bool RequiresApiKey => false; // Some local endpoints need no key.

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string sourceLanguage = "auto",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        CustomAiConfig config = _configAccessor();
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new AiRequestException("A base URL is required for the custom AI provider.");
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new AiRequestException("A model name is required for the custom AI provider.");

        string endpoint = BuildEndpoint(config.BaseUrl);
        string systemPrompt = BuildSystemPrompt(targetLanguage);

        // Try the user's default model first, then any known fallbacks. Automatic
        // failover means a single slow/broken model no longer blocks translation.
        IReadOnlyList<string> candidates = BuildModelCandidates(config);

        AiRequestException? lastError = null;
        foreach (string model in candidates)
        {
            try
            {
                return await TranslateOnceAsync(
                    endpoint, config, model, systemPrompt, text, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AiRequestException ex) when (ex.IsAuthFailure || ex.IsNetwork)
            {
                // Auth failure (wrong/expired key) and a genuinely unreachable
                // endpoint will fail identically for every other model on the same
                // host, so stop immediately instead of hanging through the whole list.
                throw;
            }
            catch (AiRequestException ex)
            {
                // Timeout or model-specific error: remember it and try the next model.
                lastError = ex;
            }
        }

        // Every candidate failed (or the only one did). Surface the last real reason.
        throw lastError ?? new AiRequestException("The AI service could not be reached.");
    }

    /// <summary>
    /// Ordered, de-duplicated list of models to try: primary first, then
    /// fallbacks. Capped so a long known-model list can't turn a dead endpoint
    /// into a multi-minute freeze (each timeout costs up to the per-model budget).
    /// </summary>
    private const int MaxFailoverCandidates = 5;

    private static IReadOnlyList<string> BuildModelCandidates(CustomAiConfig config)
    {
        var candidates = new List<string> { config.Model };
        if (config.EnableFailover && config.FallbackModels is { Count: > 0 })
        {
            foreach (string m in config.FallbackModels)
            {
                if (candidates.Count >= MaxFailoverCandidates)
                    break;
                if (!string.IsNullOrWhiteSpace(m) &&
                    !candidates.Contains(m, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(m);
                }
            }
        }

        return candidates;
    }

    private static string BuildSystemPrompt(string targetLanguage) =>
        $"You are a professional translator. Translate the user's text into {DescribeLanguage(targetLanguage)}. " +
        "Keep line breaks, numbers, code, URLs, file paths and error codes exactly as-is. " +
        "Do not add explanations. Return only the translated text.";

    /// <summary>
    /// Sends one chat-completion request for a specific model with a per-model
    /// timeout. Distinguishes a genuine user cancellation from a timeout so the
    /// caller can fail over. Throws <see cref="AiRequestException"/> on failure.
    /// </summary>
    private async Task<string> TranslateOnceAsync(
        string endpoint,
        CustomAiConfig config,
        string model,
        string systemPrompt,
        string text,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = text }
            }
        };

        // Per-model timeout, linked to the caller's token. The HttpClient's own
        // timeout resets on every SendAsync, so each failover attempt gets a full
        // window; this shorter budget is what actually triggers failover.
        int seconds = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(seconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage response =
                await _http.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
                // Never surface the API key; only the status and a short body snippet.
                throw new AiRequestException(
                    $"AI request failed ({(int)response.StatusCode}) for model '{model}'. {Truncate(body, 300)}",
                    statusCode: (int)response.StatusCode);
            }

            await using Stream stream =
                await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
            using JsonDocument doc =
                await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out JsonElement message) &&
                message.TryGetProperty("content", out JsonElement content) &&
                content.ValueKind == JsonValueKind.String)
            {
                return content.GetString()?.Trim() ?? string.Empty;
            }

            throw new AiRequestException($"Model '{model}' returned an unexpected response.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The linked token fired but the caller did not cancel => timeout.
            throw new AiRequestException(
                $"Model '{model}' timed out after {seconds}s.", isTimeout: true);
        }
        catch (HttpRequestException ex)
        {
            throw new AiRequestException(
                $"Could not reach the AI endpoint for model '{model}'. {ex.Message}",
                isNetwork: true, inner: ex);
        }
    }

    /// <summary>Maps an ISO code to an English language name so the model gets a clear instruction.</summary>
    private static string DescribeLanguage(string code) => code?.ToLowerInvariant() switch
    {
        "en" => "English",
        "fa" => "Persian (Farsi)",
        "ar" => "Arabic",
        "fr" => "French",
        "de" => "German",
        "es" => "Spanish",
        "it" => "Italian",
        "ru" => "Russian",
        "tr" => "Turkish",
        "zh" => "Chinese",
        "ja" => "Japanese",
        "ko" => "Korean",
        "hi" => "Hindi",
        "pt" => "Portuguese",
        _ => string.IsNullOrWhiteSpace(code) ? "English" : code
    };

    /// <summary>
    /// Lists the model ids the endpoint exposes via the OpenAI-compatible
    /// <c>GET {base}/models</c> route. Returns an empty list when the endpoint
    /// does not implement it (some local servers don't) — callers should treat
    /// the field as free-text in that case.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListModelsAsync(
        CustomAiConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new InvalidOperationException("A base URL is required.");

        string endpoint = BuildBase(config.BaseUrl) + "/models";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        using HttpResponseMessage response =
            await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream stream =
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument doc =
            await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var models = new List<string>();

        // OpenAI shape: { "data": [ { "id": "..." }, ... ] }
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("data", out JsonElement data) &&
            data.ValueKind == JsonValueKind.Array)
        {
            CollectModelIds(data, models);
        }
        // Ollama / some local servers: { "models": [ { "name": "..." }, ... ] }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                 doc.RootElement.TryGetProperty("models", out JsonElement alt) &&
                 alt.ValueKind == JsonValueKind.Array)
        {
            CollectModelIds(alt, models);
        }
        // Bare array: [ { "id": "..." }, ... ] or [ "model-a", "model-b" ]
        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            CollectModelIds(doc.RootElement, models);
        }

        models.Sort(StringComparer.OrdinalIgnoreCase);
        return models;
    }

    /// <summary>
    /// Extracts model identifiers from a JSON array, tolerating the common
    /// OpenAI-compatible variants: objects keyed by <c>id</c>/<c>name</c>/<c>model</c>,
    /// or plain strings.
    /// </summary>
    private static void CollectModelIds(JsonElement array, List<string> into)
    {
        foreach (JsonElement item in array.EnumerateArray())
        {
            string? id = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object => ReadFirstString(item, "id", "name", "model"),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(id) &&
                !into.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                into.Add(id);
            }
        }
    }

    private static string? ReadFirstString(JsonElement obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (obj.TryGetProperty(key, out JsonElement value) &&
                value.ValueKind == JsonValueKind.String &&
                value.GetString() is { Length: > 0 } s)
            {
                return s;
            }
        }
        return null;
    }

    /// <summary>
    /// Probes the endpoint (a lightweight <c>GET /models</c>) and measures
    /// round-trip latency, so the UI can show a live connection indicator. Never
    /// throws and never surfaces the API key.
    /// </summary>
    public async Task<ProviderProbeResult> TestConnectionAsync(
        CustomAiConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            return new ProviderProbeResult(false, 0, "No base URL");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            string endpoint = BuildBase(config.BaseUrl) + "/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            using HttpResponseMessage response =
                await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
                return new ProviderProbeResult(true, stopwatch.ElapsedMilliseconds, "OK");

            // 404 on /models still means the host answered — many local servers
            // only implement /chat/completions. Report reachable-but-limited.
            if ((int)response.StatusCode == 404)
                return new ProviderProbeResult(true, stopwatch.ElapsedMilliseconds,
                    "Reachable (no model list)");

            // 401/403: the host is reachable but the API key is missing/wrong —
            // the single most common reason a provider "works elsewhere but not
            // here". Say so explicitly instead of a bare status number.
            if ((int)response.StatusCode is 401 or 403)
                return new ProviderProbeResult(false, stopwatch.ElapsedMilliseconds,
                    $"HTTP {(int)response.StatusCode} — check the API key");

            return new ProviderProbeResult(false, stopwatch.ElapsedMilliseconds,
                $"HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            return new ProviderProbeResult(false, stopwatch.ElapsedMilliseconds, "Cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ProviderProbeResult(false, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    /// <summary>
    /// Normalises a user-entered base URL into a chat-completions endpoint.
    /// Accepts either a bare base (".../v1") or a full endpoint URL.
    /// </summary>
    private static string BuildEndpoint(string baseUrl)
    {
        string trimmed = baseUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("completions", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed + "/chat/completions";
    }

    /// <summary>Returns the base URL trimmed of a trailing slash for GET routes.</summary>
    private static string BuildBase(string baseUrl) => baseUrl.Trim().TrimEnd('/');

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
