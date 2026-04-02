using System.Buffers;
using StressTestApp.Server.Core.IO.FileLoader.Errors;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

namespace StressTestApp.Server.Core.IO.FileLoader;

/// <summary>
/// Defines a high-performance, zero-allocation contract for loading raw file data 
/// into memory-managed buffers.
/// </summary>
public interface IFileLoader
{
    /// <summary>
    /// Asynchronously loads a file's content into a rented buffer from a <see cref="MemoryPool{T}"/>.
    /// </summary>
    /// <param name="path">The physical or network path to the source file.</param>
    /// <param name="bufferSize">The maximum number of bytes to rent from the memory pool.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation. 
    /// The result contains:
    /// <list type="bullet">
    /// <item>
    /// <term>Success</term>
    /// <description>A tuple containing the <see cref="IMemoryOwner{T}"/> and the actual count of bytes read.</description>
    /// </item>
    /// <item>
    /// <term>Failure</term>
    /// <description>An <see cref="Error"/> mapped via <see cref="FileLoaderError"/> (e.g., NotFound, Locked, or ReadFailure).</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <b>Memory Management:</b> This method transfers ownership of the underlying buffer to the caller. 
    /// The caller <b>MUST</b> dispose of the returned <see cref="IMemoryOwner{byte}"/> to prevent memory leaks 
    /// and ensure the buffer is returned to the pool for reuse.
    /// </remarks>
    ValueTask<Result<(IMemoryOwner<byte> Memory, int BytesRead), Error>>
        LoadAsync(string path, int bufferSize, CancellationToken ct = default);
}