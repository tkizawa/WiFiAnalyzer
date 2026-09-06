using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WiFiAnalyzer.Models;

namespace WiFiAnalyzer.Services;

/// <summary>
/// アプリケーション設定の永続化実装クラス。
/// %LocalAppData%\WiFiAnalyzer\settings.json に UTF-8 可視テキスト（Unicodeエスケープなし）で保存します。
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WiFiAnalyzer");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // プロジェクト規約: 日本語をUnicodeエスケープせず、UTF-8可視テキストで保存
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // 読み込みエラー時は安全に既定値を返す
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // 保存エラー時はクラッシュを防止
        }
    }
}
