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

    [Fact(DisplayName = "ミリ秒オーバーロードで削除が正常に完了すること")]
    public void Delete_MsOverload_ShouldDeleteFileSuccessfully()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");

        // Act
        SafeFile.Delete(this._testFilePath, 5000, 50);

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

    [Fact(DisplayName = "最大リトライ回数に達した場合にタイムアウト例外が発生すること")]
    public void Delete_ShouldThrowTimeout_WhenMaxRetriesExceeded()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");
        using var fileStream = File.Open(this._testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        Assert.Throws<TimeoutException>(() =>
            SafeFile.Delete(this._testFilePath, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), maxRetries: 1));
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

    [Fact(DisplayName = "ミリ秒オーバーロード（非同期）で削除が正常に完了すること")]
    public async Task DeleteAsync_MsOverload_ShouldDeleteFileSuccessfully()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");

        // Act
        await SafeFile.DeleteAsync(this._testFilePath, 5000, 50);

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

    [Fact(DisplayName = "最大リトライ回数（非同期）に達した場合にタイムアウト例外が発生すること")]
    public async Task DeleteAsync_ShouldThrowTimeout_WhenMaxRetriesExceeded()
    {
        // Arrange
        File.WriteAllText(this._testFilePath, "test content");
        using var fileStream = File.Open(this._testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            SafeFile.DeleteAsync(this._testFilePath, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), maxRetries: 1));
    }

    [Fact(DisplayName = "SafeFile.Copy で .tmp 経由の安全なコピーが行えること（同期）")]
    public void Copy_ShouldCopySafely_WithTmpAndReplace_Sync()
    {
        // Arrange
        using var tempDir = new TempDir();
        var src = Path.Combine(tempDir.Path, "src.txt");
        var dst = Path.Combine(tempDir.Path, "dst.txt");
        File.WriteAllText(src, "hello");

        // Act
        SafeFile.Copy(src, dst, overwrite: false, timeout: TimeSpan.FromSeconds(5), retryInterval: TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.True(File.Exists(dst));
        Assert.Equal("hello", File.ReadAllText(dst));
        Assert.False(File.Exists(dst + ".tmp"));
    }

    [Fact(DisplayName = "SafeFile.CopyAsync で .tmp 経由の安全なコピーが行えること（非同期）")]
    public async Task CopyAsync_ShouldCopySafely_WithTmpAndReplace_Async()
    {
        // Arrange
        using var tempDir = new TempDir();
        var src = Path.Combine(tempDir.Path, "src.txt");
        var dst = Path.Combine(tempDir.Path, "dst.txt");
        File.WriteAllText(src, "hello");

        // Act
        await SafeFile.CopyAsync(src, dst, overwrite: false, timeout: TimeSpan.FromSeconds(5), retryInterval: TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.True(File.Exists(dst));
        Assert.Equal("hello", await File.ReadAllTextAsync(dst));
        Assert.False(File.Exists(dst + ".tmp"));
    }

    [Fact(DisplayName = "SafeFile.Copy がポリシー経由でも成功すること（同期）")]
    public void Copy_ShouldSucceed_WithPolicy_Sync()
    {
        using var tempDir = new TempDir();
        var src = Path.Combine(tempDir.Path, "src.txt");
        var dst = Path.Combine(tempDir.Path, "dst.txt");
        File.WriteAllText(src, "hello");

        var policy = new FixedRetryPolicy(maxRetries: 3, delay: TimeSpan.FromMilliseconds(10));
        SafeFile.Copy(src, dst, overwrite: true, timeout: TimeSpan.FromSeconds(5), retryInterval: TimeSpan.FromMilliseconds(10), policy: policy);

        Assert.True(File.Exists(dst));
        Assert.Equal("hello", File.ReadAllText(dst));
    }

    [Fact(DisplayName = "SafeFile.CopyAsync がポリシー経由でも成功すること（非同期）")]
    public async Task CopyAsync_ShouldSucceed_WithPolicy_Async()
    {
        using var tempDir = new TempDir();
        var src = Path.Combine(tempDir.Path, "src.txt");
        var dst = Path.Combine(tempDir.Path, "dst.txt");
        File.WriteAllText(src, "hello");

        var policy = new FixedRetryPolicy(maxRetries: 3, delay: TimeSpan.FromMilliseconds(10));
        await SafeFile.CopyAsync(src, dst, overwrite: true, timeout: TimeSpan.FromSeconds(5), retryInterval: TimeSpan.FromMilliseconds(10), ct: default, policy: policy);

        Assert.True(File.Exists(dst));
        Assert.Equal("hello", File.ReadAllText(dst));
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"SafeFileTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(this.Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(this.Path, recursive: true); } catch { }
        }
    }
}
