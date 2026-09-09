# WoodStream Wi-Fi Analyzer (WoodStream Wi-Fiアナライザー)

周囲の Wi-Fi アクセスポイント（AP）をスキャンしてリアルタイムに一覧表示する Windows デスクトップアプリケーションです。

![WoodStream Wi-Fi Analyzer Icon](Resources/app.ico)

---

📖 **ユーザー向けドキュメント**: [操作説明書 (MANUAL.md)](MANUAL.md)

---

## 主な機能

- **Wi-Fi リアルタイムスキャン & 一覧表示**:
  - SSID（非公開時は `<非公開ネットワーク>` / `<Hidden Network>` 表示）
  - BSSID（MACアドレス）
  - 電波強度（グラフィカルカラーインジケーター & % & RSSI dBm）
  - 周波数帯 / チャンネル（2.4 GHz / 5 GHz / 6 GHz、Wi-Fi 6E / Wi-Fi 7 対応）
  - セキュリティ・暗号化方式（WPA2-Personal, WPA3-SAE, Open 等 / AES, TKIP 等）
  - 物理規格（802.11ax, 802.11ac, 802.11n 等）
- **リアルタイム自動更新**:
  - `PeriodicTimer` による完全非同期バックグラウンドスキャン（UI のフリーズ一切なし）
  - スキャン間隔の選択（3秒 / 5秒 / 10秒）
  - 自動スキャンの開始 / 停止トグル
  - 手動更新（「今すぐスキャン」ボタン）
- **接続中アクセスポイントの自動特定 & 強調表示**:
  - アクティブな Wi-Fi インターフェースの接続先（BSSID / SSID）を特定
  - 最上部に「接続中のネットワーク」専用カードを表示
  - 一覧行をアクセントカラーでハイライト＆「接続中」バッジを表示
- **周波数帯フィルター & 検索**:
  - バンド別ワンクリック絞り込み（すべて / 2.4 GHz / 5 GHz / 6 GHz）
  - SSID / BSSID のインクリメンタル検索ボックス
- **ピン止め（お気に入り）& 並び替え**:
  - アクセスポイントごとのピン留め（★ / 📌）登録・解除
  - SSID 列での並び順指定（「SSID 名順 (A-Z / Z-A)」「既定（電波強度順）」）
  - 「お気に入りを上位表示」トグルによるピン留め AP の最優先表示
  - ピン留め状態およびソート設定の永続化
- **多言語対応 (Bilingual)**:
  - 日本語（ja-JP）および英語（en-US）に対応
  - Windows の表示言語設定に自動連動、UI 上のドロップダウンからも即座に切り替え可能
- **設定・ウィンドウ状態の永続化**:
  - 終了時のウィンドウ位置、サイズ、最大化状態を次回起動時に正確に復元
  - `%LocalAppData%\WiFiAnalyzer\settings.json` に UTF-8 可視テキスト（Unicode エスケープなし）で保存

---

## 開発環境・技術スタック

- **ターゲットフレームワーク**: .NET 10 (`net10.0-windows10.0.19041.0`)
- **UI フレームワーク**: WPF (Windows Presentation Foundation)
- **言語バージョン**: C# 14 (`<LangVersion>preview</LangVersion>`)
- **主要ライブラリ**:
  - [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) (8.4.2)
  - [ManagedNativeWifi](https://github.com/emoacht/ManagedNativeWifi) (3.0.2)
  - [WPF-UI](https://github.com/lepoco/wpfui) (4.3.0)

---

## ビルドおよび実行

### 前提条件
- .NET 10 SDK (`10.0.300` 以降)
- Windows 10 / 11

### 実行方法
```powershell
# デバッグ実行
dotnet run --project WiFiAnalyzer.csproj

# リリースビルド
dotnet build -c Release
```

### パッケージ（インストーラー / ポータブル版）の作成

セットアップインストーラー（Inno Setup 形式）およびインストール不要でそのまま起動できるポータブル版（単一 exe）を作成するスクリプトが用意されています。

```powershell
# すべて（x64 / Arm64 の通常インストーラーおよびポータブル版）を作成
.\build-installer.ps1 -Architecture all -Type all

# 通常インストーラー（Setup）のみ作成
.\build-installer.ps1 -Architecture all -Type installer

# ポータブル版（単一 exe）のみ作成
.\build-installer.ps1 -Architecture all -Type portable

# x64 のポータブル版のみ作成
.\build-installer.ps1 -Architecture x64 -Type portable
```

作成された成果物は `.\Installer` フォルダに出力されます:
- **セットアップインストーラー**: `WiFiAnalyzer_Setup_v<バージョン>_<アーキテクチャ>.exe`
- **ポータブル版（単一 exe）**: `WiFiAnalyzer_Portable_v<バージョン>_<アーキテクチャ>.exe`


---

## ライセンス
MIT License
