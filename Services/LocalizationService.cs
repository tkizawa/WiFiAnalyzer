using System.Globalization;
using System.Windows;

namespace WiFiAnalyzer.Services;

/// <summary>
/// アプリケーションの表示言語管理および切り替えサービス
/// </summary>
public sealed class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    private const string StringsJaPath = "Resources/Strings.ja-JP.xaml";
    private const string StringsEnPath = "Resources/Strings.en-US.xaml";

    /// <summary>現在の設定言語コード ("ja-JP", "en-US")</summary>
    public string CurrentCultureCode { get; private set; } = "ja-JP";

    public event EventHandler? LanguageChanged;

    private LocalizationService() { }

    /// <summary>
    /// 指定された言語設定（"Auto", "ja-JP", "en-US"）に基づいてリソース辞書を適用します。
    /// </summary>
    public void ApplyLanguage(string languageSetting)
    {
        string targetCode = languageSetting switch
        {
            "ja-JP" => "ja-JP",
            "en-US" => "en-US",
            _ => CultureInfo.CurrentUICulture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "ja-JP" : "en-US"
        };

        CurrentCultureCode = targetCode;
        string resourcePath = targetCode == "ja-JP" ? StringsJaPath : StringsEnPath;

        try
        {
            var dictUri = new Uri(resourcePath, UriKind.Relative);
            var newDict = new ResourceDictionary { Source = dictUri };

            var appResources = Application.Current.Resources;
            var oldDict = appResources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.Contains("Strings.ja-JP") || d.Source.OriginalString.Contains("Strings.en-US")));

            if (oldDict != null)
            {
                int index = appResources.MergedDictionaries.IndexOf(oldDict);
                appResources.MergedDictionaries[index] = newDict;
            }
            else
            {
                appResources.MergedDictionaries.Add(newDict);
            }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // リソース読み込み失敗時のフォールバック（無視）
        }
    }

    /// <summary>
    /// リソース辞書からローカライズされた文字列を取得します。
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        if (Application.Current?.TryFindResource(key) is string text)
        {
            return args.Length > 0 ? string.Format(text, args) : text;
        }
        return key;
    }
}
