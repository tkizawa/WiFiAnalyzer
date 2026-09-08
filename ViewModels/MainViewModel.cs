using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiFiAnalyzer.Models;
using WiFiAnalyzer.Services;

namespace WiFiAnalyzer.ViewModels;

/// <summary>
/// メインウィンドウの ViewModel
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IWifiScannerService _scannerService;
    private readonly ISettingsService _settingsService;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    private CancellationTokenSource? _scanLoopCts;
    private Task? _scanLoopTask;
    private List<AccessPointInfo> _allAccessPoints = [];
    private readonly HashSet<string> _pinnedKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

    // --- Observable Properties (CommunityToolkit.Mvvm) ---

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isAutoScanEnabled = true;

    [ObservableProperty]
    private int _scanIntervalSeconds = 3;

    [ObservableProperty]
    private BandType _selectedBandFilter = BandType.All;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private AccessPointInfo? _connectedAccessPoint;

    [ObservableProperty]
    private string _adapterName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _lastScanTimeDisplay = "--:--:--";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasWarning;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private string _currentLanguage = "Auto";

    /// <summary>
    /// お気に入りを常に上位表示するかどうか
    /// </summary>
    [ObservableProperty]
    private bool _isFavoritesOnTop = true;

    /// <summary>
    /// SSID の並び順（既定、昇順、降順）
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSsidSortDefault))]
    [NotifyPropertyChangedFor(nameof(IsSsidSortAscending))]
    [NotifyPropertyChangedFor(nameof(IsSsidSortDescending))]
    private SsidSortOrder _ssidSortOrder = SsidSortOrder.Default;

    public bool IsSsidSortDefault => SsidSortOrder == SsidSortOrder.Default;
    public bool IsSsidSortAscending => SsidSortOrder == SsidSortOrder.Ascending;
    public bool IsSsidSortDescending => SsidSortOrder == SsidSortOrder.Descending;

    /// <summary>
    /// UI にバインドされるフィルタ・ソート適用済みのアクセスポイント一覧
    /// </summary>
    public ObservableCollection<AccessPointInfo> DisplayAccessPoints { get; } = [];

    /// <summary>
    /// 利用可能なスキャン間隔の選択肢
    /// </summary>
    public IReadOnlyList<int> AvailableIntervals { get; } = [3, 5, 10];

    /// <summary>
    /// アプリケーションのバージョン表示文字列 (例: Version 1.0.0.0)
    /// </summary>
    public string AppVersionDisplay { get; } =
        $"Version {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(4) ?? "1.0.0.0"}";

    /// <summary>
    /// ウィンドウタイトル表示文字列
    /// </summary>
    public string WindowTitle => $"{_loc.GetString("App_Title")} - {AppVersionDisplay}";

    /// <summary>
    /// コンストラクター
    /// </summary>
    public MainViewModel(IWifiScannerService scannerService, ISettingsService settingsService)
    {
        _scannerService = scannerService;
        _settingsService = settingsService;

        // 設定の読み込みと適用
        var settings = _settingsService.Load();
        _isAutoScanEnabled = settings.IsAutoScanEnabled;
        _scanIntervalSeconds = settings.ScanIntervalSeconds is 3 or 5 or 10 ? settings.ScanIntervalSeconds : 3;
        _selectedBandFilter = settings.SelectedBandFilter;
        _currentLanguage = settings.Language;
        _isFavoritesOnTop = settings.IsFavoritesOnTop;
        _ssidSortOrder = settings.SsidSortOrder;

        foreach (var key in settings.PinnedKeys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _pinnedKeys.Add(key);
            }
        }

        _loc.ApplyLanguage(_currentLanguage);
        _loc.LanguageChanged += (_, _) =>
        {
            UpdateStatusMessages();
            OnPropertyChanged(nameof(WindowTitle));
        };

        StatusMessage = _loc.GetString("Status_Ready");
    }

    /// <summary>
    /// 初回起動時の初期化および自動スキャンループの開始
    /// </summary>
    public async Task InitializeAsync()
    {
        // 最初のスキャンを即座に実行
        await ExecuteScanAsync();

        // 自動スキャンが有効な場合はバックグラウンドループを開始
        if (IsAutoScanEnabled)
        {
            StartAutoScanLoop();
        }
    }

    /// <summary>
    /// 手動スキャンコマンド
    /// </summary>
    [RelayCommand]
    private async Task ManualScanAsync()
    {
        if (IsScanning) return;
        await ExecuteScanAsync();
    }

    /// <summary>
    /// 自動スキャンの有効/無効トグル変更時のハンドラー
    /// </summary>
    partial void OnIsAutoScanEnabledChanged(bool value)
    {
        SaveCurrentSettings();
        if (value)
        {
            StartAutoScanLoop();
        }
        else
        {
            StopAutoScanLoop();
        }
    }

    /// <summary>
    /// スキャン間隔変更時のハンドラー
    /// </summary>
    partial void OnScanIntervalSecondsChanged(int value)
    {
        SaveCurrentSettings();
        if (IsAutoScanEnabled)
        {
            // 間隔変更を反映するためループを再起動
            StopAutoScanLoop();
            StartAutoScanLoop();
        }
    }

    /// <summary>
    /// 周波数帯フィルター変更時のハンドラー
    /// </summary>
    partial void OnSelectedBandFilterChanged(BandType value)
    {
        SaveCurrentSettings();
        ApplyFilter();
    }

    /// <summary>
    /// 検索テキスト変更時のハンドラー
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// 言語切り替えコマンド
    /// </summary>
    [RelayCommand]
    private void SwitchLanguage(string targetLanguage)
    {
        CurrentLanguage = targetLanguage;
        _loc.ApplyLanguage(targetLanguage);
        SaveCurrentSettings();
        ApplyFilter();
    }

    /// <summary>
    /// アクセスポイントのピン留め（お気に入り）トグルコマンド
    /// </summary>
    [RelayCommand]
    private void TogglePin(AccessPointInfo? ap)
    {
        if (ap == null) return;

        ap.IsPinned = !ap.IsPinned;
        string key = !string.IsNullOrWhiteSpace(ap.Bssid) ? ap.Bssid : ap.Ssid;

        if (ap.IsPinned)
        {
            _pinnedKeys.Add(key);
        }
        else
        {
            _pinnedKeys.Remove(key);
        }

        SaveCurrentSettings();
        ApplyFilter();
    }

    /// <summary>
    /// お気に入りを上位表示するかどうかのトグルコマンド
    /// </summary>
    [RelayCommand]
    private void ToggleFavoritesOnTop()
    {
        IsFavoritesOnTop = !IsFavoritesOnTop;
        SaveCurrentSettings();
        ApplyFilter();
    }

    /// <summary>
    /// SSID ソート順を切り替え（既定 → 昇順 → 降順 → 既定）
    /// </summary>
    [RelayCommand]
    private void CycleSsidSortOrder()
    {
        SsidSortOrder = SsidSortOrder switch
        {
            SsidSortOrder.Default => SsidSortOrder.Ascending,
            SsidSortOrder.Ascending => SsidSortOrder.Descending,
            _ => SsidSortOrder.Default
        };
        SaveCurrentSettings();
        ApplyFilter();
    }

    /// <summary>
    /// 指定の SSID ソート順を設定するコマンド
    /// </summary>
    [RelayCommand]
    private void SetSsidSortOrder(SsidSortOrder order)
    {
        SsidSortOrder = order;
        SaveCurrentSettings();
        ApplyFilter();
    }

    private readonly CancellationTokenSource _appLifetimeCts = new();

    /// <summary>
    /// 単一のスキャン処理を実行します
    /// </summary>
    private async Task ExecuteScanAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        IsScanning = true;
        StatusMessage = _loc.GetString("Status_Scanning");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appLifetimeCts.Token);

        try
        {
            var result = await _scannerService.ScanAsync(linkedCts.Token).ConfigureAwait(false);

            if (Application.Current != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_isDisposed) return;

                    AdapterName = result.AdapterName ?? string.Empty;
                    ConnectedAccessPoint = result.ConnectedAccessPoint;

                    // ピン留め状態を反映
                    foreach (var ap in result.AccessPoints)
                    {
                        string key = !string.IsNullOrWhiteSpace(ap.Bssid) ? ap.Bssid : ap.Ssid;
                        ap.IsPinned = _pinnedKeys.Contains(key) || (!string.IsNullOrWhiteSpace(ap.Bssid) && _pinnedKeys.Contains(ap.Bssid));
                    }

                    _allAccessPoints = [.. result.AccessPoints];

                    // 警告チェック
                    if (!result.HasAdapter)
                    {
                        HasWarning = true;
                        WarningMessage = result.ErrorMessage ?? _loc.GetString("Status_NoAdapter");
                    }
                    else if (result.NeedsLocationServiceNotice)
                    {
                        HasWarning = true;
                        WarningMessage = _loc.GetString("Status_LocationWarning");
                    }
                    else
                    {
                        HasWarning = false;
                        WarningMessage = string.Empty;
                    }

                    LastScanTimeDisplay = DateTime.Now.ToString("HH:mm:ss");
                    ApplyFilter();
                });
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は正常終了
        }
        catch (Exception ex)
        {
            if (Application.Current != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    HasWarning = true;
                    WarningMessage = ex.Message;
                });
            }
        }
        finally
        {
            if (Application.Current != null && !_isDisposed)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsScanning = false;
                    UpdateStatusMessages();
                });
            }
        }
    }

    /// <summary>
    /// PeriodicTimer を利用したノンブロッキングな自動スキャンループを開始
    /// </summary>
    private void StartAutoScanLoop()
    {
        StopAutoScanLoop();
        _scanLoopCts = new CancellationTokenSource();
        var token = _scanLoopCts.Token;

        _scanLoopTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, ScanIntervalSeconds)));
            while (!token.IsCancellationRequested && !_appLifetimeCts.IsCancellationRequested)
            {
                try
                {
                    if (!await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (!IsScanning && !token.IsCancellationRequested)
                    {
                        await ExecuteScanAsync(token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // ループ継続
                }
            }
        }, token);
    }

    /// <summary>
    /// 自動スキャンループを停止
    /// </summary>
    private void StopAutoScanLoop()
    {
        if (_scanLoopCts != null)
        {
            _scanLoopCts.Cancel();
            _scanLoopCts.Dispose();
            _scanLoopCts = null;
        }
    }

    /// <summary>
    /// 周波数帯および検索テキストによるフィルタリング、およびピン留め・ソート順を適用してコレクションを更新
    /// </summary>
    private void ApplyFilter()
    {
        var query = _allAccessPoints.AsEnumerable();

        // 周波数帯フィルター
        if (SelectedBandFilter != BandType.All)
        {
            query = query.Where(ap => ap.Band == SelectedBandFilter);
        }

        // 検索文字列フィルター
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string q = SearchText.Trim();
            query = query.Where(ap =>
                ap.Ssid.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                ap.Bssid.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        // ソート処理
        // 非公開ネットワークは無条件に最下部に配置 (false: 公開ネットワーク, true: 非公開ネットワーク)
        IOrderedEnumerable<AccessPointInfo> ordered = query.OrderBy(ap => ap.IsHidden);

        if (IsFavoritesOnTop)
        {
            // お気に入りを最優先で上位表示
            ordered = ordered.ThenByDescending(ap => ap.IsPinned);

            ordered = SsidSortOrder switch
            {
                SsidSortOrder.Ascending => ordered
                    .ThenBy(ap => ap.Ssid, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(ap => ap.SignalQuality),
                SsidSortOrder.Descending => ordered
                    .ThenByDescending(ap => ap.Ssid, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(ap => ap.SignalQuality),
                _ => ordered
                    .ThenByDescending(ap => ap.IsConnected)
                    .ThenByDescending(ap => ap.SignalQuality)
                    .ThenBy(ap => ap.Ssid)
            };
        }
        else
        {
            ordered = SsidSortOrder switch
            {
                SsidSortOrder.Ascending => ordered
                    .ThenBy(ap => ap.Ssid, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(ap => ap.SignalQuality),
                SsidSortOrder.Descending => ordered
                    .ThenByDescending(ap => ap.Ssid, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(ap => ap.SignalQuality),
                _ => ordered
                    .ThenByDescending(ap => ap.IsConnected)
                    .ThenByDescending(ap => ap.SignalQuality)
                    .ThenBy(ap => ap.Ssid)
            };
        }

        var resultList = ordered.ToList();

        DisplayAccessPoints.Clear();
        foreach (var item in resultList)
        {
            DisplayAccessPoints.Add(item);
        }

        TotalCount = DisplayAccessPoints.Count;
        UpdateStatusMessages();
    }

    /// <summary>
    /// ステータスバー表示文字列を更新
    /// </summary>
    private void UpdateStatusMessages()
    {
        if (IsScanning)
        {
            StatusMessage = _loc.GetString("Status_Scanning");
        }
        else
        {
            string totalStr = _loc.GetString("Status_TotalNetworks", TotalCount);
            string lastScanStr = _loc.GetString("Status_LastScan", LastScanTimeDisplay);
            StatusMessage = $"{totalStr} | {lastScanStr}";
        }
    }

    /// <summary>
    /// 現在のユーザー設定を保存
    /// </summary>
    public void SaveCurrentSettings()
    {
        var settings = _settingsService.Load();
        settings.IsAutoScanEnabled = IsAutoScanEnabled;
        settings.ScanIntervalSeconds = ScanIntervalSeconds;
        settings.SelectedBandFilter = SelectedBandFilter;
        settings.Language = CurrentLanguage;
        settings.PinnedKeys = _pinnedKeys.ToList();
        settings.IsFavoritesOnTop = IsFavoritesOnTop;
        settings.SsidSortOrder = SsidSortOrder;
        _settingsService.Save(settings);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _appLifetimeCts.Cancel();
        StopAutoScanLoop();
        _appLifetimeCts.Dispose();
        _scannerService.Dispose();
    }
}
