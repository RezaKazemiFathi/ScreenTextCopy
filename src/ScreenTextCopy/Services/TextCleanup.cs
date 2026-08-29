using System.Text;
using System.Text.RegularExpressions;

namespace ScreenTextCopy.Services;

/// <summary>
/// Conservative post-OCR text cleanup.
///
/// Deliberately does NOT reverse RTL text or reorder words: the OCR output is
/// already in logical order, and blind reversal corrupts mixed content such as
/// error codes, URLs, file paths and numbers. We only fix a handful of common
/// OCR spacing artefacts and normalise whitespace.
///
/// Emoji note: Tesseract has no training data for emoji or pictographs, so it
/// cannot recognise them. It typically emits the Unicode replacement character
/// (U+FFFD '�') where an emoji sat. We strip that OCR noise here (it is never a
/// real character the user wanted) but leave any genuine emoji that survive a
/// paste untouched — we never rewrite or reorder the real content.
/// </summary>
public static partial class TextCleanup
{
    /// <summary>Unicode replacement character emitted by OCR for glyphs it cannot read.</summary>
    private const char ReplacementChar = '�';

    /// <summary>
    /// True when the raw OCR output contained replacement characters, i.e. the
    /// engine hit glyphs (commonly emoji/pictographs) it could not recognise.
    /// Lets the UI tell the user honestly instead of silently dropping them.
    /// </summary>
    public static bool HasUnrecognizableGlyphs(string? rawText) =>
        !string.IsNullOrEmpty(rawText) && rawText.IndexOf(ReplacementChar) >= 0;

    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // Drop OCR "unrecognised glyph" markers (emoji/pictographs Tesseract
        // cannot read). Collapse any spaces they leave behind afterwards.
        if (text.IndexOf(ReplacementChar) >= 0)
            text = text.Replace("�", string.Empty);

        // "https: //example" -> "https://example"
        text = SchemeSlashRegex().Replace(text, "$1://");

        // "0x  80070005" / "0 x80070005" -> "0x80070005"
        text = HexPrefixRegex().Replace(text, "0x");

        // Collapse runs of spaces/tabs (but keep newlines).
        text = HorizontalSpaceRegex().Replace(text, " ");

        // Trim trailing spaces on each line and drop excess blank lines.
        var builder = new StringBuilder(text.Length);
        int consecutiveBlank = 0;
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                consecutiveBlank++;
                if (consecutiveBlank > 1)
                    continue;
            }
            else
            {
                consecutiveBlank = 0;
            }

            builder.Append(line).Append('\n');
        }

        return builder.ToString().Trim();
    }

    [GeneratedRegex(@"\b(https?|ftp)\s*:\s*/\s*/", RegexOptions.IgnoreCase)]
    private static partial Regex SchemeSlashRegex();

    [GeneratedRegex(@"\b0\s*x\s*(?=[0-9a-fA-F])", RegexOptions.IgnoreCase)]
    private static partial Regex HexPrefixRegex();

    [GeneratedRegex(@"[^\S\n]{2,}")]
    private static partial Regex HorizontalSpaceRegex();
}
