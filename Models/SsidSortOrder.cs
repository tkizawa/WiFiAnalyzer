namespace WiFiAnalyzer.Models;

/// <summary>
/// SSID 列の並び順モード
/// </summary>
public enum SsidSortOrder
{
    /// <summary>既定（電波強度順、接続中優先）</summary>
    Default,

    /// <summary>SSID 昇順 (A → Z)</summary>
    Ascending,

    /// <summary>SSID 降順 (Z → A)</summary>
    Descending
}
