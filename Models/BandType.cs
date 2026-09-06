namespace WiFiAnalyzer.Models;

/// <summary>
/// Wi-Fi 周波数帯の分類
/// </summary>
public enum BandType
{
    /// <summary>すべての周波数帯</summary>
    All = 0,

    /// <summary>2.4 GHz 帯</summary>
    Band24GHz = 1,

    /// <summary>5 GHz 帯</summary>
    Band5GHz = 2,

    /// <summary>6 GHz 帯 (Wi-Fi 6E / Wi-Fi 7)</summary>
    Band6GHz = 3
}
