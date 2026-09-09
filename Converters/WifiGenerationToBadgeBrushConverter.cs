using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WiFiAnalyzer.Converters;

/// <summary>
/// Wi-Fi 世代（Wi-Fi 7, Wi-Fi 6/6E, Wi-Fi 5, Wi-Fi 4）に応じたバッジの背景色・枠線色・文字色ブラシを返します。
/// </summary>
public sealed class WifiGenerationToBadgeBrushConverter : IValueConverter
{
    // Wi-Fi 7: Purple
    private static readonly SolidColorBrush Wifi7Bg = new(Color.FromArgb(30, 168, 85, 247));
    private static readonly SolidColorBrush Wifi7Border = new(Color.FromRgb(168, 85, 247));
    private static readonly SolidColorBrush Wifi7Fg = new(Color.FromRgb(126, 34, 206));

    // Wi-Fi 6 / 6E: Emerald / Teal
    private static readonly SolidColorBrush Wifi6Bg = new(Color.FromArgb(30, 16, 185, 129));
    private static readonly SolidColorBrush Wifi6Border = new(Color.FromRgb(16, 185, 129));
    private static readonly SolidColorBrush Wifi6Fg = new(Color.FromRgb(4, 120, 87));

    // Wi-Fi 5: Cyan / Sky
    private static readonly SolidColorBrush Wifi5Bg = new(Color.FromArgb(30, 14, 165, 233));
    private static readonly SolidColorBrush Wifi5Border = new(Color.FromRgb(14, 165, 233));
    private static readonly SolidColorBrush Wifi5Fg = new(Color.FromRgb(3, 105, 161));

    // Wi-Fi 4: Slate / Blue-Gray
    private static readonly SolidColorBrush Wifi4Bg = new(Color.FromArgb(25, 100, 116, 139));
    private static readonly SolidColorBrush Wifi4Border = new(Color.FromRgb(148, 163, 184));
    private static readonly SolidColorBrush Wifi4Fg = new(Color.FromRgb(71, 85, 105));

    // Default / Other
    private static readonly SolidColorBrush DefaultBg = new(Color.FromArgb(20, 100, 116, 139));
    private static readonly SolidColorBrush DefaultBorder = new(Color.FromRgb(203, 213, 225));
    private static readonly SolidColorBrush DefaultFg = new(Color.FromRgb(100, 116, 139));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string param = parameter?.ToString() ?? "Background";
        string gen = value?.ToString() ?? string.Empty;

        return gen switch
        {
            "Wi-Fi 7" => param switch
            {
                "Border" => Wifi7Border,
                "Foreground" => Wifi7Fg,
                _ => Wifi7Bg
            },
            "Wi-Fi 6" or "Wi-Fi 6E" => param switch
            {
                "Border" => Wifi6Border,
                "Foreground" => Wifi6Fg,
                _ => Wifi6Bg
            },
            "Wi-Fi 5" => param switch
            {
                "Border" => Wifi5Border,
                "Foreground" => Wifi5Fg,
                _ => Wifi5Bg
            },
            "Wi-Fi 4" => param switch
            {
                "Border" => Wifi4Border,
                "Foreground" => Wifi4Fg,
                _ => Wifi4Bg
            },
            _ => param switch
            {
                "Border" => DefaultBorder,
                "Foreground" => DefaultFg,
                _ => DefaultBg
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
