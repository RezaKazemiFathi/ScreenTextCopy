using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// OCR engine backed by the bundled Tesseract 5 command-line executable.
/// Produces both plain text and a TSV file so we can compute a mean confidence.
/// </summary>
public sealed partial class TesseractOcrEngine : IOcrEngine
{
    private readonly string _exePath;
    private readonly string _tessDataPath;

    public TesseractOcrEngine()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _exePath = Path.Combine(baseDir, "Tesseract", "tesseract.exe");
        _tessDataPath = Path.Combine(baseDir, "Tesseract", "tessdata");
    }

    public string TessDataPath => _tessDataPath;

    public async Task<OcrResult> RecognizeAsync(
        string imagePath,
        IReadOnlyList<string> languages,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_exePath))
            throw new FileNotFoundException("Bundled Tesseract executable not found.", _exePath);
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("OCR input image not found.", imagePath);

        string langArg = BuildLanguageArgument(languages);
        string outBase = Path.Combine(Path.GetTempPath(), "stc_" + Guid.NewGuid().ToString("N"));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _exePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // ArgumentList escapes each value, preventing command injection.
            psi.ArgumentList.Add(imagePath);
            psi.ArgumentList.Add(outBase);
            psi.ArgumentList.Add("-l");
            psi.ArgumentList.Add(langArg);
            psi.ArgumentList.Add("--tessdata-dir");
            psi.ArgumentList.Add(_tessDataPath);
            // OEM 1 = LSTM only. The LSTM engine handles small text and mixed
            // scripts far better than the legacy engine and produces logically
            // ordered output for bidirectional (RTL/LTR) text.
            psi.ArgumentList.Add("--oem");
            psi.ArgumentList.Add("1");
            // PSM 6 = assume a single uniform block of text. Good default for a
            // rectangular screen selection.
            psi.ArgumentList.Add("--psm");
            psi.ArgumentList.Add("6");
            // Keep multiple spaces so column-aligned / mixed content stays
            // readable instead of being collapsed.
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("preserve_interword_spaces=1");
            psi.ArgumentList.Add("txt");
            psi.ArgumentList.Add("tsv");

            using var process = new Process { StartInfo = psi };
            process.Start();

            Task<string> stdErr = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                string error = await stdErr.ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"Tesseract failed (exit {process.ExitCode}). {error}".Trim());
            }

            string text = await ReadIfExistsAsync(outBase + ".txt", cancellationToken).ConfigureAwait(false);
            double? confidence = TryReadConfidence(outBase + ".tsv");

            return new OcrResult
            {
                Text = TextCleanup.Clean(text),
                MeanConfidence = confidence,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                Languages = languages.ToArray(),
                HasUnrecognizableGlyphs = TextCleanup.HasUnrecognizableGlyphs(text)
            };
        }
        finally
        {
            TryDelete(outBase + ".txt");
            TryDelete(outBase + ".tsv");
        }
    }

    private string BuildLanguageArgument(IReadOnlyList<string> languages)
    {
        var valid = languages
            .Where(l => !string.IsNullOrWhiteSpace(l) && LanguageCodeRegex().IsMatch(l))
            .Where(l => File.Exists(Path.Combine(_tessDataPath, l + ".traineddata")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Fall back to English if nothing valid/installed was supplied.
        if (valid.Count == 0)
            valid.Add("eng");

        // Order matters: Tesseract gives the FIRST language priority when several
        // are loaded. The Latin model is greedy and will happily claim short,
        // ambiguous Persian/Arabic words (e.g. "تو" -> "gö", "کنید" -> "AS") when
        // English is listed first. Complex right-to-left scripts must therefore be
        // recognised first, with Latin kept as the trailing fallback so embedded
        // English (like a stray "A") is still picked up. This is the single biggest
        // accuracy win for mixed Persian+English captures. A stable partition keeps
        // the user's own ordering within each group.
        var ordered = valid
            .Where(l => !IsLatinScriptLanguage(l))
            .Concat(valid.Where(IsLatinScriptLanguage));

        return string.Join('+', ordered);
    }

    /// <summary>
    /// True for languages written in the Latin script. These are deprioritised in
    /// the recognition order so a Latin-first bias cannot corrupt right-to-left or
    /// other complex-script text (see <see cref="BuildLanguageArgument"/>).
    /// </summary>
    private static bool IsLatinScriptLanguage(string code) =>
        LatinScriptLanguages.Contains(code);

    private static readonly HashSet<string> LatinScriptLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "eng", "afr", "aze", "cat", "ces", "cym", "dan", "deu", "epo", "est",
        "eus", "fin", "fra", "gle", "glg", "hrv", "hun", "ind", "isl", "ita",
        "jav", "lat", "lav", "lit", "msa", "mlt", "nld", "nor", "pol", "por",
        "ron", "slk", "slv", "spa", "sqi", "swa", "swe", "tur", "vie", "cos",
        "fil", "gla", "haw", "kmr", "ltz", "mri", "oci", "que", "srp_latn",
        "tgl", "uzb_latn", "yor", "zul", "som", "hat",
    };


    private static async Task<string> ReadIfExistsAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return string.Empty;
        return await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    private static double? TryReadConfidence(string tsvPath)
    {
        try
        {
            if (!File.Exists(tsvPath))
                return null;

            double sum = 0;
            int count = 0;
            foreach (string line in File.ReadLines(tsvPath))
            {
                string[] cols = line.Split('\t');
                if (cols.Length < 12)
                    continue;
                // Column 10 = conf, column 11 = word text.
                if (!double.TryParse(cols[10], NumberStyles.Float, CultureInfo.InvariantCulture, out double conf))
                    continue;
                if (conf < 0 || string.IsNullOrWhiteSpace(cols[11]))
                    continue;

                sum += conf;
                count++;
            }

            return count == 0 ? null : Math.Round(sum / count, 1);
        }
        catch
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
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

    [GeneratedRegex("^[a-z]{2,10}(_[a-z0-9]+)*$", RegexOptions.IgnoreCase)]
    private static partial Regex LanguageCodeRegex();
}
