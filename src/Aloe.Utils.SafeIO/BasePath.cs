// <copyright file="BasePath.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

namespace Aloe.Utils.SafeIO;

/// <summary>
/// ベースパスを基準としたパス解決ユーティリティ
/// </summary>
public static class BasePath
{
    /// <summary>
    /// スレッド間での可視性を確保するためのベースディレクトリ
    /// </summary>
    private static volatile string s_baseDirectory = AppContext.BaseDirectory;

    /// <summary>
    /// Gets or sets the base directory used for path resolution.
    /// </summary>
    /// <exception cref="ArgumentException">null、空文字、空白文字が指定された場合</exception>
    public static string BaseDirectory
    {
        get => s_baseDirectory;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

            // 無効なパス文字列の場合は Path.GetFullPath が例外を投げる
            s_baseDirectory = Path.GetFullPath(value);
        }
    }

    /// <summary>
    /// 指定パスを、BaseDirectory を基準とした絶対パスに解決します。
    /// </summary>
    /// <param name="path">解決対象のパス</param>
    /// <returns>解決された絶対パス</returns>
    /// <exception cref="ArgumentException">path が null、空文字、または空白文字の場合</exception>
    public static string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        // 絶対パスの場合はそのまま、相対パスの場合は BaseDirectory と結合して解決
        return Path.GetFullPath(path, BaseDirectory);
    }

    /// <summary>
    /// 指定パスを、BaseDirectory からの相対パスに変換します。
    /// </summary>
    /// <param name="path">変換対象のパス</param>
    /// <returns>BaseDirectory からの相対パス</returns>
    /// <exception cref="ArgumentException">path が null、空文字、または空白文字の場合</exception>
    public static string GetRelativePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var full = GetFullPath(path);
        return Path.GetRelativePath(BaseDirectory, full);
    }

    /// <summary>
    /// 複数のパスセグメントを結合し、BaseDirectory を基点とした絶対パスに解決します。
    /// </summary>
    /// <param name="paths">結合するパスセグメントの配列</param>
    /// <returns>結合・解決された絶対パス</returns>
    /// <exception cref="ArgumentNullException">paths が null の場合</exception>
    /// <exception cref="ArgumentException">paths が空配列の場合</exception>
    public static string Combine(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths, nameof(paths));
        if (paths.Length == 0)
        {
            throw new ArgumentException("少なくとも 1 つ以上のパスを指定してください。", nameof(paths));
        }

        // パスセグメントを結合してから絶対パスに解決
        var merged = Path.Combine(paths);
        return GetFullPath(merged);
    }
}
