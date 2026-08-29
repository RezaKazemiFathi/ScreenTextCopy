using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ScreenTextCopy.Views;

/// <summary>
/// A full-virtual-desktop transparent overlay used to select a rectangular
/// region.
///
/// COORDINATE SYSTEMS (read before editing):
///  * The overlay window is positioned/sized in WPF logical units relative to
///    the primary monitor (SystemParameters.VirtualScreen*). The crosshair and
///    selection rectangle are drawn in those same logical units, so the visual
///    feedback is always consistent with the mouse.
///  * The rectangle that is actually CAPTURED is built from the Win32 cursor
///    position (GetCursorPos), which returns *virtual-desktop physical pixels*.
///    This is exactly the coordinate space Graphics.CopyFromScreen expects, so
///    the captured pixels match the selected region on every monitor regardless
///    of per-monitor DPI. We never feed WPF logical units into the capture.
/// </summary>
public partial class SelectionOverlay : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private Point _startWpf;
    private POINT _startDevice;
    private bool _dragging;
    private Int32Rect? _result;

    public SelectionOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Shows the overlay modally and returns the selected region in virtual
    /// desktop physical pixels, or null if the user cancelled.
    /// </summary>
    public static Int32Rect? PickRegion()
    {
        var overlay = new SelectionOverlay();
        overlay.ShowDialog();
        return overlay._result;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Cover the entire virtual desktop.
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        // Size the crosshair guides to the whole canvas.
        CrossV.Y2 = Height;
        CrossH.X2 = Width;

        Activate();
        Focus();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _startWpf = e.GetPosition(OverlayCanvas);
        GetCursorPos(out _startDevice);
        _dragging = true;
        CaptureMouse();

        System.Windows.Controls.Canvas.SetLeft(SelectionBox, _startWpf.X);
        System.Windows.Controls.Canvas.SetTop(SelectionBox, _startWpf.Y);
        SelectionBox.Width = 0;
        SelectionBox.Height = 0;
        SelectionBox.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point current = e.GetPosition(OverlayCanvas);

        // Move the crosshair guides to the cursor at all times.
        CrossV.X1 = current.X;
        CrossV.X2 = current.X;
        CrossH.Y1 = current.Y;
        CrossH.Y2 = current.Y;

        if (!_dragging)
            return;

        // Normalized rectangle so all four drag directions work.
        double x = Math.Min(current.X, _startWpf.X);
        double y = Math.Min(current.Y, _startWpf.Y);
        double w = Math.Abs(current.X - _startWpf.X);
        double h = Math.Abs(current.Y - _startWpf.Y);

        System.Windows.Controls.Canvas.SetLeft(SelectionBox, x);
        System.Windows.Controls.Canvas.SetTop(SelectionBox, y);
        SelectionBox.Width = w;
        SelectionBox.Height = h;

        // The badge shows the ACTUAL physical capture size (device pixels).
        GetCursorPos(out POINT dev);
        int pxW = Math.Abs(dev.X - _startDevice.X);
        int pxH = Math.Abs(dev.Y - _startDevice.Y);
        SizeText.Text = $"{pxW} × {pxH} px";

        double badgeX = x;
        double badgeY = y - 28;
        if (badgeY < 0)
            badgeY = y + h + 6;
        System.Windows.Controls.Canvas.SetLeft(SizeBadge, badgeX);
        System.Windows.Controls.Canvas.SetTop(SizeBadge, Math.Max(0, badgeY));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_dragging)
            return;

        _dragging = false;
        ReleaseMouseCapture();

        GetCursorPos(out POINT endDevice);
        int left = Math.Min(_startDevice.X, endDevice.X);
        int top = Math.Min(_startDevice.Y, endDevice.Y);
        int width = Math.Abs(endDevice.X - _startDevice.X);
        int height = Math.Abs(endDevice.Y - _startDevice.Y);

        // Accept even fairly small selections; only reject essentially-zero drags.
        if (width >= 2 && height >= 2)
            _result = new Int32Rect(left, top, width, height);

        Close();
    }

    // Right-click cancels the selection.
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        Cancel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
            Cancel();
    }

    private void Cancel()
    {
        _result = null;
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();
        }
        Close();
    }
}
