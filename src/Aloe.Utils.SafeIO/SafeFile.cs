using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aloe.Utils.SafeIO;

/// <summary>
/// ファイルの安全な操作を提供する静的クラス
/// </summary>
public static class SafeFile
{
    /// <summary>
    /// ファイルを安全に削除します。削除が完了するまで待機します。
    /// </summary>
    /// <param name="path">削除するファイルのパス</param>
    /// <param name="timeout">タイムアウト時間</param>
    /// <param name="retryInterval">再試行の間隔</param>
    /// <exception cref="ArgumentException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">指定されたタイムアウト時間内に削除が完了しない場合にスローされます</exception>
    /// <remarks>
    /// ファイルの存在確認にFile.Existsではなく排他アクセスでのFileStreamオープンを試みる理由：
    /// 1. File.Existsはファイルの存在確認のみで、他のプロセスによるアクセス状態を確認できない
    /// 2. ファイルが削除中（delete-pending）の状態の場合、File.Existsはfalseを返すが、実際にはまだ削除が完了していない
    /// 3. 排他アクセスでのオープン試行により、ファイルが完全に解放され、削除可能な状態になったことを確実に検知できる
    /// </remarks>
    public static void Delete(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        // ファイルの削除を試行
        File.Delete(path);

        var sw = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                // ファイルが完全に削除されたか確認するため、排他アクセスで開けるか試行
                // File.Existsではなく排他アクセスを使用することで、ファイルが完全に解放されたことを確実に検知
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (FileNotFoundException)
            {
                // ファイルが見つからない場合は削除完了
                return;
            }
            catch (IOException)
            {
                // ファイルが開けない場合（削除中または既に削除済み）も完了と見做す
                return;
            }

            // タイムアウトチェック
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeFile)}] ファイル「{path}」の削除完了を待機中にタイムアウトしました（{timeout}）。");
            }

            // 次の試行まで待機
            Thread.Sleep(retryInterval);
        }
    }

    /// <summary>
    /// ファイルを安全に非同期で削除します。削除が完了するまで待機します。
    /// </summary>
    /// <param name="path">削除するファイルのパス</param>
    /// <param name="timeout">タイムアウト時間</param>
    /// <param name="retryInterval">再試行の間隔</param>
    /// <param name="ct">キャンセレーショントークン</param>
    /// <exception cref="ArgumentException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="OperationCanceledException">操作がキャンセルされた場合にスローされます</exception>
    /// <exception cref="TimeoutException">指定されたタイムアウト時間内に削除が完了しない場合にスローされます</exception>
    /// <remarks>
    /// ファイルの存在確認にFile.Existsではなく排他アクセスでのFileStreamオープンを試みる理由：
    /// 1. File.Existsはファイルの存在確認のみで、他のプロセスによるアクセス状態を確認できない
    /// 2. ファイルが削除中（delete-pending）の状態の場合、File.Existsはfalseを返すが、実際にはまだ削除が完了していない
    /// 3. 排他アクセスでのオープン試行により、ファイルが完全に解放され、削除可能な状態になったことを確実に検知できる
    /// </remarks>
    public static async Task DeleteAsync(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        // ファイルの削除を試行
        File.Delete(path);

        var sw = Stopwatch.StartNew();
        // 定期的な再試行のためのタイマーを生成
        using var timer = new PeriodicTimer(retryInterval);

        while (true)
        {
            // キャンセル要求のチェック
            ct.ThrowIfCancellationRequested();

            try
            {
                // ファイルが完全に削除されたか確認するため、排他アクセスで開けるか試行
                // File.Existsではなく排他アクセスを使用することで、ファイルが完全に解放されたことを確実に検知
                await using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
                // ファイルが開けた場合はまだ存在するため、次のループへ
            }
            catch (FileNotFoundException)
            {
                // ファイルが見つからない場合は削除完了
                return;
            }
            catch (IOException)
            {
                // ファイルが開けない場合（削除中または既に削除済み）も完了と見做す
                return;
            }

            // タイムアウトチェック
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException(
                    $"[{nameof(SafeFile)}] ファイル「{path}」の削除完了を待機中にタイムアウトしました（{timeout}）。");
            }

            // 次のタイマーまで待機
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。");
            }
        }
    }
}
