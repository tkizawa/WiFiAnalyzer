using WiFiAnalyzer.Models;

namespace WiFiAnalyzer.Services;

/// <summary>
/// Wi-Fi スキャン結果データ
/// </summary>
public sealed class WifiScanResult
{
    /// <summary>検出されたアクセスポイント一覧</summary>
    public required IReadOnlyList<AccessPointInfo> AccessPoints { get; init; }

    /// <summary>現在接続中のアクセスポイント情報（未接続の場合は null）</summary>
    public AccessPointInfo? ConnectedAccessPoint { get; init; }

    /// <summary>使用された Wi-Fi アダプター名</summary>
    public string? AdapterName { get; init; }

    /// <summary>Wi-Fi アダプターが有効に存在するかどうか</summary>
    public bool HasAdapter { get; init; } = true;

    /// <summary>位置情報サービス起因の SSID 欠落警告が必要かどうか</summary>
    public bool NeedsLocationServiceNotice { get; init; } = false;

    /// <summary>エラーメッセージ（正常終了時は null）</summary>
    public string? ErrorMessage { get; init; }
}
