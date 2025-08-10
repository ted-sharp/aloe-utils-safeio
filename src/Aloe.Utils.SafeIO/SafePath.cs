// <copyright file="SafePath.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

namespace Aloe.Utils.SafeIO;

/// <summary>
/// 柔軟なパス結合ユーティリティ。
/// null/空/空白のセグメントを無視し、余計な区切りを気にせず連結できます。
/// </summary>
public static class SafePath
{
    /// <summary>
    /// ファイルシステム用にセグメントを柔軟に結合します。
    /// null/空/空白を無視し、<see cref="Path.Combine(string[])"/> で結合します。
    /// </summary>
    /// <param name="segments">結合するセグメント</param>
    /// <returns>結合結果（相対/絶対は入力に依存）</returns>
    /// <exception cref="ArgumentException">有効なセグメントが 1 つもない場合</exception>
    public static string Combine(params string?[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var usable = segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();

        if (usable.Length == 0)
        {
            throw new ArgumentException("有効なパスセグメントがありません。", nameof(segments));
        }

        return Path.Combine(usable);
    }

    /// <summary>
    /// Web 用のパスを柔軟に結合します。重複スラッシュを除去し、末尾のクエリ/フラグメントを保持します。
    /// 先頭にスキームを含む場合、<c>scheme://authority</c> を保ちつつパス部のみを結合します。
    /// </summary>
    /// <param name="segments">結合するセグメント</param>
    /// <returns>結合結果の URL 風パス</returns>
    /// <exception cref="ArgumentException">有効なセグメントが 1 つもない場合</exception>
    public static string WebCombine(params string?[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var usable = segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();

        if (usable.Length == 0)
        {
            throw new ArgumentException("有効なパスセグメントがありません。", nameof(segments));
        }

        // クエリ/フラグメントは末尾のセグメントのみ採用
        string ExtractTail(string s, out string head)
        {
            var q = s.IndexOf('?');
            var h = s.IndexOf('#');

            var cut = -1;
            if (q >= 0 && h >= 0)
            {
                cut = Math.Min(q, h);
            }
            else if (q >= 0)
            {
                cut = q;
            }
            else if (h >= 0)
            {
                cut = h;
            }

            if (cut >= 0)
            {
                head = s.Substring(0, cut);
                return s.Substring(cut);
            }

            head = s;
            return string.Empty;
        }

        static string NormalizePart(string part)
        {
            // バックスラッシュをスラッシュに寄せ、前後のスラッシュを除去
            part = part.Replace('\\', '/');
            return part.Trim('/');
        }

        string prefix = string.Empty; // 例: https://example.com
        var pathParts = new List<string>();

        // 先頭セグメントのスキーム/オーソリティ処理
        var first = usable[0];
        var hasScheme = first.Contains("://", StringComparison.Ordinal);
        if (hasScheme)
        {
            // 先頭のクエリ/フラグメント分離
            _ = ExtractTail(first, out var baseWithoutTail);

            var schemeIdx = baseWithoutTail.IndexOf("://", StringComparison.Ordinal);
            var rest = baseWithoutTail.Substring(schemeIdx + 3);
            var slashIdx = rest.IndexOf('/');
            if (slashIdx >= 0)
            {
                var authority = rest.Substring(0, slashIdx);
                var basePath = rest.Substring(slashIdx + 1);
                prefix = baseWithoutTail.Substring(0, schemeIdx + 3) + authority;
                if (!string.IsNullOrEmpty(basePath))
                {
                    pathParts.Add(NormalizePart(basePath));
                }
            }
            else
            {
                // パスを持たない純粋な origin
                prefix = baseWithoutTail;
            }
        }
        else
        {
            // 先頭は相対。クエリ/フラグメントは除去
            _ = ExtractTail(first, out var head);
            if (!string.IsNullOrEmpty(head))
            {
                pathParts.Add(NormalizePart(head));
            }
        }

        // 中間セグメント（末尾以外）はパスのみ採用
        for (var i = 1; i < usable.Length - 1; i++)
        {
            _ = ExtractTail(usable[i], out var head);
            if (!string.IsNullOrEmpty(head))
            {
                pathParts.Add(NormalizePart(head));
            }
        }

        // 末尾セグメント：パス + 最後のクエリ/フラグメント
        var lastTail = ExtractTail(usable[^1], out var lastHead);
        if (!string.IsNullOrEmpty(lastHead))
        {
            pathParts.Add(NormalizePart(lastHead));
        }

        var joinedPath = string.Join('/', pathParts.Where(p => !string.IsNullOrEmpty(p)));
        var result = string.IsNullOrEmpty(prefix)
            ? joinedPath
            : (string.IsNullOrEmpty(joinedPath) ? prefix : prefix + "/" + joinedPath);

        return result + lastTail;
    }
}
