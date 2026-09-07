using System.Text.Json.Serialization;

namespace WiFiAnalyzer.Models;

/// <summary>
/// アプリケーションの設定データモデル
/// </summary>
public sealed class AppSettings
{
    /// <summary>ウィンドウの X 座標</summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>ウィンドウの Y 座標</summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>ウィンドウの幅</summary>
    public double WindowWidth { get; set; } = 1050;

    /// <summary>ウィンドウの高さ</summary>
    public double WindowHeight { get; set; } = 680;

    /// <summary>最大化状態で終了したかどうか</summary>
    public bool IsMaximized { get; set; } = false;

    /// <summary>自動スキャンが有効かどうか</summary>
    public bool IsAutoScanEnabled { get; set; } = true;

    /// <summary>スキャン間隔（秒）</summary>
    public int ScanIntervalSeconds { get; set; } = 3;

    /// <summary>選択中の周波数帯フィルター</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BandType SelectedBandFilter { get; set; } = BandType.All;

    /// <summary>表示言語設定 ("Auto", "ja-JP", "en-US")</summary>
    public string Language { get; set; } = "Auto";

    /// <summary>ピン留め（お気に入り）されたアクセスポイントのキー一覧（BSSID）</summary>
    public List<string> PinnedKeys { get; set; } = [];

    /// <summary>お気に入りを上位表示するかどうか</summary>
    public bool IsFavoritesOnTop { get; set; } = true;

    /// <summary>SSID のソート順</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SsidSortOrder SsidSortOrder { get; set; } = SsidSortOrder.Default;
}
