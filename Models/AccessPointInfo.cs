using CommunityToolkit.Mvvm.ComponentModel;

namespace WiFiAnalyzer.Models;

/// <summary>
/// 周囲のアクセスポイント（AP）を表すデータモデル
/// </summary>
public sealed partial class AccessPointInfo : ObservableObject
{
    /// <summary>SSID（ネットワーク名）</summary>
    public required string Ssid { get; init; }

    /// <summary>非公開ネットワークかどうか</summary>
    public required bool IsHidden { get; init; }

    /// <summary>BSSID (MACアドレス)</summary>
    public required string Bssid { get; init; }

    /// <summary>電波品質 (0 - 100%)</summary>
    public required int SignalQuality { get; init; }

    /// <summary>受信信号強度 (RSSI, dBm)</summary>
    public required int Rssi { get; init; }

    /// <summary>周波数帯 (BandType)</summary>
    public required BandType Band { get; init; }

    /// <summary>周波数帯表示文字列 (例: "2.4 GHz", "5 GHz", "6 GHz")</summary>
    public required string BandDisplay { get; init; }

    /// <summary>チャンネル番号</summary>
    public required int Channel { get; init; }

    /// <summary>中心周波数 (MHz)</summary>
    public required double FrequencyMHz { get; init; }

    /// <summary>認証方式 (例: WPA2-Personal, WPA3-SAE, Open)</summary>
    public required string Authentication { get; init; }

    /// <summary>暗号化アルゴリズム (例: AES, TKIP, None)</summary>
    public required string Cipher { get; init; }

    /// <summary>物理無線タイプ (内部値または表示用)</summary>
    public required string RadioType { get; init; }

    /// <summary>Wi-Fi 世代表示（例: "Wi-Fi 6", "Wi-Fi 5", "Wi-Fi 4", ""）</summary>
    public required string WifiGeneration { get; init; }

    /// <summary>IEEE 規格名（例: "802.11ax", "802.11ac", "802.11n", "802.11g"）</summary>
    public required string IeeeStandard { get; init; }

    /// <summary>規格の表示文字列（例: "Wi-Fi 6 (IEEE 802.11ax)", "IEEE 802.11g"）</summary>
    public required string RadioTypeDisplay { get; init; }

    /// <summary>Wi-Fi 世代（Wi-Fi 4〜7 等）が存在するかどうか</summary>
    public bool HasWifiGeneration => !string.IsNullOrEmpty(WifiGeneration);

    /// <summary>現在この PC が接続中かどうか</summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>ピン留め（お気に入り）されているかどうか</summary>
    [ObservableProperty]
    private bool _isPinned;

    /// <summary>周波数表示用文字列 (例: "5540 MHz")</summary>
    public string FrequencyDisplay => $"{FrequencyMHz:F0} MHz";

    /// <summary>電波強度表示用文字列 (例: "82% (-60 dBm)")</summary>
    public string SignalDisplay => $"{SignalQuality}% ({Rssi} dBm)";

    /// <summary>セキュリティ表示用文字列 (例: "WPA2-Personal / AES")</summary>
    public string SecurityDisplay => string.IsNullOrWhiteSpace(Cipher) || Cipher == "None"
        ? Authentication
        : $"{Authentication} / {Cipher}";
}
