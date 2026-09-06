using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WiFiAnalyzer.Models;

namespace WiFiAnalyzer.Converters;

/// <summary>
/// 周波数帯 (BandType) に応じたバッジの背景ブラシを返します。
/// </summary>
public sealed class BandToBadgeBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Band24Brush = new(Color.FromArgb(35, 14, 165, 233)); // Light Sky
    private static readonly SolidColorBrush Band5Brush = new(Color.FromArgb(35, 99, 102, 241));  // Light Indigo
    private static readonly SolidColorBrush Band6Brush = new(Color.FromArgb(35, 217, 70, 239));  // Light Fuchsia
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(20, 100, 116, 139));

    private static readonly SolidColorBrush Band24Border = new(Color.FromRgb(14, 165, 233));
    private static readonly SolidColorBrush Band5Border = new(Color.FromRgb(99, 102, 241));
    private static readonly SolidColorBrush Band6Border = new(Color.FromRgb(217, 70, 239));
    private static readonly SolidColorBrush DefaultBorder = new(Color.FromRgb(100, 116, 139));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isBorder = parameter?.ToString() == "Border";
        if (value is BandType band)
        {
            return band switch
            {
                BandType.Band24GHz => isBorder ? Band24Border : Band24Brush,
                BandType.Band5GHz => isBorder ? Band5Border : Band5Brush,
                BandType.Band6GHz => isBorder ? Band6Border : Band6Brush,
                _ => isBorder ? DefaultBorder : DefaultBrush
            };
        }
        return isBorder ? DefaultBorder : DefaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
