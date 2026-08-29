using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenTextCopy.Services;

/// <summary>
/// Captures a rectangular region of the virtual desktop (in real device pixels)
/// to a temporary PNG file for OCR.
/// </summary>
public sealed class ScreenCaptureService
{
    /// <summary>
    /// Captures the given pixel rectangle and returns the path to a temporary PNG.
    /// Coordinates are real screen pixels; order of corners does not matter.
    /// </summary>
    public string CaptureToFile(int x1, int y1, int x2, int y2)
    {
        int left = Math.Min(x1, x2);
        int top = Math.Min(y1, y2);
        int width = Math.Abs(x2 - x1);
        int height = Math.Abs(y2 - y1);

        if (width <= 0 || height <= 0)
            throw new ArgumentException("The selected region is not valid.");

        string filePath = Path.Combine(
            Path.GetTempPath(),
            "stc_cap_" + Guid.NewGuid().ToString("N") + ".png");

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                left, top, 0, 0,
                new Size(width, height),
                CopyPixelOperation.SourceCopy);
        }

        bitmap.Save(filePath, ImageFormat.Png);
        return filePath;
    }
}
