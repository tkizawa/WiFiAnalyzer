using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WiFiAnalyzer.Converters;

/// <summary>
/// 電波強度 (0〜100%) に基づいて視覚的なステータス色 (SolidColorBrush) を返します。
/// </summary>
public sealed class SignalToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ExcellentBrush = new(Color.FromRgb(16, 185, 129)); // #10B981 Emerald
    private static readonly SolidColorBrush GoodBrush = new(Color.FromRgb(34, 197, 94));       // #22C55E Green
    private static readonly SolidColorBrush FairBrush = new(Color.FromRgb(245, 158, 11));      // #F59E0B Amber
    private static readonly SolidColorBrush PoorBrush = new(Color.FromRgb(239, 68, 68));       // #EF4444 Red

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int signal)
        {
            return signal switch
            {
                >= 70 => ExcellentBrush,
                >= 50 => GoodBrush,
                >= 25 => FairBrush,
                _ => PoorBrush
            };
        }
        return PoorBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
