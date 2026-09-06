using WiFiAnalyzer.Models;

namespace WiFiAnalyzer.Services;

/// <summary>
/// アプリケーション設定の読み込み・保存サービスのインターフェース
/// </summary>
public interface ISettingsService
{
    /// <summary>設定を読み込みます。存在しない場合は既定値を返します。</summary>
    AppSettings Load();

    /// <summary>設定を保存します。</summary>
    void Save(AppSettings settings);
}
