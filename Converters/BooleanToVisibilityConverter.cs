using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WiFiAnalyzer.Converters;

/// <summary>
/// 真偽値を Visibility (Visible / Collapsed) に変換します。
/// Parameter に "Inverse" を指定すると反転します。
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolVal = value switch
        {
            bool b => b,
            null => false,
            _ => true
        };
        bool isInverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);

        if (isInverse) boolVal = !boolVal;

        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility vis)
        {
            bool boolVal = vis == Visibility.Visible;
            bool isInverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
            return isInverse ? !boolVal : boolVal;
        }
        return false;
    }
}
