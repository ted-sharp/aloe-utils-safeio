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
    /// ファイルを安全にコピーします。.tmp にコピーしてから原子的に置換/移動します（同期版）。
    /// </summary>
    /// <param name="source">コピー元ファイル</param>
    /// <param name="destination">コピー先ファイル</param>
    /// <param name="overwrite">既存の宛先を上書きするか</param>
    /// <param name="timeout">完了待機の最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <param name="policy">外部から注入された再試行ポリシー（未指定時は内部ループ）。</param>
    /// <exception cref="ArgumentOutOfRangeException">timeout が retryInterval より短い場合</exception>
    /// <exception cref="TimeoutException">完了前に終了条件に達した場合</exception>
    public static void Copy(
        string source,
        string destination,
        bool overwrite,
        TimeSpan timeout,
        TimeSpan retryInterval,
        ISafeRetryPolicy? policy = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();
        var destDir = Path.GetDirectoryName(Path.GetFullPath(destination));
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // 上書き不可かつ宛先が存在するなら即時失敗
        if (!overwrite && File.Exists(destination))
        {
            throw new IOException($"コピー先が既に存在します: {destination}");
        }

        var tmp = destination + ".tmp";

        if (policy is not null)
        {
            var ok = policy.Execute(() =>
            {
                try
                {
                    try
                    {
                        File.Delete(tmp);
                    }
                    catch
                    {
                    }

                    File.Copy(source, tmp, overwrite: true);
                    SafeFileHelpers.RemoveReadOnlyIfPresent(tmp);
                    SafeFileHelpers.RemoveReadOnlyIfPresent(destination);

                    if (SafeFileHelpers.IsSameVolume(tmp, destination) && File.Exists(destination))
                    {
                        File.Replace(tmp, destination, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tmp, destination, overwrite: overwrite);
                    }

                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            });

            if (!ok)
            {
                throw new TimeoutException($"[SafeFile] ファイルコピー 「{source}」→「{destination}」がポリシーにより完了しませんでした。");
            }

            return;
        }

        while (true)
        {
                try
                {
                    // 前回の残骸を掃除
                    try
                    {
                        File.Delete(tmp);
                    }
                    catch
                    {
                    }

                    // .tmp へコピー（常に上書き）
                    File.Copy(source, tmp, overwrite: true);

                    // CD 等からの読み取りで ReadOnly が付与されるケースを想定し解除
                    SafeFileHelpers.RemoveReadOnlyIfPresent(tmp);

                    // 宛先に ReadOnly が付いていれば解除
                    SafeFileHelpers.RemoveReadOnlyIfPresent(destination);

                    // 同一ボリュームなら原子的置換、異なるなら Move(overwrite)
                    if (SafeFileHelpers.IsSameVolume(tmp, destination) && File.Exists(destination))
                    {
                        // ignoreMetadataErrors: true で付帯メタデータ差異を無視
                        File.Replace(tmp, destination, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tmp, destination, overwrite: overwrite);
                    }

                    return;
                }
                catch (IOException)
                {
                    // 再試行
                }
                catch (UnauthorizedAccessException)
                {
                    // 再試行
                }

                if (sw.Elapsed >= timeout)
                {
                    throw new TimeoutException(
                        $"[SafeFile] ファイルコピー 「{source}」→「{destination}」がタイムアウトしました（{timeout}）。");
                }

                Thread.Sleep(retryInterval);
            }
        }

    /// <summary>
    /// ファイルを安全にコピーします（ミリ秒指定・同期版）。
    /// </summary>
    /// <param name="source">コピー元ファイル</param>
    /// <param name="destination">コピー先ファイル</param>
    /// <param name="overwrite">既存の宛先を上書きするか</param>
    /// <param name="timeoutMs">完了待機の最大時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">再試行間隔（ミリ秒）</param>
    public static void Copy(string source, string destination, bool overwrite, int timeoutMs, int retryIntervalMs)
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
        Copy(source, destination, overwrite, timeout, retry);
    }

    /// <summary>
    /// ファイルを安全にコピーします。.tmp にコピーしてから原子的に置換/移動します（非同期版）。
    /// </summary>
    /// <param name="source">コピー元ファイル</param>
    /// <param name="destination">コピー先ファイル</param>
    /// <param name="overwrite">既存の宛先を上書きするか</param>
    /// <param name="timeout">完了待機の最大時間</param>
    /// <param name="retryInterval">再試行間隔</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <param name="policy">外部から注入された再試行ポリシー（未指定時は内部ループ）。</param>
    /// <returns>完了を示す Task</returns>
    public static async Task CopyAsync(
        string source,
        string destination,
        bool overwrite,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default,
        ISafeRetryPolicy? policy = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();
        var destDir = Path.GetDirectoryName(Path.GetFullPath(destination));
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        if (!overwrite && File.Exists(destination))
        {
            throw new IOException($"コピー先が既に存在します: {destination}");
        }

        var tmp = destination + ".tmp";
        using var timer = new PeriodicTimer(retryInterval);

        if (policy is not null)
        {
            var ok = await policy.ExecuteAsync(
                async ctk =>
                {
                    ctk.ThrowIfCancellationRequested();

                    try
                    {
                    try
                    {
                        File.Delete(tmp);
                    }
                    catch
                    {
                    }

                    await using (var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                    await using (var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await src.CopyToAsync(dst, ctk);
                    }

                    SafeFileHelpers.RemoveReadOnlyIfPresent(tmp);
                    SafeFileHelpers.RemoveReadOnlyIfPresent(destination);

                    if (SafeFileHelpers.IsSameVolume(tmp, destination) && File.Exists(destination))
                    {
                        File.Replace(
                            tmp,
                            destination,
                            destinationBackupFileName: null,
                            ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(
                            tmp,
                            destination,
                            overwrite: overwrite);
                    }

                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                },
                ct);

            if (!ok)
            {
                throw new TimeoutException($"[SafeFile] ファイルコピー（非同期）「{source}」→「{destination}」がポリシーにより完了しませんでした。");
            }

            return;
        }
        else
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    try
                    {
                        File.Delete(tmp);
                    }
                    catch
                    {
                    }

                    // 非同期コピー
                    await using (var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                    await using (var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await src.CopyToAsync(dst, ct);
                    }

                    SafeFileHelpers.RemoveReadOnlyIfPresent(tmp);
                    SafeFileHelpers.RemoveReadOnlyIfPresent(destination);

                    if (SafeFileHelpers.IsSameVolume(tmp, destination) && File.Exists(destination))
                    {
                        File.Replace(tmp, destination, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tmp, destination, overwrite: overwrite);
                    }

                    return;
                }
                catch (IOException)
                {
                    // 再試行
                }
                catch (UnauthorizedAccessException)
                {
                    // 再試行
                }

                if (sw.Elapsed >= timeout)
                {
                    throw new TimeoutException(
                        $"[SafeFile] ファイルコピー（非同期）「{source}」→「{destination}」がタイムアウトしました（{timeout}）。");
                }

                if (!await timer.WaitForNextTickAsync(ct))
                {
                    throw new OperationCanceledException("タイマー待機中にキャンセルされました。", ct);
                }
            }
        }
    }

    /// <summary>
    /// ファイルを安全にコピーします（ミリ秒指定・非同期版）。
    /// </summary>
    /// <param name="source">コピー元ファイル</param>
    /// <param name="destination">コピー先ファイル</param>
    /// <param name="overwrite">既存の宛先を上書きするか</param>
    /// <param name="timeoutMs">完了待機の最大時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">再試行間隔（ミリ秒）</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>完了を示す Task</returns>
    public static Task CopyAsync(
        string source,
        string destination,
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
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var retry = TimeSpan.FromMilliseconds(retryIntervalMs);
        return CopyAsync(source, destination, overwrite, timeout, retry, ct);
    }

    /// <summary>
    /// 指定されたファイルを安全に削除します。ミリ秒指定のオーバーロードです。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeoutMs">削除操作の最大待機時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">削除失敗時の再試行間隔（ミリ秒）</param>
    /// <exception cref="ArgumentOutOfRangeException">timeoutMs が retryIntervalMs より短い、または負値の場合にスローされます</exception>
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
    /// 指定されたファイルを安全に削除します。削除が完了するまで同期的に待機します。
    /// 最大リトライ回数を指定できるオーバーロードです。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeout">削除操作の最大待機時間</param>
    /// <param name="retryInterval">削除失敗時の再試行間隔</param>
    /// <param name="maxRetries">リトライの最大回数（0 は再試行なし、負値は不可）</param>
    /// <exception cref="ArgumentOutOfRangeException">timeout が retryInterval より短い、または maxRetries が負の場合にスローされます</exception>
    /// <exception cref="TimeoutException">指定された条件内に削除が完了しない場合にスローされます</exception>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval, int maxRetries)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        }

        // ベースの検証を流用
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();
        var attempt = 0;

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

            // ── 3) タイムアウト／リトライ上限判定 ──
            if (sw.Elapsed >= timeout || attempt >= maxRetries)
            {
                throw new TimeoutException(
                    $"[SafeFile] ファイル「{path}」の削除待機が終了条件に達しました（timeout={timeout}, retries={attempt}/{maxRetries}）。");
            }

            attempt++;

            // ── 4) 次の再試行まで待機 ──
            Thread.Sleep(retryInterval);
        }
    }

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
    /// <param name="policy">外部から注入された再試行ポリシー（未指定時は内部ループ）。</param>
    public static void Delete(string path, TimeSpan timeout, TimeSpan retryInterval, ISafeRetryPolicy? policy = null)
    {
        // バリデーション
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        if (policy is not null)
        {
            var ok = policy.Execute(
                () =>
                {
                try
                {
                    File.Delete(path);

                    // 完全に消えたか排他オープンで確認
                    try
                    {
                        using var probeStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        return false; // まだ存在
                    }
                    catch (FileNotFoundException)
                    {
                        return true; // 完全に消滅
                    }
                    catch (IOException)
                    {
                        return false; // ロック中等
                    }
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                });

            if (!ok)
            {
                throw new TimeoutException($"[SafeFile] ファイル「{path}」の削除がポリシーにより完了しませんでした。");
            }

            return;
        }

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
    /// 指定されたファイルを安全に非同期で削除します。ミリ秒指定のオーバーロードです。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeoutMs">削除操作の最大待機時間（ミリ秒）</param>
    /// <param name="retryIntervalMs">削除失敗時の再試行間隔（ミリ秒）</param>
    /// <param name="ct">操作のキャンセルを要求するためのトークン</param>
    /// <exception cref="ArgumentOutOfRangeException">timeoutMs が retryIntervalMs より短い、または負値の場合にスローされます</exception>
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
    /// 指定されたファイルを安全に非同期で削除します。最大リトライ回数を指定できるオーバーロードです。
    /// </summary>
    /// <param name="path">削除対象のファイルパス</param>
    /// <param name="timeout">削除操作の最大待機時間</param>
    /// <param name="retryInterval">削除失敗時の再試行間隔</param>
    /// <param name="maxRetries">リトライの最大回数（0 は再試行なし、負値は不可）</param>
    /// <param name="ct">操作のキャンセルを要求するためのトークン</param>
    /// <exception cref="ArgumentOutOfRangeException">timeout が retryInterval より短い、または maxRetries が負の場合にスローされます</exception>
    /// <exception cref="TimeoutException">指定条件内に削除が完了しない場合にスローされます</exception>
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

        // timeout < retryInterval のチェック
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        var sw = Stopwatch.StartNew();

        using var timer = new PeriodicTimer(retryInterval);
        var attempt = 0;

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

            // ── 3) タイムアウト／リトライ上限判定 ──
            if (sw.Elapsed >= timeout || attempt >= maxRetries)
            {
                throw new TimeoutException(
                    $"[SafeFile] ファイル「{path}」の削除完了待機が終了条件に達しました（timeout={timeout}, retries={attempt}/{maxRetries}）。");
            }

            attempt++;

            // ── 4) 次の再試行まで待機 ──
            if (!await timer.WaitForNextTickAsync(ct))
            {
                throw new OperationCanceledException("タイマー待機中にキャンセルされました。", ct);
            }
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
    /// <param name="policy">外部から注入された再試行ポリシー（未指定時は内部ループ）。</param>
    public static async Task DeleteAsync(
        string path,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct = default,
        ISafeRetryPolicy? policy = null)
    {
        // timeout < retryInterval のチェック
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, retryInterval, nameof(timeout));

        if (policy is not null)
        {
            var ok = await policy.ExecuteAsync(
                async ctk =>
                {
                ctk.ThrowIfCancellationRequested();
                try
                {
                    File.Delete(path);

                    try
                    {
                        await using var probeStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        return false;
                    }
                    catch (FileNotFoundException)
                    {
                        return true;
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                },
                ct);

            if (!ok)
            {
                throw new TimeoutException($"[SafeFile] ファイル「{path}」の削除がポリシーにより完了しませんでした。");
            }

            return;
        }

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
