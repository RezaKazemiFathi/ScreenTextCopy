using System.IO;
using System.Net.Http;

namespace ScreenTextCopy.Services;

/// <summary>
/// A Tesseract language pack (traineddata) that can be shipped or downloaded.
/// </summary>
public sealed record LanguagePack(string Code, string EnglishName, string NativeName)
{
    public bool IsInstalled { get; set; }
}

/// <summary>
/// Lists installed OCR language packs and downloads missing ones on demand from
/// the official tesseract-ocr tessdata_fast repository over HTTPS.
/// </summary>
public sealed class LanguagePackService
{
    private const string BaseUrl =
        "https://github.com/tesseract-ocr/tessdata_fast/raw/main/";

    private readonly HttpClient _http;
    private readonly string _tessDataPath;

    public LanguagePackService(HttpClient http, TesseractOcrEngine engine)
    {
        _http = http;
        _tessDataPath = engine.TessDataPath;
    }

    /// <summary>Catalog of well-known languages users can enable.</summary>
    public static IReadOnlyList<LanguagePack> Catalog { get; } = new[]
    {
        new LanguagePack("eng", "English", "English"),
        new LanguagePack("fas", "Persian", "فارسی"),
        new LanguagePack("ara", "Arabic", "العربية"),
        new LanguagePack("fra", "French", "Français"),
        new LanguagePack("deu", "German", "Deutsch"),
        new LanguagePack("spa", "Spanish", "Español"),
        new LanguagePack("ita", "Italian", "Italiano"),
        new LanguagePack("rus", "Russian", "Русский"),
        new LanguagePack("tur", "Turkish", "Türkçe"),
        new LanguagePack("chi_sim", "Chinese (Simplified)", "简体中文"),
        new LanguagePack("jpn", "Japanese", "日本語"),
        new LanguagePack("kor", "Korean", "한국어"),
        new LanguagePack("hin", "Hindi", "हिन्दी"),
        new LanguagePack("por", "Portuguese", "Português")
    };

    public bool IsInstalled(string code) =>
        File.Exists(Path.Combine(_tessDataPath, code + ".traineddata"));

    /// <summary>Returns the catalog with the installed flag populated.</summary>
    public IReadOnlyList<LanguagePack> GetPacks()
    {
        foreach (LanguagePack pack in Catalog)
            pack.IsInstalled = IsInstalled(pack.Code);
        return Catalog;
    }

    /// <summary>
    /// Downloads the traineddata for <paramref name="code"/> into the tessdata
    /// folder. Reports progress as a fraction 0..1 when the size is known.
    /// </summary>
    public async Task DownloadAsync(
        string code,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Guard against path traversal: only allow known catalog codes.
        if (!Catalog.Any(p => string.Equals(p.Code, code, StringComparison.Ordinal)))
            throw new ArgumentException($"Unknown language code '{code}'.", nameof(code));

        Directory.CreateDirectory(_tessDataPath);
        string url = BaseUrl + code + ".traineddata";
        string targetPath = Path.Combine(_tessDataPath, code + ".traineddata");
        string tempPath = targetPath + ".download";

        using var response = await _http.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? total = response.Content.Headers.ContentLength;

        await using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                readTotal += read;
                if (total is > 0)
                    progress?.Report((double)readTotal / total.Value);
            }
        }

        var info = new FileInfo(tempPath);
        if (info.Length < 1024)
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Downloaded language pack looks invalid (too small).");
        }

        File.Move(tempPath, targetPath, overwrite: true);
        progress?.Report(1.0);
    }
}
