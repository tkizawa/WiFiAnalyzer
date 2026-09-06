namespace WiFiAnalyzer.Services;

/// <summary>
/// Wi-Fi スキャンおよびアクセスポイント情報取得サービスのインターフェース
/// </summary>
public interface IWifiScannerService : IDisposable
{
    /// <summary>
    /// 周囲の Wi-Fi ネットワークを非同期にスキャンし、詳細情報を返します。
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>スキャン結果情報</returns>
    Task<WifiScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
