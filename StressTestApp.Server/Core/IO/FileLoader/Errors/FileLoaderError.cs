using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Core.IO.FileLoader.Errors;

/// <summary>
/// Specialized factory for mapping File System and Hardware Stream failures 
/// to standardized <see cref="Error"/> primitives.
/// </summary>
public static class FileLoaderError
{
    #region Reading Operations

    /// <summary>
    /// The specified path does not exist or is inaccessible.
    /// Use this when <see cref="File.Exists"/> returns false.
    /// </summary>
    /// <param name="path">The target file path that failed to resolve.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.IO.NotFound"/> code.</returns>
    public static Error NotFound(string path) =>
        Error.Create(ErrorCode.IO.NotFound, $"File not found at path: {path}");

    /// <summary>
    /// The application lacks the OS-level identity permissions (NTFS/AppPool) 
    /// required to read or stream the target resource.
    /// </summary>
    /// <param name="path">The file path where access was restricted.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.IO.AccessDenied"/> code.</returns>
    public static Error AccessDenied(string path) =>
        Error.Create(ErrorCode.IO.AccessDenied, $"Access denied to file: {path}. Verify service account permissions.");

    /// <summary>
    /// A critical failure occurred during an active stream read operation.
    /// Typically indicates hardware failure, network disconnect, or unexpected EOF.
    /// </summary>
    /// <param name="reason">The underlying failure context or exception message.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.IO.ReadError"/> code.</returns>
    public static Error ReadFailure(string reason) =>
        Error.Create(ErrorCode.IO.ReadError, $"Critical failure during stream read: {reason}");

    /// <summary>
    /// The file is currently locked by another process with an exclusive handle.
    /// Common when files are open in Excel or being written to by another service.
    /// </summary>
    /// <param name="path">The file path currently under lock.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.IO.Locked"/> code.</returns>
    public static Error Locked(string path) =>
        Error.Create(ErrorCode.IO.Locked, $"The file is currently locked by another process: {path}");

    #endregion

    #region Writing Operations

    /// <summary>
    /// Failure during a write operation (e.g., Disk Full or Quota Exceeded).
    /// Use this when persisting stress test results or logs back to disk.
    /// </summary>
    /// <param name="path">The target destination path.</param>
    /// <param name="reason">The write failure message or underlying reason.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.IO.WriteError"/> code.</returns>
    public static Error WriteFailure(string path, string reason) =>
        Error.Create(ErrorCode.IO.WriteError, $"Failed to write to file '{path}': {reason}");

    #endregion

    #region Lifecycle & Resources

    /// <summary>
    /// Use when the file operation was aborted via a <see cref="System.Threading.CancellationToken"/>.
    /// Essential for handling graceful shutdowns of large data streams.
    /// </summary>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.System.CancellationRequested"/> code.</returns>
    public static Error OperationCancelled() =>
        Error.Create(ErrorCode.System.CancellationRequested, "The file operation was cancelled by the user or system.");

    /// <summary>
    /// The system was unable to allocate the required resources for the IO operation.
    /// Use this if <see cref="System.Buffers.MemoryPool{T}.Shared.Rent"/> fails or 
    /// if the file size exceeds the maximum allowed buffer capacity.
    /// </summary>
    /// <param name="detail">Specific details regarding the resource exhaustion.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.System.ResourceExhausted"/> code.</returns>
    public static Error InsufficientResources(string detail) =>
        Error.Create(ErrorCode.System.ResourceExhausted, $"System resources exhausted during IO: {detail}");

    #endregion
}