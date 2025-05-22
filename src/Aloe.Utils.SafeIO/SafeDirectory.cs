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
    /// <returns>削除操作が完了したことを示すTask</returns>
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
}
