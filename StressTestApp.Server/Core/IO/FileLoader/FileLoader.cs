using System.Buffers;
using StressTestApp.Server.Core.IO.FileLoader.Errors;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

namespace StressTestApp.Server.Core.IO.FileLoader;

/// <summary>
/// A high-performance file loader that utilizes <see cref="MemoryPool{T}"/> 
/// to minimize heap allocations and pressure on the Garbage Collector.
/// </summary>
public class FileLoader : IFileLoader
{
    /// <summary>
    /// Asynchronously reads a file into a rented memory buffer.
    /// </summary>
    /// <param name="path">The physical path to the source file.</param>
    /// <param name="bufferSize">The internal buffer size used by the <see cref="FileStream"/>.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T, E}"/> containing the rented <see cref="IMemoryOwner{byte}"/> 
    /// and the actual count of bytes read into that buffer.
    /// </returns>
    /// <remarks>
    /// <b>Ownership:</b> The caller is strictly responsible for disposing of the 
    /// returned <see cref="IMemoryOwner{byte}"/> to return the memory to the pool.
    /// </remarks>
    public async ValueTask<Result<(IMemoryOwner<byte> Memory, int BytesRead), Error>> LoadAsync(
        string path,
        int bufferSize = 4096,
        CancellationToken ct = default)
    {
        // 1. Guard against invalid paths
        if (string.IsNullOrWhiteSpace(path))
        {
            return FileLoaderError.NotFound("Empty Path");
        }

        var fileInfo = new FileInfo(path);

        // 2. Physical existence check
        if (!fileInfo.Exists)
        {
            return FileLoaderError.NotFound(path);
        }

        // 3. Resource check (Safety for massive files)
        if (fileInfo.Length > int.MaxValue)
        {
            return FileLoaderError.InsufficientResources("File size exceeds maximum buffer capacity (2GB).");
        }

        IMemoryOwner<byte> owner;
        // Borrow a buffer from the pool based on file size
        owner = MemoryPool<byte>.Shared.Rent((int)fileInfo.Length);
        try
        {
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                useAsync: true);

            // Read directly into the pooled memory slice
            // owner.Memory might be larger than fileInfo.Length, so we slice to be precise
            var destination = owner.Memory[..(int)fileInfo.Length];
            var totalBytesRead = 0;
            while (totalBytesRead < destination.Length)
            {
                var bytesRead = await fs.ReadAsync(destination[totalBytesRead..], ct);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return (owner, totalBytesRead);
        }
        catch (OperationCanceledException)
        {
            owner.Dispose(); // Clean up memory if cancelled
            return FileLoaderError.OperationCancelled();
        }
        catch (UnauthorizedAccessException)
        {
            owner.Dispose();
            return FileLoaderError.AccessDenied(path);
        }
        catch (IOException ex)
        {
            owner.Dispose();
            // Check for specific "Locked" HResult if needed, or default to ReadFailure
            return FileLoaderError.ReadFailure(ex.Message);
        }
        catch (Exception ex)
        {
            owner.Dispose();
            return Error.Unexpected(ex.Message);
        }
    }
}
