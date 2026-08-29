using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using QRCoder;

namespace ScreenTextCopy.Services;

/// <summary>
/// Renders text into a QR code image for "send to mobile" (scan with the phone
/// camera). Fully local: no network or account involved.
/// </summary>
public sealed class QrCodeService
{
    /// <summary>
    /// Approximate safe capacity for a QR code at ECC level Q with UTF-8 byte
    /// data. Beyond this, the module count grows so dense that phone cameras
    /// struggle, so we surface a warning.
    /// </summary>
    public const int RecommendedMaxChars = 900;

    public bool ExceedsRecommended(string text) =>
        !string.IsNullOrEmpty(text) && Encoding.UTF8.GetByteCount(text) > RecommendedMaxChars;

    /// <summary>
    /// Generates a QR bitmap for the given text. Uses ECC level Q (25% recovery)
    /// and forces UTF-8 so Persian/emoji content survives, with a quiet zone so
    /// phone cameras lock on reliably.
    /// </summary>
    public BitmapSource Generate(string text, int pixelsPerModule = 10)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Cannot create a QR code from empty text.", nameof(text));

        using var generator = new QRCodeGenerator();
        // forceUtf8: true + ECC Q makes non-Latin text and denser payloads scan
        // far more reliably than the previous ECC M / no-UTF-8 defaults.
        using QRCodeData data = generator.CreateQrCode(
            text, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);
        using var png = new PngByteQRCode(data);
        // drawQuietZones: true adds the mandatory white margin scanners need.
        byte[] bytes = png.GetGraphic(pixelsPerModule, drawQuietZones: true);

        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
