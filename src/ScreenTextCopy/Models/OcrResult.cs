namespace ScreenTextCopy.Models;

/// <summary>
/// Result of an OCR recognition pass.
/// </summary>
public sealed record OcrResult
{
    public string Text { get; init; } = string.Empty;

    /// <summary>Mean word confidence in the range 0..100, or null if unknown.</summary>
    public double? MeanConfidence { get; init; }

    /// <summary>Wall-clock time the recognition took.</summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>Language codes actually used for recognition (e.g. "fas", "eng").</summary>
    public IReadOnlyList<string> Languages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// True when the source contained glyphs the OCR engine could not recognise
    /// (typically emoji / pictographs). Tesseract cannot read these, so they are
    /// dropped from <see cref="Text"/>; this flag lets the UI say so honestly.
    /// </summary>
    public bool HasUnrecognizableGlyphs { get; init; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}
