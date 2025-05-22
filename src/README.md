# Aloe.Utils.SafeIO

`Aloe.Utils.SafeIO` is a lightweight and easy-to-use library for safe file operations.
It can be used in applications that require file reading, writing, and manipulation.

## Key Features

* Safe File Deletion
  * Supports both synchronous and asynchronous operations
  * Configurable timeout and retry intervals
  * Proper handling of file lock states
* Safe Directory Deletion
  * Supports recursive deletion
  * Supports both synchronous and asynchronous operations
  * Configurable timeout and retry intervals
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
```

## License

MIT License
