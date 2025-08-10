// <copyright file="ISafeRetryPolicy.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

namespace Aloe.Utils.SafeIO;

/// <summary>
/// 再試行戦略を注入するためのインターフェイス。Polly 等のライブラリをアダプトして利用できます。
/// </summary>
public interface ISafeRetryPolicy
{
    /// <summary>
    /// 同期操作に対して再試行を実行します。
    /// </summary>
    /// <param name="attempt">「1 回分の試行」を表すデリゲート。成功時に true、継続時に false を返します。</param>
    /// <returns>最終的に成功した場合は true、失敗/断念時は false。</returns>
    bool Execute(Func<bool> attempt);

    /// <summary>
    /// 非同期操作に対して再試行を実行します。
    /// </summary>
    /// <param name="attempt">「1 回分の試行」を表すデリゲート。成功時に true、継続時に false を返します。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>最終的に成功した場合は true、失敗/断念時は false。</returns>
    Task<bool> ExecuteAsync(Func<CancellationToken, Task<bool>> attempt, CancellationToken ct);
}
