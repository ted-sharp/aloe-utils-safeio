# Aloe.Utils.SafeIO

[![English](https://img.shields.io/badge/Language-English-blue)](./README.md)
[![日本語](https://img.shields.io/badge/言語-日本語-blue)](./README.ja.md)

[![NuGet Version](https://img.shields.io/nuget/v/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![License](https://img.shields.io/github/license/ted-sharp/aloe-utils-safeio.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

`Aloe.Utils.SafeIO` は、ファイル操作を安全に行うための軽量で使いやすいライブラリです。
主にファイルの読み書きや操作が必要なアプリケーションで使用できます。

## 主な機能

* ファイルの安全な削除
  * 同期・非同期の両方に対応
  * タイムアウトとリトライ間隔の設定
  * ファイルロック状態の適切な処理
* ディレクトリの安全な削除
  * 再帰的な削除をサポート
  * 同期・非同期の両方に対応
  * タイムアウトとリトライ間隔の設定
* キャンセレーショントークンによる操作の中断
* 例外処理の最適化

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

### ファイルの安全な削除

```csharp
using Aloe.Utils.SafeIO;

// 同期版
SafeFile.Delete(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// 非同期版
await SafeFile.DeleteAsync(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

### ディレクトリの安全な削除

```csharp
using Aloe.Utils.SafeIO;

// 同期版
SafeDirectory.Delete(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// 非同期版
await SafeDirectory.DeleteAsync(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

## License

MIT License

## Contributing

Please report bugs and feature requests through GitHub Issues. Pull requests are welcome.
