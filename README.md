# Aloe.Utils.SafeIO

[![NuGet Version](https://img.shields.io/nuget/v/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![License](https://img.shields.io/github/license/ted-sharp/aloe-utils-safeio.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

`Aloe.Utils.SafeIO` は、ファイル操作を安全に行うための軽量で使いやすいライブラリです。
主にファイルの読み書きや操作が必要なアプリケーションで使用できます。

## 主な機能

* ファイルの安全な読み書き
* ファイル操作の例外処理
* ファイルの存在確認
* ディレクトリ操作
* パス操作
* ファイルロック処理
* 非同期操作のサポート

## 対応環境

* .NET 9 以降

## インストール

NuGet パッケージマネージャーからインストール：

```cmd
Install-Package Aloe.Utils.SafeIO
```

または、.NET CLI で：

```cmd
dotnet add package Aloe.Utils.SafeIO
```

## 使用例

```csharp
using Aloe.Utils.SafeIO;

// ファイルの読み込み
string content = await SafeFile.ReadAllTextAsync("example.txt");

// ファイルの書き込み
await SafeFile.WriteAllTextAsync("example.txt", "Hello, World!");

// ファイルの存在確認
bool exists = SafeFile.Exists("example.txt");

// ディレクトリの作成
SafeDirectory.CreateDirectory("newfolder");

// ファイルのコピー
SafeFile.Copy("source.txt", "destination.txt");
```

## 機能詳細

### 安全なファイル操作

* ファイルの読み書き時の例外処理
* ファイルロックの適切な処理
* 非同期操作のサポート
* パスの正規化と検証

### パフォーマンス

* 効率的なファイル操作
* メモリ使用量の最適化
* スレッドセーフな設計

## License

MIT License

## Contributing

Please report bugs and feature requests through GitHub Issues. Pull requests are welcome.
