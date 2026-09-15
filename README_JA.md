# Vibecore Hub

[中文](README.md) · [English](README_EN.md) · [日本語](README_JA.md)

Vibecore Hub は、Windows 向けのネイティブなモジュール式デスクトップツールバーです。普段は小さなバーとしてデスクトップに常駐し、必要な機能だけを下方向へ展開できます。

> 現在のバージョン：1.2.1 · Windows 10/11 · x64

軽量・ポータブルで、自由にカスタマイズ可能。よく使うアプリ、ファイル、フォルダー、メモ、オーディオデバイスを、美しい小さなウィンドウにまとめられます。

## 現在のモジュール

- ショートカットライブラリ：アプリ、ファイル、フォルダー、定型文、ショートカットグループ
- メモ：コンパクトなカード、独立表示ウィンドウ、複数列レイアウト
- 全体検索：ショートカット、フォルダー、メモを横断検索
- フォルダーディレクトリ：よく使うフォルダーをすばやく開く
- オーディオ切り替え：入出力プリセットを保存し、既定の再生デバイスと通信デバイスを同時に切り替え
- PC プロファイル：PC ごとにデータとウィンドウ配置を個別保存

## 技術概要

- Windows ネイティブ WPF アプリ
- .NET 8
- Core Audio をネイティブに利用
- 各機能は独立したモジュールに分割され、`IHubModule` 経由で登録
- WebView やサードパーティ製ランタイムは不要

## ローカルビルド

```powershell
dotnet restore desktop\VibecoreHub.Desktop.csproj -r win-x64
dotnet build desktop\VibecoreHub.Desktop.csproj -c Release -r win-x64
```

## ポータブル版の作成

```powershell
dotnet publish desktop\VibecoreHub.Desktop.csproj -c Release -r win-x64 --self-contained true -o "release\Vibecore Hub" -p:PublishReadyToRun=false
```

アプリのデータは実行ファイルと同じ場所にある `data` フォルダーへ保存されます。このフォルダーは `.gitignore` の対象です。個人データをソースコードへ含めないでください。

## ダウンロード

ポータブル版は [Releases](https://github.com/VibecoreStudio/Vibecore-Hub/releases) ページからダウンロードできます。展開後、`VibecoreHub.exe` を実行するだけで利用できます。

## データとプライバシー

- 設定と内容はローカルの `data` フォルダーに保存
- 個人データを外部へ送信せず、PC 移行時はフォルダーごとコピー可能
- アカウント登録は不要

## 開発状況

Vibecore Hub は継続的に開発中です。不具合報告、提案、便利な軽量モジュールのアイデアを歓迎します。
