using System.Windows;
using System.Windows.Media.Imaging;
using ScreenTextCopy.Services;

namespace ScreenTextCopy.Views;

/// <summary>
/// Displays a QR code so the user can scan the recognized text with a phone.
/// Fully local; nothing is transmitted.
/// </summary>
public partial class QrWindow : Window
{
    public QrWindow(BitmapSource qr, LocalizationService loc, bool tooLong)
    {
        InitializeComponent();
        FlowDirection = loc.FlowDirection;
        QrImage.Source = qr;
        WarnText.Visibility = tooLong ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
