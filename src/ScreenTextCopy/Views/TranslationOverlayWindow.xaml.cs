using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScreenTextCopy.ViewModels;

namespace ScreenTextCopy.Views;

/// <summary>
/// A frameless, always-on-top popup that shows a translation in place, pinned
/// near the region the user selected — the "game / movie" mode. It does not
/// steal focus from the underlying app, can be dragged by its header, and
/// closes on Esc or the ✕ button.
///
/// Positioning takes a rectangle in virtual-desktop PHYSICAL pixels (the same
/// space <see cref="SelectionOverlay"/> returns) and converts it to WPF logical
/// units for this window using the window's own DPI.
/// </summary>
public partial class TranslationOverlayWindow : Window
{
    private readonly TranslationOverlayViewModel _vm;

    public TranslationOverlayWindow(TranslationOverlayViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    /// <summary>
    /// Places the popup just below the captured region (or above it when there
    /// is no room), clamped to the working area. <paramref name="regionPx"/> is
    /// in virtual-desktop physical pixels.
    /// </summary>
    public void PositionNear(Int32Rect regionPx)
    {
        // Convert physical pixels -> this window's logical units via its DPI.
        double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        double dpiScaleY = VisualTreeHelper.GetDpi(this).DpiScaleY;
        if (dpiScale <= 0) dpiScale = 1;
        if (dpiScaleY <= 0) dpiScaleY = 1;

        double regionLeft = regionPx.X / dpiScale;
        double regionTop = regionPx.Y / dpiScaleY;
        double regionWidth = regionPx.Width / dpiScale;
        double regionHeight = regionPx.Height / dpiScaleY;

        // Center horizontally on the region; sit just beneath it.
        double desiredLeft = regionLeft + (regionWidth - Width) / 2;
        double desiredTop = regionTop + regionHeight + 8;

        double vsLeft = SystemParameters.VirtualScreenLeft;
        double vsTop = SystemParameters.VirtualScreenTop;
        double vsRight = vsLeft + SystemParameters.VirtualScreenWidth;
        double vsBottom = vsTop + SystemParameters.VirtualScreenHeight;

        // If the popup would fall off the bottom, place it above the region.
        double estimatedHeight = ActualHeight > 0 ? ActualHeight : MinHeight;
        if (desiredTop + estimatedHeight > vsBottom)
            desiredTop = regionTop - estimatedHeight - 8;

        // Clamp within the virtual desktop.
        desiredLeft = Math.Max(vsLeft, Math.Min(desiredLeft, vsRight - Width));
        desiredTop = Math.Max(vsTop, Math.Min(desiredTop, vsBottom - estimatedHeight));

        Left = desiredLeft;
        Top = desiredTop;
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        string text = _vm.TranslatedText;
        if (string.IsNullOrEmpty(text))
            return;
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // best effort
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
            Close();
    }
}
