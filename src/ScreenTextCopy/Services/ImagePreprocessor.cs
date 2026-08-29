using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenTextCopy.Services;

/// <summary>
/// Optional image preprocessing to improve OCR accuracy on small / low-contrast
/// text. Pipeline: aggressive high-quality upscale for small captures ->
/// grayscale (luminance) -> Otsu binarization with automatic polarity so both
/// dark-on-light and light-on-dark text become black text on a white page,
/// which is what Tesseract's LSTM engine expects.
/// </summary>
public static class ImagePreprocessor
{
    // Screen text is usually captured at ~12-16px cap height. Tesseract wants
    // ~30-33px, so we upscale small selections substantially. A generous cap lets
    // tiny single-line captures grow enough that Tesseract can resolve the small
    // inter-word gaps in connected Arabic-script text (Persian), which is what
    // stops adjacent words being merged (e.g. "انتخاب می‌کنم" -> "انتخابمی‌کنم").
    private const int MinTargetHeight = 1100;
    private const float MaxScale = 6f;

    // Tesseract's LSTM engine recognises text noticeably better when there is a
    // quiet margin around the content (it uses the border for line/segment
    // detection). Screen selections are usually cropped tight to the glyphs, so
    // we re-add a white quiet-zone after binarisation. This is one of the biggest
    // accuracy wins for tight, mixed Persian+English captures.
    private const int QuietZone = 24;

    /// <summary>
    /// Preprocesses the PNG at <paramref name="sourcePath"/> and writes a new
    /// temporary PNG, returning its path. On any failure the original path is
    /// returned unchanged.
    /// </summary>
    public static string Enhance(string sourcePath)
    {
        try
        {
            using var original = new Bitmap(sourcePath);

            // Scale based on the smaller dimension so tiny/thin selections still
            // get enough resolution.
            int minDim = Math.Min(original.Width, original.Height);
            float scale = 1f;
            if (minDim > 0 && original.Height < MinTargetHeight)
                scale = Math.Min(MaxScale, (float)MinTargetHeight / original.Height);
            if (scale < 1f)
                scale = 1f;

            int width = Math.Max(1, (int)(original.Width * scale));
            int height = Math.Max(1, (int)(original.Height * scale));

            using var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, 0, 0, width, height);
            }

            using var processed = Binarize(scaled);
            using var padded = AddQuietZone(processed, QuietZone);

            string outPath = Path.Combine(
                Path.GetTempPath(),
                "stc_pre_" + Guid.NewGuid().ToString("N") + ".png");
            padded.Save(outPath, ImageFormat.Png);
            return outPath;
        }
        catch
        {
            return sourcePath;
        }
    }

    /// <summary>
    /// Converts to grayscale, computes an Otsu threshold, and produces a clean
    /// black-on-white binary image regardless of the original text polarity.
    /// </summary>
    private static Bitmap Binarize(Bitmap source)
    {
        int width = source.Width;
        int height = source.Height;
        var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        var rect = new Rectangle(0, 0, width, height);
        BitmapData srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData dstData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            int srcStride = srcData.Stride;
            int dstStride = dstData.Stride;

            // Pass 1: build a grayscale histogram.
            var histogram = new int[256];
            var gray = new byte[width * height];

            unsafe
            {
                byte* srcBase = (byte*)srcData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcBase + y * srcStride;
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        byte b = srcRow[x * 4 + 0];
                        byte gg = srcRow[x * 4 + 1];
                        byte r = srcRow[x * 4 + 2];
                        byte lum = (byte)(0.299 * r + 0.587 * gg + 0.114 * b);
                        gray[rowOffset + x] = lum;
                        histogram[lum]++;
                    }
                }
            }

            int threshold = OtsuThreshold(histogram, width * height);

            // Decide polarity: if the majority of pixels are dark, the text is
            // probably light-on-dark, so invert to get black-on-white.
            long darkCount = 0;
            for (int i = 0; i < threshold; i++)
                darkCount += histogram[i];
            bool invert = darkCount > (long)width * height / 2;

            // Pass 2: threshold to pure black/white.
            unsafe
            {
                byte* dstBase = (byte*)dstData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* dstRow = dstBase + y * dstStride;
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        bool isForeground = gray[rowOffset + x] <= threshold;
                        if (invert)
                            isForeground = !isForeground;

                        // Foreground (text) => black, background => white.
                        byte v = isForeground ? (byte)0 : (byte)255;
                        dstRow[x * 3 + 0] = v;
                        dstRow[x * 3 + 1] = v;
                        dstRow[x * 3 + 2] = v;
                    }
                }
            }
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(dstData);
        }

        return result;
    }

    /// <summary>
    /// Surrounds a (black-on-white) binary image with a white margin. Tesseract
    /// treats this quiet zone as page background and segments lines/words more
    /// reliably, which improves accuracy on tightly-cropped, mixed-script text.
    /// </summary>
    private static Bitmap AddQuietZone(Bitmap source, int margin)
    {
        var padded = new Bitmap(
            source.Width + margin * 2,
            source.Height + margin * 2,
            PixelFormat.Format24bppRgb);

        using (var g = Graphics.FromImage(padded))
        {
            g.Clear(Color.White);
            g.DrawImageUnscaled(source, margin, margin);
        }

        return padded;
    }

    /// <summary>Classic Otsu between-class variance maximisation.</summary>
    private static int OtsuThreshold(int[] histogram, int total)
    {
        double sum = 0;
        for (int i = 0; i < 256; i++)
            sum += i * (double)histogram[i];

        double sumB = 0;
        int wB = 0;
        double maxVariance = 0;
        int threshold = 127;

        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0)
                continue;
            int wF = total - wB;
            if (wF == 0)
                break;

            sumB += t * (double)histogram[t];
            double mB = sumB / wB;
            double mF = (sum - sumB) / wF;
            double between = (double)wB * wF * (mB - mF) * (mB - mF);
            if (between > maxVariance)
            {
                maxVariance = between;
                threshold = t;
            }
        }

        return threshold;
    }
}
