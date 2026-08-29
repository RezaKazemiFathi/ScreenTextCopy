using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// Registers a configurable system-wide hotkey using the Win32 RegisterHotKey
/// API and raises <see cref="Pressed"/> when it fires.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [Flags]
    private enum Modifiers : uint
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Window _window;
    private readonly int _id;
    private HwndSource? _source;
    private bool _registered;

    public event EventHandler? Pressed;

    public GlobalHotkeyService(Window window, int id = 9001)
    {
        _window = window;
        _id = id;
    }

    /// <summary>
    /// Attaches the message hook. Must be called after the window handle exists
    /// (e.g. from SourceInitialized).
    /// </summary>
    public void Attach()
    {
        var helper = new WindowInteropHelper(_window);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);
    }

    /// <summary>
    /// Registers the hotkey. Returns false if the combination is already taken.
    /// </summary>
    public bool Register(HotkeyConfig config)
    {
        Unregister();
        if (_source is null)
            return false;

        uint mods = (uint)Modifiers.NoRepeat;
        if (config.Control) mods |= (uint)Modifiers.Control;
        if (config.Shift) mods |= (uint)Modifiers.Shift;
        if (config.Alt) mods |= (uint)Modifiers.Alt;
        if (config.Win) mods |= (uint)Modifiers.Win;

        _registered = RegisterHotKey(_source.Handle, _id, mods, config.VirtualKey);
        return _registered;
    }

    public void Unregister()
    {
        // Always ask Windows to release the id, even if we believe it is not
        // registered. This is defensive: if a previous RegisterHotKey succeeded
        // at the OS level while our flag drifted out of sync, a conditional
        // unregister would leave the OLD chord live and the new RegisterHotKey
        // would fail with ERROR_HOTKEY_ALREADY_REGISTERED — exactly the "the old
        // shortcut still works after rebinding" symptom. The extra call is a
        // harmless no-op when nothing is registered.
        if (_source is not null)
        {
            UnregisterHotKey(_source.Handle, _id);
            _registered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }
}
