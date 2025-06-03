# 📄 PDFCon - PDF to Image Converter

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/tomo551/PDF-con.svg)](https://github.com/tomo551/PDF-con/releases)

> 🚀 シンプルで高速なPDFから画像への変換ツール

PDFConは、PDFファイルを高品質な画像（JPEG、PNG、TIFF）に変換するWindowsアプリケーションです。直感的なドラッグ&ドロップインターフェースと豊富なカスタマイズオプションを提供します。

## ✨ 主な機能

- 🎯 **ドラッグ&ドロップ対応** - PDFファイルを画面にドロップするだけ
- 📚 **一括変換** - 複数のPDFファイルを同時処理
- 🎨 **多様な出力形式** - JPEG、PNG、TIFF形式に対応
- 🌈 **カラーモード選択** - フルカラー、グレースケール、白黒
- 📏 **解像度調整** - 300〜5000 DPIの範囲で自由設定
- ⚡ **A4プリセット** - A4サイズの縦横、品質別プリセット
- 🖼️ **リアルタイムプレビュー** - 変換前に結果を確認

## 📸 スクリーンショット

> 💡 **ヒント**: `docs/screenshots/`フォルダにアプリケーションのスクリーンショットを追加することをお勧めします

## 🎯 対象ユーザー

- PDFを画像に変換したい個人ユーザー
- 文書のデジタル化を行う事務職
- プレゼンテーション資料を画像として活用したいビジネスユーザー
- 印刷業界での画像変換作業

## 💻 動作環境

| 項目 | 要件 |
|------|------|
| **OS** | Windows 10 version 1903 以上 (x64) |
| **ランタイム** | .NET 9.0 (自己完結型) |
| **CPU** | Intel Core i3 相当以上 |
| **メモリ** | 4GB以上推奨 |
| **ストレージ** | 100MB以上の空き容量 |

## 📦 インストール方法

### Option 1: インストーラー版（推奨）
1. [Releases](https://github.com/tomo551/PDF-con/releases)から最新の`PDFCon-Setup.exe`をダウンロード
2. ダウンロードしたファイルを実行
3. インストールウィザードの指示に従って進める

## 🚀 使い方

### 基本操作
1. **PDFConを起動**
2. **PDFファイルをドラッグ&ドロップ** - メイン画面にPDFファイルをドロップ
3. **設定を調整**:
   - 出力形式を選択（JPEG/PNG/TIFF）
   - カラーモードを選択
   - 解像度をスライダーで調整
4. **保存ボタンをクリック** - 元のPDFと同じフォルダに画像が保存されます

### 便利な機能

#### A4プリセット
- **A4縦/横 中品質**: 200 DPI相当（ファイルサイズ小）
- **A4縦/横 高品質**: 350 DPI相当（印刷品質）

#### 解像度ガイド
| 用途 | 推奨DPI | 説明 |
|------|---------|------|
| Web表示 | 300-600 | 画面表示に最適 |
| 文書印刷 | 600-1200 | 一般的な印刷品質 |
| 高品質印刷 | 1200-2400 | 商用印刷レベル |
| アーカイブ | 2400-5000 | 長期保存・拡大対応 |

## ⚙️ 設定

アプリケーションは以下の設定を自動保存します：
- 最後に使用した出力形式
- カラーモード設定
- DPI設定

## 🛠️ ビルド方法

開発者向け情報:

```bash
# リポジトリをクローン
git clone https://github.com/tomo551/PDF-con.git
cd PDF-con

# プロジェクトをビルド
dotnet build --configuration Release

# 実行可能ファイルを作成
dotnet publish -c Release -r win-x64 --self-contained true
```

### 開発環境
- Visual Studio 2022 または Visual Studio Code
- .NET 9.0 SDK
- Windows 10 SDK

## 🤝 貢献方法

プロジェクトへの貢献を歓迎します！

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

### 報告・要望
- [Issues](https://github.com/tomo551/PDF-con/issues)でバグ報告や機能要望をお願いします
- 質問やサポートも同様にIssuesをご利用ください

## 📋 既知の制限事項

- パスワード保護されたPDFファイルには対応していません
- 非常に大きなPDFファイル（100MB以上）では処理が遅くなる場合があります
- 一部の特殊なPDF形式では正しく変換されない場合があります

## 🗺️ ロードマップ

- [ ] パスワード保護PDF対応
- [ ] バッチ処理の進捗表示改善
- [ ] OCR機能の追加
- [ ] 他の画像形式対応（WebP、AVIF）
- [ ] ダークモード対応

## 📄 ライセンス

このプロジェクトは[MITライセンス](LICENSE)の下で公開されています。

## 👨‍💻 作者

**IgaSystems**
- GitHub: [@tomo551](https://github.com/tomo551)

## 🙏 謝辞

- Microsoft Windows Runtime PDF API
- .NET WPF Framework

## 📞 サポート

- 🐛 バグ報告: [Issues](https://github.com/tomo551/PDF-con/issues)
- 💡 機能要望: [Issues](https://github.com/tomo551/PDF-con/issues)
- 📧 その他のお問い合わせ: [適切な連絡先]

---

⭐ このプロジェクトが役に立った場合は、ぜひスターをお願いします！
