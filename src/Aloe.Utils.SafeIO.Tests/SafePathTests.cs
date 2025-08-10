using System;
using Xunit;

namespace Aloe.Utils.SafeIO.Tests;

public class SafePathTests
{
    [Fact(DisplayName = "SafePath.Combine が null/空/空白を無視して結合できること")]
    public void Combine_ShouldSkipNullAndWhitespace()
    {
        var path = SafePath.Combine(null, " ", "folder", "sub", "file.txt");
        Assert.EndsWith("folder" + System.IO.Path.DirectorySeparatorChar + "sub" + System.IO.Path.DirectorySeparatorChar + "file.txt", path);
    }

    [Fact(DisplayName = "SafePath.Combine が有効なセグメントなしで例外を投げること")]
    public void Combine_ShouldThrow_WhenNoUsableSegments()
    {
        Assert.Throws<ArgumentException>(() => SafePath.Combine(null, " "));
    }

    [Fact(DisplayName = "SafePath.WebCombine が重複スラッシュを取り除いて結合できること")]
    public void WebCombine_ShouldNormalizeSlashes()
    {
        var url = SafePath.WebCombine("https://example.com/", "/api/", "v1//", "/items");
        Assert.Equal("https://example.com/api/v1/items", url);
    }

    [Fact(DisplayName = "SafePath.WebCombine がクエリ/フラグメントを末尾に保持すること")]
    public void WebCombine_ShouldKeepTailFromLast()
    {
        var url = SafePath.WebCombine("https://example.com/base", "api", "v1", "items?id=1#top");
        Assert.Equal("https://example.com/base/api/v1/items?id=1#top", url);
    }
}



