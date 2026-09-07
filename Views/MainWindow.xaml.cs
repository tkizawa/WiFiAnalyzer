using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WiFiAnalyzer.Models;
using WiFiAnalyzer.Services;
using WiFiAnalyzer.ViewModels;

namespace WiFiAnalyzer.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        var scannerService = new WifiScannerService();
        _viewModel = new MainViewModel(scannerService, _settingsService);
        DataContext = _viewModel;

        // プロジェクト規約: ウィンドウ位置・サイズの復元
        RestoreWindowState();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 設定ファイルが存在しない場合は初期設定を書き込み
        _viewModel.SaveCurrentSettings();

        await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// 前回の終了時ウィンドウ位置・サイズを復元します。
    /// </summary>
    private void RestoreWindowState()
    {
        var settings = _settingsService.Load();

        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualWidth = SystemParameters.VirtualScreenWidth;
        double virtualHeight = SystemParameters.VirtualScreenHeight;

        // 保存されたサイズが妥当な値かチェック
        if (settings.WindowWidth >= MinWidth && settings.WindowHeight >= MinHeight)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }

        // 保存された位置が現在の仮想画面内にあるかチェック（モニタ構成変更対応）
        if (settings.WindowLeft >= virtualLeft &&
            settings.WindowLeft + 100 <= virtualLeft + virtualWidth &&
            settings.WindowTop >= virtualTop &&
            settings.WindowTop + 100 <= virtualTop + virtualHeight)
        {
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (settings.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// ウィンドウ終了時に現在の位置・サイズ・状態を保存します。
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        var settings = _settingsService.Load();

        if (WindowState == WindowState.Maximized)
        {
            settings.IsMaximized = true;
            settings.WindowLeft = RestoreBounds.Left;
            settings.WindowTop = RestoreBounds.Top;
            settings.WindowWidth = RestoreBounds.Width;
            settings.WindowHeight = RestoreBounds.Height;
        }
        else if (WindowState == WindowState.Normal)
        {
            settings.IsMaximized = false;
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
        }

        settings.IsAutoScanEnabled = _viewModel.IsAutoScanEnabled;
        settings.ScanIntervalSeconds = _viewModel.ScanIntervalSeconds;
        settings.SelectedBandFilter = _viewModel.SelectedBandFilter;
        settings.Language = _viewModel.CurrentLanguage;
        settings.IsFavoritesOnTop = _viewModel.IsFavoritesOnTop;
        settings.SsidSortOrder = _viewModel.SsidSortOrder;

        _settingsService.Save(settings);

        _viewModel.Dispose();

        Application.Current.Shutdown();
    }

    private void OnSortMenuButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void OnFilterAllClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedBandFilter = BandType.All;
    }

    private void OnFilter24Clicked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedBandFilter = BandType.Band24GHz;
    }

    private void OnFilter5Clicked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedBandFilter = BandType.Band5GHz;
    }

    private void OnFilter6Clicked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedBandFilter = BandType.Band6GHz;
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            _viewModel.SwitchLanguageCommand.Execute(lang);
        }
    }
}
