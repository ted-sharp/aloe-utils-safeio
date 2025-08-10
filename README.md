# Aloe.Utils.SafeIO

[![English](https://img.shields.io/badge/Language-English-blue)](./README.md)
[![日本語](https://img.shields.io/badge/言語-日本語-blue)](./README.ja.md)

[![NuGet Version](https://img.shields.io/nuget/v/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aloe.Utils.SafeIO.svg)](https://www.nuget.org/packages/Aloe.Utils.SafeIO)
[![License](https://img.shields.io/github/license/ted-sharp/aloe-utils-safeio.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

`Aloe.Utils.SafeIO` is a lightweight and easy-to-use library for safe file operations.
It can be used in applications that require file reading, writing, and manipulation.

## Key Features

* Safe File Deletion
  * Supports both synchronous and asynchronous operations
  * Configurable timeout and retry intervals (also millisecond overloads)
  * Optional maxRetries overloads
  * Proper handling of file lock states (delete-pending, exclusive open check)
* Safe Directory Deletion
  * Supports recursive deletion
  * Supports both synchronous and asynchronous operations
  * Configurable timeout and retry intervals (also millisecond overloads)
  * Optional maxRetries overloads
* Safe File Copy
  * Copies to a temporary `.tmp` file, then atomically replaces/moves to destination
  * Handles read-only attributes (e.g., media/CD sources)
  * Synchronous and asynchronous APIs with retry and timeout
* SafePath Utilities
  * `SafePath.Combine(...)` for flexible FS path joins (skips null/empty/whitespace)
  * `SafePath.WebCombine(...)` for URL-like path joins with duplicate slash removal
* Operation cancellation using cancellation tokens
* Optimized exception handling

## Supported Environments

* .NET 9 and later

## Installation

Install via NuGet Package Manager:

```cmd
Install-Package Aloe.Utils.SafeIO
```

Or using .NET CLI:

```cmd
dotnet add package Aloe.Utils.SafeIO
```

## Usage Examples

### Safe File Deletion

```csharp
using Aloe.Utils.SafeIO;

// Synchronous version
SafeFile.Delete(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// Asynchronous version
await SafeFile.DeleteAsync(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);

// Millisecond overload
SafeFile.Delete(
    "example.txt",
    timeoutMs: 30_000,
    retryIntervalMs: 100
);

// Max retries overload (stops when either timeout or max retries reached)
SafeFile.Delete(
    "example.txt",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    maxRetries: 100
);
```

### Safe Directory Deletion

```csharp
using Aloe.Utils.SafeIO;

// Synchronous version
SafeDirectory.Delete(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// Asynchronous version
await SafeDirectory.DeleteAsync(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);

// Millisecond overload
SafeDirectory.Delete(
    "example-directory",
    timeoutMs: 30_000,
    retryIntervalMs: 100
);

// Max retries overload
SafeDirectory.Delete(
    "example-directory",
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    maxRetries: 100
);
```

### Safe File Copy

```csharp
using Aloe.Utils.SafeIO;

// Synchronous
SafeFile.Copy(
    source: "source.txt",
    destination: "dest.txt",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// Asynchronous
await SafeFile.CopyAsync(
    source: "source.txt",
    destination: "dest.txt",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

### Safe Directory Copy

```csharp
using Aloe.Utils.SafeIO;

// Synchronous (copies recursively)
SafeDirectory.Copy(
    sourceDirectory: "srcDir",
    destinationDirectory: "dstDir",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100)
);

// Asynchronous
await SafeDirectory.CopyAsync(
    sourceDirectory: "srcDir",
    destinationDirectory: "dstDir",
    overwrite: true,
    timeout: TimeSpan.FromSeconds(30),
    retryInterval: TimeSpan.FromMilliseconds(100),
    ct: cancellationToken
);
```

### SafePath Utilities

```csharp
using Aloe.Utils.SafeIO;

// Flexible FS path combining (skips null/empty/whitespace)
var fsPath = SafePath.Combine(null, "  ", "folder", "sub", "file.txt");

// URL-like combining with duplicate slash removal and tail preservation
var url = SafePath.WebCombine("https://example.com/", "/api/", "v1//", "items?id=1#top");
```

## Optional: Integrate Polly (custom retry policy)

You can inject your own retry policy using `ISafeRetryPolicy` without adding Polly as a hard dependency of this library.

```csharp
using Aloe.Utils.SafeIO;
using Polly;
using Polly.Retry;

sealed class PollyRetryPolicy : ISafeRetryPolicy
{
  private readonly RetryPolicy _sync;
  private readonly AsyncRetryPolicy _async;

  public PollyRetryPolicy(RetryPolicy sync, AsyncRetryPolicy async)
  {
    _sync = sync; _async = async;
  }

  public bool Execute(Func<bool> attempt)
    => _sync.Execute(attempt);

  public Task<bool> ExecuteAsync(Func<CancellationToken, Task<bool>> attempt, CancellationToken ct)
    => _async.ExecuteAsync((_, token) => attempt(token), new Context(), ct);
}

var policy = new PollyRetryPolicy(
  Policy
    .HandleResult(false)
    .Or<IOException>()
    .Or<UnauthorizedAccessException>()
    .WaitAndRetry(5, i => TimeSpan.FromMilliseconds(100)),
  Policy
    .HandleResult(false)
    .Or<IOException>()
    .Or<UnauthorizedAccessException>()
    .WaitAndRetryAsync(5, i => TimeSpan.FromMilliseconds(100))
);

// Use with SafeFile.Copy (policy parameter is optional)
SafeFile.Copy("source.txt", "dest.txt", overwrite: true,
              timeout: TimeSpan.FromSeconds(30), retryInterval: TimeSpan.FromMilliseconds(100),
              policy: policy);
```

## License

MIT License

## Contributing

Please report bugs and feature requests through GitHub Issues. Pull requests are welcome. 