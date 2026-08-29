using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ScreenTextCopy.Services;

namespace ScreenTextCopy.Converters;

/// <summary>true =&gt; Visible, false =&gt; Collapsed. Use ConverterParameter="Invert" to flip.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>Non-empty / non-null string =&gt; Visible.</summary>
public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool has = value is string s && !string.IsNullOrWhiteSpace(s);
        if (parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            has = !has;
        return has ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverts a boolean.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Formats a 0..1 progress fraction as a whole-number percentage, e.g. 0.42 =&gt; "42%".</summary>
public sealed class FractionToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double fraction = value is double d ? d : 0;
        int percent = (int)Math.Round(Math.Clamp(fraction, 0, 1) * 100);
        return percent + "%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Compares the bound value to the parameter for equality (for radio-style enum binding).</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b && parameter is not null
            ? Enum.Parse(targetType, parameter.ToString()!)
            : Binding.DoNothing;
}

/// <summary>
/// Two-way maps an enum value to its integer index and back, for binding a
/// ComboBox's SelectedIndex to an enum property. Uses a shared singleton so it
/// can be referenced from XAML via x:Static without a resource declaration.
/// </summary>
public sealed class EnumIndexConverter : IValueConverter
{
    public static readonly EnumIndexConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? 0 : System.Convert.ToInt32(value, culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && targetType.IsEnum && Enum.IsDefined(targetType, i))
            return Enum.ToObject(targetType, i);
        return Binding.DoNothing;
    }
}

/// <summary>
/// Picks a <see cref="FlowDirection"/> from the bound text's own content using
/// the Unicode first-strong-character rule, so recognised/translated text reads
/// naturally regardless of the app's UI language. Prevents an RTL Persian shell
/// from mirroring Latin/mixed content (and vice-versa).
/// </summary>
public sealed class TextToFlowDirectionConverter : IValueConverter
{
    public static readonly TextToFlowDirectionConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => TextDirection.IsRightToLeft(value as string)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
