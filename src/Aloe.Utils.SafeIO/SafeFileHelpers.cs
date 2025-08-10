// <copyright file="SafeFileHelpers.cs" company="ted-sharp">
// Copyright (c) ted-sharp. All rights reserved.
// </copyright>

using System;
using System.IO;

namespace Aloe.Utils.SafeIO;

/// <summary>
/// SafeFile 内部で用いる補助関数群。
/// </summary>
internal static class SafeFileHelpers
{
    /// <summary>
    /// 指定パスに ReadOnly 属性が付いている場合は解除します。
    /// </summary>
    /// <param name="path">対象パス</param>
    public static void RemoveReadOnlyIfPresent(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var attrs = File.GetAttributes(path);
            if ((attrs & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// 2 つのパスが同一ボリュームかどうか判定します。
    /// </summary>
    /// <param name="pathA">パスA</param>
    /// <param name="pathB">パスB</param>
    /// <returns>同一ボリュームなら true</returns>
    public static bool IsSameVolume(string pathA, string pathB)
    {
        var rootA = Path.GetPathRoot(Path.GetFullPath(pathA));
        var rootB = Path.GetPathRoot(Path.GetFullPath(pathB));
        return string.Equals(rootA, rootB, StringComparison.OrdinalIgnoreCase);
    }
}
