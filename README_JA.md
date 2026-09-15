# Vibecore Hub

[中文](README.md) · [English](README_EN.md) · [日本語](README_JA.md)

Vibecore Hub は、Windows 向けのネイティブなモジュール式デスクトップツールバーです。普段は小さなバーとしてデスクトップに常駐し、必要な機能だけを下方向へ展開できます。

<p align="center">
  <img src="docs/images/vibecore-hub-dark.png" width="300" alt="Vibecore Hub ダークテーマ">
  &nbsp;&nbsp;
  <img src="docs/images/vibecore-hub-light.png" width="300" alt="Vibecore Hub ライトテーマ">
</p>

> 現在のバージョン：1.2.1 · Windows 10/11 · x64

軽量・ポータブルで、自由にカスタマイズ可能。よく使うアプリ、ファイル、フォルダー、メモ、オーディオデバイスを、美しい小さなウィンドウにまとめられます。

## Vibecore Hub を作った理由

きっかけは Windows の付箋でした。メモが増えるにつれて必要な文章を探すのが難しくなり、デスクトップにはアプリのショートカットが増え続けました。日々の作業で使うファイルやフォルダーも、そのたびに探す必要があり、直感的ではなく、作業の流れが途切れてしまいます。

さらに、私の PC では用途の異なる 2 台のオーディオインターフェースを使用しており、入力、出力、音量を毎回切り替えるのも手間でした。そこで、ショートカット、メモ、よく使うフォルダー、オーディオデバイスを、軽量で美しく、自由に組み合わせられる一つのデスクトップ Hub にまとめようと考えました。必要なモジュールだけを開き、よく使う操作をできるだけワンクリックで完了する。ツールに合わせるのではなく、ツールが自分のワークフローに寄り添うことを目指しています。

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

## ライセンス

Copyright © 2026 VibecoreStudio

本プロジェクトは [GNU General Public License v3.0](LICENSE)（`GPL-3.0-only`）の下で公開されています。利用、研究、改変、再配布が可能です。改変版または派生版を公開配布する場合は、GPLv3 に基づいて対応するソースコードとライセンスを提供する必要があります。
