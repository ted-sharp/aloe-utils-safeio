using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aloe.Utils.SafeIO.Tests;

public class SafeFileTests
{
    private readonly string _testFilePath;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _defaultRetryInterval = TimeSpan.FromMilliseconds(100);

    public SafeFileTests()
    {
        this._testFilePath = Path.Combine(Path.GetTempPath(), $"SafeFileTest_{Guid.NewGuid()}.txt");
    }

    [Fact(DisplayName = "ファイルの削除が正常に完了すること")]
    public void Delete_ShouldDeleteFileSuccessfully()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");

        // Act
        SafeFile.Delete(this._testFilePath, this._defaultTimeout, this._defaultRetryInterval);

        // Assert
        Assert.False(File.Exists(this._testFilePath));
    }

    [Fact(DisplayName = "ファイルが削除できない場合、タイムアウト例外が発生すること")]
    public void Delete_ShouldThrowTimeoutException_WhenFileCannotBeDeleted()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");
        using var fileStream = File.Open(this._testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        Assert.Throws<TimeoutException>(() =>
            SafeFile.Delete(this._testFilePath, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)));
    }

    [Fact(DisplayName = "タイムアウト時間が再試行間隔より短い場合、引数例外が発生すること")]
    public void Delete_ShouldThrowArgumentException_WhenTimeoutIsLessThanRetryInterval()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(10);
        var retryInterval = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeFile.Delete(this._testFilePath, timeout, retryInterval));
    }

    [Fact(DisplayName = "非同期でファイルの削除が正常に完了すること")]
    public async Task DeleteAsync_ShouldDeleteFileSuccessfully()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");

        // Act
        await SafeFile.DeleteAsync(this._testFilePath, this._defaultTimeout, this._defaultRetryInterval);

        // Assert
        Assert.False(File.Exists(this._testFilePath));
    }

    [Fact(DisplayName = "非同期でファイルが削除できない場合、タイムアウト例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowTimeoutException_WhenFileCannotBeDeleted()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");
        using var fileStream = File.Open(this._testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            SafeFile.DeleteAsync(this._testFilePath, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)));
    }

    [Fact(DisplayName = "非同期で操作がキャンセルされた場合、キャンセル例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");
        using var fileStream = File.Open(this._testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var cts = new CancellationTokenSource();

        // Act
        var deleteTask = SafeFile.DeleteAsync(this._testFilePath, this._defaultTimeout, this._defaultRetryInterval, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => deleteTask);
    }

    [Fact(DisplayName = "非同期でタイムアウト時間が再試行間隔より短い場合、引数例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowArgumentException_WhenTimeoutIsLessThanRetryInterval()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(10);
        var retryInterval = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            SafeFile.DeleteAsync(this._testFilePath, timeout, retryInterval));
    }
}
