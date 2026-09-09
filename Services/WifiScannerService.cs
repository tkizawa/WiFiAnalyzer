using System.Text.RegularExpressions;
using ManagedNativeWifi;
using WiFiAnalyzer.Models;

namespace WiFiAnalyzer.Services;

/// <summary>
/// ManagedNativeWifi を用いたネイティブ Wi-Fi スキャナーサービス実装
/// </summary>
public sealed class WifiScannerService : IWifiScannerService
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private bool _isDisposed;

    /// <summary>
    /// 周囲の Wi-Fi ネットワークを非同期にスキャンし、結果を取得します。
    /// </summary>
    public async Task<WifiScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        try
        {
            // 1. Wi-Fi インターフェースの列挙
            var interfaces = NativeWifi.EnumerateInterfaces().ToList();
            if (interfaces.Count == 0)
            {
                return new WifiScanResult
                {
                    AccessPoints = [],
                    HasAdapter = false,
                    ErrorMessage = _loc.GetString("Status_NoAdapter")
                };
            }

            var primaryInterface = interfaces[0];
            string adapterName = primaryInterface.Description;

            // 2. ネイティブ Wi-Fi スキャン要求（タイムアウト 2 秒）
            try
            {
                await NativeWifi.ScanNetworksAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // スキャン要求がハードウェアやドライバ制約でタイムアウトした場合でも既存キャッシュ情報で続行
            }

            // 3. 現在接続中の BSSID および SSID を特定
            string? connectedBssid = null;
            string? connectedSsid = null;
            foreach (var iface in interfaces)
            {
                var (_, conn) = NativeWifi.GetCurrentConnection(iface.Id);
                if (conn != null && conn.InterfaceState == InterfaceState.Connected)
                {
                    if (conn.Bssid != null)
                    {
                        connectedBssid = FormatMacAddress(conn.Bssid.ToString());
                    }
                    connectedSsid = conn.Ssid?.ToString();
                    adapterName = iface.Description;
                    break;
                }
            }

            // 4. 利用可能ネットワーク一覧（認証・暗号化方式用）をマップ化
            var authCipherMap = new Dictionary<string, (string Auth, string Cipher)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var net in NativeWifi.EnumerateAvailableNetworks())
                {
                    string ssidStr = net.Ssid?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(ssidStr) && !authCipherMap.ContainsKey(ssidStr))
                    {
                        authCipherMap[ssidStr] = (
                            net.AuthenticationAlgorithm.ToString(),
                            net.CipherAlgorithm.ToString()
                        );
                    }
                }
            }
            catch
            {
                // 利用可能ネットワーク取得エラー時はデフォルト値を使用
            }

            // 5. BSS ネットワーク（各 AP の詳細）を取得
            var bssNetworks = NativeWifi.EnumerateBssNetworks().ToList();
            var apList = new List<AccessPointInfo>(bssNetworks.Count);
            int hiddenCount = 0;

            foreach (var bss in bssNetworks)
            {
                string rawSsid = bss.Ssid?.ToString() ?? string.Empty;
                bool isHidden = string.IsNullOrWhiteSpace(rawSsid);
                if (isHidden)
                {
                    hiddenCount++;
                }

                string displaySsid = isHidden
                    ? _loc.GetString("Grid_HiddenNetwork")
                    : rawSsid;

                string formattedBssid = FormatMacAddress(bss.Bssid?.ToString() ?? string.Empty);

                // 周波数とバンドの判定
                double freqMhz = bss.Frequency / 1000.0;
                var (bandType, bandDisplay) = DetermineBand(bss.Band, freqMhz);

                // 認証・暗号化方式の紐付け
                string auth = "Unknown";
                string cipher = "None";
                if (!isHidden && authCipherMap.TryGetValue(rawSsid, out var ac))
                {
                    auth = FormatAuthName(ac.Auth);
                    cipher = ac.Cipher;
                }

                // 物理無線タイプ（PHY規格）と Wi-Fi 世代・IEEE規格
                var (wifiGen, ieeeStd, radioDisplay) = DetermineRadioType(bss.PhyType, bandType);

                // 接続中判定: BSSID が判明している場合は BSSID の完全一致のみ
                bool isConnected = !string.IsNullOrEmpty(connectedBssid) &&
                                   string.Equals(formattedBssid, connectedBssid, StringComparison.OrdinalIgnoreCase);

                var ap = new AccessPointInfo
                {
                    Ssid = displaySsid,
                    IsHidden = isHidden,
                    Bssid = formattedBssid,
                    SignalQuality = Math.Clamp(bss.LinkQuality, 0, 100),
                    Rssi = bss.Rssi,
                    Band = bandType,
                    BandDisplay = bandDisplay,
                    Channel = bss.Channel,
                    FrequencyMHz = freqMhz,
                    Authentication = auth,
                    Cipher = cipher,
                    RadioType = radioDisplay,
                    WifiGeneration = wifiGen,
                    IeeeStandard = ieeeStd,
                    RadioTypeDisplay = radioDisplay,
                    IsConnected = isConnected
                };

                apList.Add(ap);
            }

            // 万が一 BSSID が未特定で SSID のみ取得できた場合、同一 SSID の中で最大電波強度の 1 件のみを接続中とする
            if (string.IsNullOrEmpty(connectedBssid) && !string.IsNullOrEmpty(connectedSsid))
            {
                var candidate = apList
                    .Where(x => !x.IsHidden && string.Equals(x.Ssid, connectedSsid, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.SignalQuality)
                    .FirstOrDefault();

                if (candidate != null)
                {
                    candidate.IsConnected = true;
                }
            }

            // 初期ソート（非公開ネットワークは最下部、接続中優先、電波強度降順）
            var sortedApList = apList
                .OrderBy(x => x.IsHidden)
                .ThenByDescending(x => x.IsConnected)
                .ThenByDescending(x => x.SignalQuality)
                .ThenBy(x => x.Ssid)
                .ToList();

            var connectedAp = sortedApList.FirstOrDefault(x => x.IsConnected);

            // 位置情報サービス無効判定: AP が複数あるのに SSID がすべて非公開の場合
            bool locationWarning = bssNetworks.Count > 0 && hiddenCount == bssNetworks.Count;

            return new WifiScanResult
            {
                AccessPoints = sortedApList,
                ConnectedAccessPoint = connectedAp,
                AdapterName = adapterName,
                HasAdapter = true,
                NeedsLocationServiceNotice = locationWarning,
                ErrorMessage = null
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new WifiScanResult
            {
                AccessPoints = [],
                HasAdapter = true,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 周波数およびバンド情報から BandType と表示文字列を特定します。
    /// </summary>
    private static (BandType Band, string Display) DetermineBand(float bandNumber, double freqMhz)
    {
        int bandInt = (int)Math.Round(bandNumber);
        if (bandInt == 6 || freqMhz >= 5925)
        {
            return (BandType.Band6GHz, "6 GHz");
        }
        if (bandInt == 5 || (freqMhz >= 5000 && freqMhz < 5925))
        {
            return (BandType.Band5GHz, "5 GHz");
        }
        return (BandType.Band24GHz, "2.4 GHz");
    }

    /// <summary>
    /// MAC アドレスを XX:XX:XX:XX:XX:XX 形式に標準化します。
    /// </summary>
    private static string FormatMacAddress(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "--:--:--:--:--:--";
        string clean = Regex.Replace(raw, "[^0-9A-Fa-f]", "").ToUpperInvariant();
        if (clean.Length == 12)
        {
            return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
        }
        return raw;
    }

    /// <summary>
    /// 認証アルゴリズム名をユーザーフレンドリーな形式に変換します。
    /// </summary>
    private static string FormatAuthName(string auth) => auth switch
    {
        "RSNA_PSK" => "WPA2-Personal",
        "RSNA" => "WPA2-Enterprise",
        "WPA_PSK" => "WPA-Personal",
        "WPA" => "WPA-Enterprise",
        "Open" => "Open",
        "Shared" => "Shared",
        "WPA3_SAE" => "WPA3-SAE",
        "WPA3_Enterprise" => "WPA3-Enterprise",
        _ => auth.Replace('_', '-')
    };

    /// <summary>
    /// ManagedNativeWifi の PhyType と周波数帯から Wi-Fi 世代名、IEEE 規格名、および総合表示文字列を特定します。
    /// </summary>
    private static (string Generation, string IeeeStandard, string FullDisplay) DetermineRadioType(PhyType phyType, BandType band)
    {
        return phyType switch
        {
            PhyType.Eht => ("Wi-Fi 7", "802.11be", "Wi-Fi 7 (IEEE 802.11be)"),
            PhyType.He => band == BandType.Band6GHz
                ? ("Wi-Fi 6E", "802.11ax", "Wi-Fi 6E (IEEE 802.11ax)")
                : ("Wi-Fi 6", "802.11ax", "Wi-Fi 6 (IEEE 802.11ax)"),
            PhyType.Vht => ("Wi-Fi 5", "802.11ac", "Wi-Fi 5 (IEEE 802.11ac)"),
            PhyType.Ht => ("Wi-Fi 4", "802.11n", "Wi-Fi 4 (IEEE 802.11n)"),
            PhyType.Erp => (string.Empty, "802.11g", "IEEE 802.11g"),
            PhyType.HrDsss => (string.Empty, "802.11b", "IEEE 802.11b"),
            PhyType.Ofdm => (string.Empty, "802.11a", "IEEE 802.11a"),
            PhyType.Dmg => ("WiGig", "802.11ad", "WiGig (IEEE 802.11ad)"),
            PhyType.Dsss => (string.Empty, "802.11", "IEEE 802.11 (DSSS)"),
            PhyType.Fhss => (string.Empty, "802.11", "IEEE 802.11 (FHSS)"),
            PhyType.IrBaseband => (string.Empty, "802.11", "IEEE 802.11 (IR)"),
            _ => (string.Empty, phyType.ToString(), phyType.ToString())
        };
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}
