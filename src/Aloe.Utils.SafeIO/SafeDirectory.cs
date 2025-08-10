// <copyright file="SafeDirectory.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

using System.Diagnostics;

namespace Aloe.Utils.SafeIO;

/// <summary>
/// ディレクトリの安全な操作を提供する静的クラス
/// </summary>
public static class SafeDirectory
{
    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します（ミリ秒指定オーバーロード・同期版）。
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeoutMs">削除完了を待機する最大時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">再試行間隔（ミリ秒）</param>
    /// <exception cref="ArgumentOutOfRangeException">timeoutMs または retryIntervalMs が負、もしくは timeoutMs &lt; retryIntervalMs の場合にスロー</exception>
    public static void Delete(string path, int timeoutMs, int retryIntervalMs)
    {
        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        }

        if (retryIntervalMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryIntervalMs));
        }

        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var retry = TimeSpan.FromMilliseconds(retryIntervalMs);
        Delete(path, timeout, retry);
    }

    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します（同期版、最大リトライ回数指定）。
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeout">削除完了を待機する最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <param name="maxRetries">最大リトライ回数（0 は再試行なし、負値は不可）</param>
    /// <exception cref="ArgumentOutOfRangeException">timeout &lt; retryInterval、または maxRetries &lt; 0</exception>
    /// <exception cref="TimeoutException">終了条件に達した場合</exception>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval, int maxRetries)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        }

        // timeout < retryInterval の検証
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();
        var attempt = 0;

        // ディレクトリが消えるまで、あるいはタイムアウト/リトライ上限に達するまで繰り返し
        while (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // 無視して再試行
            }
            catch (UnauthorizedAccessException)
            {
                // 無視して再試行
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            // タイムアウト／リトライ上限判定
            if (sw.Elapsed >= timeout || attempt >= maxRetries)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除待機が終了条件に達しました（timeout={timeout}, retries={attempt}/{maxRetries}）。");
            }

            attempt++;
            Thread.Sleep(retryInterval);
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します。（同期版）
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeout">削除完了を待機する最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <exception cref="ArgumentOutOfRangeException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">タイムアウト時にスローされます</exception>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval)
    {
        // timeout < retryInterval の検証
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();

        // ディレクトリが消えるまで、あるいはタイムアウトするまで繰り返し
        while (Directory.Exists(path))
        {
            try
            {
                // 再帰削除を試みる
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // ファイルロック等の例外は無視して再試行
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス拒否も一旦無視して再試行
            }

            if (sw.Elapsed >= timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除待機がタイムアウトしました（{timeout}）。");
            }

            Thread.Sleep(retryInterval);
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します。（非同期版）
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeout">削除完了を待機する最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>削除操作の完了を示す Task。</returns>
    /// <exception cref="ArgumentException">timeout が retryInterval より短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">タイムアウト時にスローされます</exception>
    public static async Task DeleteAsync(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default)
    {
        // バリデーション
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();

        using var timer = new PeriodicTimer(retryInterval);

        // ディレクトリが消えるまでループ
        while (Directory.Exists(path))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // 再帰的削除を試み
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // ファイルロック等で失敗したら無視して再試行
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス拒否も無視
            }

            // 削除に成功してパスがなくなったら終了
            if (!Directory.Exists(path))
            {
                return;
            }

            // タイムアウトチェック
            if (sw.Elapsed >= timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除完了待機がタイムアウトしました（{timeout}）。");
            }

            // 次のリトライまで待機
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。", ct);
            }
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に安全にコピーします（同期版）。ファイルは
    /// <see cref="SafeFile.Copy(string, string, bool, System.TimeSpan, System.TimeSpan, Aloe.Utils.SafeIO.ISafeRetryPolicy?)"/>
    /// を用いて安全にコピーします。
    /// </summary>
    /// <param name="sourceDirectory">コピー元ディレクトリ</param>
    /// <param name="destinationDirectory">コピー先ディレクトリ</param>
    /// <param name="overwrite">既存ファイルを上書きするか</param>
    /// <param name="timeout">各ファイルコピーのタイムアウト</param>
    /// <param name="retryInterval">各ファイルコピーの再試行間隔</param>
    /// <param name="policy">再試行ポリシー（省略可）。未指定時は内部ループで再試行します。</param>
    /// <remarks>戻り値はありません。</remarks>
    public static void Copy(
        string sourceDirectory,
        string destinationDirectory,
        bool overwrite,
        TimeSpan timeout,
        TimeSpan retryInterval,
        ISafeRetryPolicy? policy = null)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirectory}");
        }

        Directory.CreateDirectory(destinationDirectory);

        // サブディレクトリを事前に作成
        foreach (var dir in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, dir);
            var destDir = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(destDir);
        }

        // ファイルをコピー（SafeFile.Copy を利用）
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destFile = Path.Combine(destinationDirectory, relative);
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            SafeFile.Copy(file, destFile, overwrite, timeout, retryInterval, policy);
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に安全にコピーします（ミリ秒指定・同期版）。
    /// </summary>
    /// <param name="sourceDirectory">コピー元ディレクトリ</param>
    /// <param name="destinationDirectory">コピー先ディレクトリ</param>
    /// <param name="overwrite">既存ファイルを上書きするか</param>
    /// <param name="timeoutMs">各ファイルコピーのタイムアウト（ミリ秒）</param>
    /// <param name="retryIntervalMs">各ファイルコピーの再試行間隔（ミリ秒）</param>
    public static void Copy(
        string sourceDirectory,
        string destinationDirectory,
        bool overwrite,
        int timeoutMs,
        int retryIntervalMs)
    {
        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        }

        if (retryIntervalMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryIntervalMs));
        }
        Copy(sourceDirectory, destinationDirectory, overwrite, TimeSpan.FromMilliseconds(timeoutMs), TimeSpan.FromMilliseconds(retryIntervalMs));
    }

    /// <summary>
    /// ディレクトリを再帰的に安全にコピーします（非同期版）。ファイルは
    /// <see cref="SafeFile.CopyAsync(string, string, bool, System.TimeSpan, System.TimeSpan, System.Threading.CancellationToken, Aloe.Utils.SafeIO.ISafeRetryPolicy?)"/>
    /// を用います。
    /// </summary>
    /// <param name="sourceDirectory">コピー元ディレクトリ</param>
    /// <param name="destinationDirectory">コピー先ディレクトリ</param>
    /// <param name="overwrite">既存ファイルを上書きするか</param>
    /// <param name="timeout">各ファイルコピーのタイムアウト</param>
    /// <param name="retryInterval">各ファイルコピーの再試行間隔</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <param name="policy">再試行ポリシー（省略可）。未指定時は内部ループで再試行します。</param>
    /// <returns>処理の完了を示す Task。</returns>
    public static async Task CopyAsync(
        string sourceDirectory,
        string destinationDirectory,
        bool overwrite,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default,
        ISafeRetryPolicy? policy = null)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"コピー元ディレクトリが見つかりません: {sourceDirectory}");
        }

        Directory.CreateDirectory(destinationDirectory);

        foreach (var dir in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, dir);
            var destDir = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destFile = Path.Combine(destinationDirectory, relative);
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await SafeFile.CopyAsync(file, destFile, overwrite, timeout, retryInterval, ct, policy);
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に安全にコピーします（ミリ秒指定・非同期版）。
    /// </summary>
    /// <param name="sourceDirectory">コピー元ディレクトリ</param>
    /// <param name="destinationDirectory">コピー先ディレクトリ</param>
    /// <param name="overwrite">既存ファイルを上書きするか</param>
    /// <param name="timeoutMs">各ファイルコピーのタイムアウト（ミリ秒）</param>
    /// <param name="retryIntervalMs">各ファイルコピーの再試行間隔（ミリ秒）</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>処理の完了を示す Task。</returns>
    public static Task CopyAsync(
        string sourceDirectory,
        string destinationDirectory,
        bool overwrite,
        int timeoutMs,
        int retryIntervalMs,
        CancellationToken ct = default)
    {
        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        }

        if (retryIntervalMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryIntervalMs));
        }
        return CopyAsync(sourceDirectory, destinationDirectory, overwrite, TimeSpan.FromMilliseconds(timeoutMs), TimeSpan.FromMilliseconds(retryIntervalMs), ct);
    }

    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します（非同期・ミリ秒指定）。
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeoutMs">削除完了を待機する最大時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">再試行間隔（ミリ秒）</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>削除操作の完了を示す Task。</returns>
    public static Task DeleteAsync(string path, int timeoutMs, int retryIntervalMs, CancellationToken ct = default)
    {
        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        }

        if (retryIntervalMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryIntervalMs));
        }

        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var retry = TimeSpan.FromMilliseconds(retryIntervalMs);
        return DeleteAsync(path, timeout, retry, ct);
    }

    /// <summary>
    /// ディレクトリを再帰的に削除し、完全に消失するまで待機します（非同期版、最大リトライ回数指定）。
    /// </summary>
    /// <param name="path">削除するディレクトリのパス</param>
    /// <param name="timeout">削除完了を待機する最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <param name="maxRetries">最大リトライ回数（0 は再試行なし、負値は不可）</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>削除操作の完了を示す Task。</returns>
    public static async Task DeleteAsync(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval,
        int maxRetries,
        CancellationToken ct = default)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        }

        // バリデーション
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(retryInterval);
        var attempt = 0;

        // ディレクトリが消えるまでループ
        while (Directory.Exists(path))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // ファイルロック等で失敗したら無視
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス拒否も無視
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            // タイムアウト／リトライ上限判定
            if (sw.Elapsed >= timeout || attempt >= maxRetries)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除完了待機が終了条件に達しました（timeout={timeout}, retries={attempt}/{maxRetries}）。");
            }

            // 次のリトライまで待機
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。", ct);
            }

            attempt++;
        }
    }
}
