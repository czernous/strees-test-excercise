using System.Buffers;

namespace StressTestApp.Server.Core.IO.FileLoader;

/// <summary>
/// Provides functionality to load files asynchronously.
/// </summary>
public class FileLoader : IFileLoader
{
    /// <summary>
    /// Loads a file asynchronously.
    /// </summary>
    /// <param name="path">The path of the file to load.</param>
    /// <param name="bufferSize">The buffer size for the file stream.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A tuple containing the file stream and file info.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer size is less than or equal to zero.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <remarks>
    /// The caller is responsible for disposing the returned memory owner after use.
    /// </remarks>
    public async ValueTask<IMemoryOwner<byte>> LoadAsync(string path, CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(path);
        // Borrow a buffer from the pool based on file size
        var owner = MemoryPool<byte>.Shared.Rent((int)fileInfo.Length);

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        // Read directly into the pooled memory
        int bytesRead = await fs.ReadAsync(owner.Memory, ct);

        // Return the owner so the caller can dispose it (returning it to the pool)
        return owner;
    }
}
