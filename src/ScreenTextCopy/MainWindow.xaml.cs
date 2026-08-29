using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using ScreenTextCopy.Services;
using ScreenTextCopy.ViewModels;

namespace ScreenTextCopy;

/// <summary>
/// The main application window. Behavior lives in
/// <see cref="ViewModels.MainViewModel"/>; this class handles view-level
/// concerns: the global hotkey, the system-tray icon, and close-to-tray.
/// </summary>
public partial class MainWindow : Window
{
    private GlobalHotkeyService? _hotkey;
    private GlobalHotkeyService? _overlayHotkey;
    private TaskbarIcon? _tray;
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closing += OnClosing;
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (Vm is null)
            return;

        // Global hotkey -> capture.
        _hotkey = new GlobalHotkeyService(this);
        _hotkey.Attach();
        _hotkey.Pressed += (_, _) => TriggerCapture();
        _hotkey.Register(Vm.Services.Settings.Current.Hotkey);

        // Second global hotkey -> in-place translation overlay (game/movie mode).
        // Uses a distinct id so it never collides with the capture hotkey.
        _overlayHotkey = new GlobalHotkeyService(this, id: 9002);
        _overlayHotkey.Attach();
        _overlayHotkey.Pressed += (_, _) => TriggerOverlay();
        _overlayHotkey.Register(Vm.Services.Settings.Current.OverlayHotkey);

        // Re-register whenever the user rebinds the shortcut in Settings.
        Vm.HotkeyChanged += (_, _) =>
        {
            _hotkey?.Register(Vm.Services.Settings.Current.Hotkey);
            _overlayHotkey?.Register(Vm.Services.Settings.Current.OverlayHotkey);
        };

        BuildTray();
    }

    private void TriggerCapture()
    {
        if (Vm?.CaptureCommand.CanExecute(null) == true)
        {
            // Surface the window first. The global hotkey keeps firing while the
            // app is hidden in the tray (the HWND still exists), but Hide() leaves
            // IsVisible false, so a plain Activate() cannot bring it forward. Calling
            // Show() re-displays a tray-hidden window; without it, pressing the
            // shortcut from a "closed" (tray) state would do nothing visible.
            if (!IsVisible)
                Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
            Vm.CaptureCommand.Execute(null);
        }
    }

    /// <summary>
    /// Fires the in-place translation overlay without surfacing the main window,
    /// so it works over a full-screen game or video.
    /// </summary>
    private void TriggerOverlay()
    {
        if (Vm?.CaptureOverlayCommand.CanExecute(null) == true)
            Vm.CaptureOverlayCommand.Execute(null);
    }

    private void BuildTray()
    {
        LocalizationService loc = Vm!.Services.Localization;
        _tray = new TaskbarIcon
        {
            ToolTipText = "ScreenTextCopy",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location)
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = loc.Get("tray.show") };
        showItem.Click += (_, _) => ShowFromTray();

        var captureItem = new System.Windows.Controls.MenuItem { Header = loc.Get("tray.capture") };
        captureItem.Click += (_, _) => TriggerCapture();

        var exitItem = new System.Windows.Controls.MenuItem { Header = loc.Get("tray.exit") };
        exitItem.Click += (_, _) => { _reallyExit = true; System.Windows.Application.Current.Shutdown(); };

        menu.Items.Add(showItem);
        menu.Items.Add(captureItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        _tray.ContextMenu = menu;
        _tray.TrayMouseDoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Close-to-tray: keep running in the background unless the user chose exit.
        if (!_reallyExit && Vm?.Services.Settings.Current.MinimizeToTray == true)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _hotkey?.Dispose();
        _overlayHotkey?.Dispose();
        _tray?.Dispose();
    }
}
