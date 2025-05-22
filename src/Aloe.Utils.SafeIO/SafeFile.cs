// <copyright file="SafeFile.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

using System.Diagnostics;

namespace Aloe.Utils.SafeIO;

/// <summary>
/// ファイル操作における競合状態やロック状態を安全に処理するための静的ユーティリティクラス。
/// ファイルの削除操作において、他のプロセスによるアクセスやロック状態を考慮し、
/// 確実な削除完了を保証します。
/// </summary>
public static class SafeFile
{
    /// <summary>
    /// 指定されたファイルを安全に削除します。削除が完了するまで同期的に待機します。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeout">削除操作の最大待機時間</param>
    /// <param name="retryInterval">削除失敗時の再試行間隔</param>
    /// <exception cref="ArgumentOutOfRangeException">timeoutがretryIntervalより短い場合にスローされます</exception>
    /// <exception cref="TimeoutException">指定されたタイムアウト時間内に削除が完了しない場合にスローされます</exception>
    /// <remarks>
    /// ファイルの存在確認にFile.Existsではなく排他アクセスでのFileStreamオープンを試みる理由：
    /// 1. File.Existsはファイルの存在確認のみで、他のプロセスによるアクセス状態を確認できない
    /// 2. ファイルが削除中（delete-pending）の状態の場合、File.Existsはfalseを返すが、実際にはまだ削除が完了していない
    /// 3. 排他アクセスでのオープン試行により、ファイルが完全に解放され、削除可能な状態になったことを確実に検知できる
    /// </remarks>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval)
    {
        // バリデーション
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();

        while (true)
        {
            // ── 1) 削除を試みる ──
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // ロック中などで失敗したら無視
            }
            catch (UnauthorizedAccessException)
            {
                // ロック中などで失敗したら無視
            }

            // ── 2) 完全に消えたか排他オープンで確認 ──
            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);

                // ここまで来たらファイルが残っていてオープンにも成功 ⇒ まだ削除されていない
            }
            catch (FileNotFoundException)
            {
                // ファイルが無くなった ⇒ 削除完了
                return;
            }
            catch (IOException)
            {
                // ロック中 or 削除進行中 ⇒ 再試行
            }

            // ── 3) タイムアウト判定 ──
            if (sw.Elapsed >= timeout)
            {
                throw new TimeoutException(
                    $"[SafeFile] ファイル「{path}」の削除待機がタイムアウトしました（{timeout} 経過）。");
            }

            // ── 4) 次の再試行まで待機 ──
            Thread.Sleep(retryInterval);
        }
    }

    /// <summary>
    /// 指定されたファイルを安全に非同期で削除します。削除が完了するまで非同期的に待機します。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeout">削除操作の最大待機時間</param>
    /// <param name="retryInterval">削除失敗時の再試行間隔</param>
    /// <param name="ct">操作のキャンセルを要求するためのトークン</param>
    /// <returns>削除操作が完了したことを示すTask</returns>
    /// <exception cref="ArgumentOutOfRangeException">timeoutがretryIntervalより短い場合にスローされます</exception>
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
        // timeout < retryInterval のチェック
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();

        using var timer = new PeriodicTimer(retryInterval);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // ── 1) 削除を試みる ──
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // ロック中などで失敗したら無視
            }
            catch (UnauthorizedAccessException)
            {
                // ロック中などで失敗したら無視
            }

            // ── 2) 完全に消えたか排他オープンで確認 ──
            try
            {
                await using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);

                // ここまで来たらファイルが残っていてオープンに成功 ⇒ まだ削除されていない
            }
            catch (FileNotFoundException)
            {
                // ファイルが無くなった ⇒ 削除完了
                return;
            }
            catch (IOException)
            {
                // ロック中 or 削除進行中 ⇒ 再試行
            }

            // ── 3) タイムアウト判定 ──
            if (sw.Elapsed >= timeout)
            {
                throw new TimeoutException(
                    $"[SafeFile] ファイル「{path}」の削除完了待機がタイムアウトしました（{timeout} 経過）。");
            }

            // ── 4) 次の再試行まで待機 ──
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。", ct);
            }
        }
    }
}
