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
  * タイムアウトとリトライ間隔の設定（ミリ秒オーバーロードも用意）
  * 最大リトライ回数の指定オーバーロード
  * ロック状態や delete-pending を考慮した排他オープン確認
* ディレクトリの安全な削除
  * 再帰的な削除をサポート
  * 同期・非同期の両方に対応
  * タイムアウトとリトライ間隔の設定（ミリ秒オーバーロードも用意）
  * 最大リトライ回数の指定オーバーロード
* ファイルの安全なコピー
  * いったん `.tmp` へコピーしてから原子的に置換/移動
  * ReadOnly 属性の解除に対応（CD 等からのコピーを想定）
  * 同期・非同期 API とリトライ/タイムアウト
* SafePath ユーティリティ
  * `SafePath.Combine(...)` で柔軟なパス結合（null/空/空白をスキップ）
  * `SafePath.WebCombine(...)` で URL 風の結合（重複スラッシュ除去、末尾のクエリ/フラグメント維持）
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

// ミリ秒オーバーロード
SafeFile.Delete(
    "example.txt",
    timeoutMs: 30_000,
    retryIntervalMs: 100
);

// 最大リトライ指定（タイムアウト/回数のいずれかに到達で打ち切り）
SafeFile.Delete(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    maxRetries: 100
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

// ミリ秒オーバーロード
SafeDirectory.Delete(
    "example-directory",
    timeoutMs: 30_000,
    retryIntervalMs: 100
);

// 最大リトライ指定
SafeDirectory.Delete(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    maxRetries: 100
);

### ファイルの安全なコピー

```csharp
using Aloe.Utils.SafeIO;

// 同期
SafeFile.Copy(
    source: "source.txt",
    destination: "dest.txt",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// 非同期
await SafeFile.CopyAsync(
    source: "source.txt",
    destination: "dest.txt",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

### ディレクトリの安全なコピー

```csharp
using Aloe.Utils.SafeIO;

// 同期（再帰コピー）
SafeDirectory.Copy(
    sourceDirectory: "srcDir",
    destinationDirectory: "dstDir",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// 非同期
await SafeDirectory.CopyAsync(
    sourceDirectory: "srcDir",
    destinationDirectory: "dstDir",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

### SafePath ユーティリティ

```csharp
using Aloe.Utils.SafeIO;

// 柔軟な FS パス結合（null/空/空白をスキップ）
var fsPath = SafePath.Combine(null, "  ", "folder", "sub", "file.txt");

// URL 風結合（重複スラッシュ除去、末尾のクエリ/フラグメント維持）
var url = SafePath.WebCombine("https://example.com/", "/api/", "v1//", "items?id=1#top");
```

## 任意: Polly によるリトライ戦略の注入

このライブラリに Polly を依存追加することなく、`ISafeRetryPolicy` を実装して注入できます。

```csharp
using Aloe.Utils.SafeIO;
using Polly;
using Polly.Retry;

sealed class PollyRetryPolicy : ISafeRetryPolicy
{
  private readonly RetryPolicy _sync;
  private readonly AsyncRetryPolicy _async;

  public PollyRetryPolicy(RetryPolicy sync, AsyncRetryPolicy async)
  {
    _sync = sync; _async = async;
  }

  public bool Execute(Func<bool> attempt)
    => _sync.Execute(attempt);

  public Task<bool> ExecuteAsync(Func<CancellationToken, Task<bool>> attempt, CancellationToken ct)
    => _async.ExecuteAsync((_, token) => attempt(token), new Context(), ct);
}

var policy = new PollyRetryPolicy(
  Policy
    .HandleResult(false)
    .Or<IOException>()
    .Or<UnauthorizedAccessException>()
    .WaitAndRetry(5, i => TimeSpan.FromMilliseconds(100)),
  Policy
    .HandleResult(false)
    .Or<IOException>()
    .Or<UnauthorizedAccessException>()
    .WaitAndRetryAsync(5, i => TimeSpan.FromMilliseconds(100))
);

// SafeFile.Copy で利用（policy は省略可能）
SafeFile.Copy("source.txt", "dest.txt", overwrite: true,
              timeout: TimeSpan.FromSeconds(30), retryInterval: TimeSpan.FromMilliseconds(100),
              policy: policy);
```
```

## License

MIT License

## Contributing

Please report bugs and feature requests through GitHub Issues. Pull requests are welcome.
