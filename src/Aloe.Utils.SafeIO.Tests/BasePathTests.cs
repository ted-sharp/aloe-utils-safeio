using System;
using System.IO;
using Xunit;

namespace Aloe.Utils.SafeIO.Tests;

public class BasePathTests : IDisposable
{
    private readonly string _originalBaseDirectory;

    public BasePathTests()
    {
        // テスト前のBaseDirectoryを保存
        this._originalBaseDirectory = BasePath.BaseDirectory;
    }

    public void Dispose()
    {
        // テスト後に元のBaseDirectoryを復元
        BasePath.BaseDirectory = this._originalBaseDirectory;
    }

    [Fact(DisplayName = "BaseDirectoryが正しく設定・取得できること")]
    public void BaseDirectory_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var expectedPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TestBaseDir"));

        // Act
        BasePath.BaseDirectory = expectedPath;

        // Assert
        Assert.Equal(expectedPath, BasePath.BaseDirectory);
    }

    [Fact(DisplayName = "BaseDirectoryにnullを設定すると例外が発生すること")]
    public void BaseDirectory_ShouldThrowArgumentException_WhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BasePath.BaseDirectory = null!);
    }

    [Theory(DisplayName = "BaseDirectoryに空文字または空白文字を設定すると例外が発生すること")]
    [InlineData("")]
    [InlineData("   ")]
    public void BaseDirectory_ShouldThrowArgumentException_WhenEmptyOrWhitespace(string invalidPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BasePath.BaseDirectory = invalidPath);
    }

    [Fact(DisplayName = "GetFullPathが絶対パスを正しく解決すること")]
    public void GetFullPath_ShouldResolveAbsolutePathCorrectly()
    {
        // Arrange
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TestBaseDir"));
        BasePath.BaseDirectory = baseDir;
        var relativePath = "test.txt";
        var expectedPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        // Act
        var result = BasePath.GetFullPath(relativePath);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Fact(DisplayName = "GetFullPathにnullを渡すと例外が発生すること")]
    public void GetFullPath_ShouldThrowArgumentNullException_WhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BasePath.GetFullPath(null!));
    }

    [Theory(DisplayName = "GetFullPathに空文字または空白文字を渡すと例外が発生すること")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFullPath_ShouldThrowArgumentException_WhenEmptyOrWhitespace(string invalidPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BasePath.GetFullPath(invalidPath));
    }

    [Fact(DisplayName = "GetRelativePathが相対パスを正しく変換すること")]
    public void GetRelativePath_ShouldConvertToRelativePathCorrectly()
    {
        // Arrange
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TestBaseDir"));
        BasePath.BaseDirectory = baseDir;
        var fullPath = Path.Combine(baseDir, "test.txt");
        var expectedRelativePath = "test.txt";

        // Act
        var result = BasePath.GetRelativePath(fullPath);

        // Assert
        Assert.Equal(expectedRelativePath, result);
    }

    [Fact(DisplayName = "GetRelativePathにnullを渡すと例外が発生すること")]
    public void GetRelativePath_ShouldThrowArgumentNullException_WhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BasePath.GetRelativePath(null!));
    }

    [Theory(DisplayName = "GetRelativePathに空文字または空白文字を渡すと例外が発生すること")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRelativePath_ShouldThrowArgumentException_WhenEmptyOrWhitespace(string invalidPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BasePath.GetRelativePath(invalidPath));
    }

    [Fact(DisplayName = "Combineが複数のパスを正しく結合すること")]
    public void Combine_ShouldCombinePathsCorrectly()
    {
        // Arrange
        var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TestBaseDir"));
        BasePath.BaseDirectory = baseDir;
        var expectedPath = Path.GetFullPath(Path.Combine(baseDir, "dir1", "dir2", "test.txt"));

        // Act
        var result = BasePath.Combine("dir1", "dir2", "test.txt");

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Fact(DisplayName = "Combineにnullを渡すと例外が発生すること")]
    public void Combine_ShouldThrowArgumentNullException_WhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BasePath.Combine(null!));
    }

    [Fact(DisplayName = "Combineに空配列を渡すと例外が発生すること")]
    public void Combine_ShouldThrowArgumentException_WhenEmptyArray()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BasePath.Combine(Array.Empty<string>()));
    }
}
