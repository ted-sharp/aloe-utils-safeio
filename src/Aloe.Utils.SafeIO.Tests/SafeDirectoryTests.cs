using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aloe.Utils.SafeIO.Tests;

public class SafeDirectoryTests
{
    private readonly string _testDirectoryPath;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _defaultRetryInterval = TimeSpan.FromMilliseconds(100);

    public SafeDirectoryTests()
    {
        this._testDirectoryPath = Path.Combine(Path.GetTempPath(), $"SafeDirectoryTest_{Guid.NewGuid()}");
    }

    [Fact(DisplayName = "ディレクトリの削除が正常に完了すること")]
    public void Delete_ShouldDeleteDirectorySuccessfully()
    {
        // Arrange
        Directory.CreateDirectory(this._testDirectoryPath);
        File.WriteAllText(Path.Combine(this._testDirectoryPath, "test.txt"), "test content");

        // Act
        SafeDirectory.Delete(this._testDirectoryPath, this._defaultTimeout, this._defaultRetryInterval);

        // Assert
        Assert.False(Directory.Exists(this._testDirectoryPath));
    }

    [Fact(DisplayName = "ディレクトリが削除できない場合、タイムアウト例外が発生すること")]
    public void Delete_ShouldThrowTimeoutException_WhenDirectoryCannotBeDeleted()
    {
        // Arrange
        Directory.CreateDirectory(this._testDirectoryPath);
        var filePath = Path.Combine(this._testDirectoryPath, "test.txt");
        File.WriteAllText(filePath, "test content");
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        Assert.Throws<TimeoutException>(() =>
            SafeDirectory.Delete(this._testDirectoryPath, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)));
    }

    [Fact(DisplayName = "タイムアウト時間が再試行間隔より短い場合、引数例外が発生すること")]
    public void Delete_ShouldThrowArgumentException_WhenTimeoutIsLessThanRetryInterval()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(10);
        var retryInterval = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeDirectory.Delete(this._testDirectoryPath, timeout, retryInterval));
    }

    [Fact(DisplayName = "非同期でディレクトリの削除が正常に完了すること")]
    public async Task DeleteAsync_ShouldDeleteDirectorySuccessfully()
    {
        // Arrange
        Directory.CreateDirectory(this._testDirectoryPath);
        File.WriteAllText(Path.Combine(this._testDirectoryPath, "test.txt"), "test content");

        // Act
        await SafeDirectory.DeleteAsync(this._testDirectoryPath, this._defaultTimeout, this._defaultRetryInterval);

        // Assert
        Assert.False(Directory.Exists(this._testDirectoryPath));
    }

    [Fact(DisplayName = "非同期でディレクトリが削除できない場合、タイムアウト例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowTimeoutException_WhenDirectoryCannotBeDeleted()
    {
        // Arrange
        Directory.CreateDirectory(this._testDirectoryPath);
        var filePath = Path.Combine(this._testDirectoryPath, "test.txt");
        File.WriteAllText(filePath, "test content");
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            SafeDirectory.DeleteAsync(this._testDirectoryPath, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10)));
    }

    [Fact(DisplayName = "非同期で操作がキャンセルされた場合、キャンセル例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        Directory.CreateDirectory(this._testDirectoryPath);
        var filePath = Path.Combine(this._testDirectoryPath, "test.txt");
        File.WriteAllText(filePath, "test content");
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var cts = new CancellationTokenSource();

        // Act
        var deleteTask = SafeDirectory.DeleteAsync(this._testDirectoryPath, this._defaultTimeout, this._defaultRetryInterval, cts.Token);
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
            SafeDirectory.DeleteAsync(this._testDirectoryPath, timeout, retryInterval));
    }
}
