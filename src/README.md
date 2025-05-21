# Aloe.Utils.SafeIO

日本語文字列をローマ字に変換するための軽量で使いやすいライブラリです。
主に日本語のテキスト処理や国際化対応が必要なアプリケーションで使用できます。

## 主な機能

* ひらがな・カタカナ文字列をヘボン式ローマ字に変換
* 基本的な文字（あいうえお等）の変換
* 濁音・半濁音（がぎぐげご等）の変換
* 拗音（きゃきゅきょ等）の変換
* 促音（っ）の適切な処理
* 長音記号（ー）の適切な処理
* 小文字（ぁぃぅぇぉ等）の変換
* パフォーマンスに最適化された実装（ValueStringBuilder使用）

## 対応環境

* .NET 9 以降

## 使用例

```csharp
using Aloe.Utils.SafeIO;

// 基本的な使用例
string result = Romanizer.Convert("こんにちは"); // "konnichiha"

// カタカナの変換
string katakana = Romanizer.Convert("コンニチハ"); // "konnichiha"

// 濁音の変換
string dakuon = Romanizer.Convert("がぎぐげご"); // "gagigugego"

// 拗音の変換
string youon = Romanizer.Convert("きゃきゅきょ"); // "kyakyukyo"

// 促音の変換
string sokuon = Romanizer.Convert("っか"); // "kka"

// 長音の変換
string chouon = Romanizer.Convert("あー"); // "aa"
```

## ライセンス

MIT License
