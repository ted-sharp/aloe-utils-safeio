using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// <exception cref="ArgumentException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">タイムアウト時にスローされます</exception>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        // 削除リクエスト（再帰的に全内容を削除）
        Directory.Delete(path, recursive: true);

        var sw = Stopwatch.StartNew();
        while (Directory.Exists(path))
        {
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除完了待機がタイムアウトしました（{timeout}）。");
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
    /// <exception cref="ArgumentException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">タイムアウト時にスローされます</exception>
    public static async Task DeleteAsync(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        Directory.Delete(path, recursive: true);

        var sw = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(retryInterval);

        while (Directory.Exists(path))
        {
            ct.ThrowIfCancellationRequested();

            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeDirectory)}] ディレクトリ「{path}」の削除完了待機がタイムアウトしました（{timeout}）。");
            }

            // 次のタイマーまで待機
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。");
            }
        }
    }
}
