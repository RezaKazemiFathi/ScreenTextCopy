using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenTextCopy.ViewModels;

namespace ScreenTextCopy.Views;

/// <summary>
/// Settings dialog. Changes to theme and language apply live; the full settings
/// object is only persisted when the user clicks Save.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.Persist();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Commits a model picked from the dropdown straight into the view model.
    /// An editable ComboBox whose Text is bound with UpdateSourceTrigger=PropertyChanged
    /// does not reliably push the source when the value comes from selection
    /// (only when typed), so the chosen model would silently revert. Writing it
    /// explicitly makes selection stick.
    /// </summary>
    private void OnModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
            return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string model &&
            !string.IsNullOrWhiteSpace(model))
        {
            vm.AiModel = model;
        }
    }

    /// <summary>
    /// Opens a creator social link in the user's default browser. The URL comes
    /// from the button's Tag; we only launch validated absolute http(s) URLs so
    /// a stray Tag can never be used to start an arbitrary process.
    /// </summary>
    private void OnOpenLink(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url })
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // Best effort: no browser available or launch blocked.
        }
    }

    // --- Global-hotkey rebinding ---------------------------------------------

    /// <summary>
    /// Resolves which hotkey editor a given rebind button drives, using its Tag
    /// ("capture" or "overlay").
    /// </summary>
    private HotkeyEditor? EditorFor(object sender)
    {
        if (DataContext is not SettingsViewModel vm)
            return null;
        return sender is FrameworkElement { Tag: "overlay" }
            ? vm.OverlayHotkeyEditor
            : vm.HotkeyEditor;
    }

    /// <summary>
    /// Enters "capture" mode: the next key chord pressed while the button is
    /// focused becomes the new shortcut for that editor.
    /// </summary>
    private void OnHotkeyCaptureClick(object sender, RoutedEventArgs e)
        => EditorFor(sender)?.StartCaptureCommand.Execute(null);

    private void OnHotkeyCaptureLostFocus(object sender, RoutedEventArgs e)
    {
        HotkeyEditor? editor = EditorFor(sender);
        if (editor is not null)
            editor.IsCapturing = false;
    }

    /// <summary>
    /// While capturing, reads the pressed chord. We require at least one modifier
    /// plus a real (non-modifier) key so the result is a valid global hotkey, and
    /// map it to a Win32 virtual-key for registration.
    /// </summary>
    private void OnHotkeyCaptureKeyDown(object sender, KeyEventArgs e)
    {
        HotkeyEditor? editor = EditorFor(sender);
        if (editor is null || !editor.IsCapturing)
            return;

        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore lone modifier presses; wait for the actual key.
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System)
        {
            return;
        }

        if (key == Key.Escape)
        {
            editor.IsCapturing = false;
            return;
        }

        ModifierKeys mods = Keyboard.Modifiers;
        bool control = mods.HasFlag(ModifierKeys.Control);
        bool shift = mods.HasFlag(ModifierKeys.Shift);
        bool alt = mods.HasFlag(ModifierKeys.Alt);
        bool win = mods.HasFlag(ModifierKeys.Windows);

        // Require at least one modifier so the shortcut does not clash with plain typing.
        if (!(control || shift || alt || win))
            return;

        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk <= 0)
            return;

        string label = DescribeKey(key);
        if (editor.TrySet(control, shift, alt, win, (uint)vk, label))
            editor.IsCapturing = false;
    }

    /// <summary>Produces a short, human-readable label for a key.</summary>
    private static string DescribeKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => "Num" + (key - Key.NumPad0),
        >= Key.F1 and <= Key.F24 => "F" + (key - Key.F1 + 1),
        Key.Space => "Space",
        Key.Oem3 => "`",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion => "/",
        Key.OemSemicolon => ";",
        Key.OemOpenBrackets => "[",
        Key.Oem6 => "]",
        Key.Oem5 => "\\",
        Key.OemQuotes => "'",
        _ => key.ToString()
    };
}
