using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.ViewModels;

/// <summary>
/// Reusable, self-contained editor for a single rebindable global hotkey.
///
/// Holds the modifier flags + non-modifier key, exposes a live human-readable
/// preview, and validates captured chords. Multiple instances let the settings
/// window edit several distinct shortcuts (e.g. capture vs. overlay) with the
/// exact same logic instead of duplicating it.
/// </summary>
public sealed partial class HotkeyEditor : ObservableObject
{
    public HotkeyEditor(HotkeyConfig source)
    {
        _control = source.Control;
        _shift = source.Shift;
        _alt = source.Alt;
        _win = source.Win;
        _virtualKey = source.VirtualKey;
        _keyLabel = source.KeyLabel;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Display))]
    private bool _control;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Display))]
    private bool _shift;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Display))]
    private bool _alt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Display))]
    private bool _win;

    [ObservableProperty] private uint _virtualKey;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Display))]
    private string _keyLabel;

    [ObservableProperty] private bool _isCapturing;

    /// <summary>Human-readable preview of the current chord, e.g. "Ctrl + Shift + X".</summary>
    public string Display
    {
        get
        {
            var parts = new List<string>(4);
            if (Control) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Win) parts.Add("Win");
            parts.Add(string.IsNullOrWhiteSpace(KeyLabel) ? "?" : KeyLabel);
            return string.Join(" + ", parts);
        }
    }

    [RelayCommand]
    private void StartCapture() => IsCapturing = true;

    /// <summary>
    /// Applies a captured chord. Requires a real (non-modifier) key so the
    /// result stays registerable; returns false and leaves state unchanged
    /// otherwise.
    /// </summary>
    public bool TrySet(bool control, bool shift, bool alt, bool win, uint virtualKey, string keyLabel)
    {
        if (virtualKey == 0 || string.IsNullOrWhiteSpace(keyLabel))
            return false;

        Control = control;
        Shift = shift;
        Alt = alt;
        Win = win;
        VirtualKey = virtualKey;
        KeyLabel = keyLabel;
        return true;
    }

    /// <summary>Writes the current chord into an existing <see cref="HotkeyConfig"/>.</summary>
    public void ApplyTo(HotkeyConfig target)
    {
        target.Control = Control;
        target.Shift = Shift;
        target.Alt = Alt;
        target.Win = Win;
        target.VirtualKey = VirtualKey;
        target.KeyLabel = KeyLabel;
    }
}
