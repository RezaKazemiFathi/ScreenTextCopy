using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// Abstraction over an OCR backend. Keeping the UI dependent only on this
/// interface lets us swap Tesseract for Windows.Media.Ocr, PaddleOCR, etc.
/// </summary>
public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(
        string imagePath,
        IReadOnlyList<string> languages,
        CancellationToken cancellationToken = default);
}
