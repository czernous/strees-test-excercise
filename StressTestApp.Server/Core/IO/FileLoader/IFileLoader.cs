using System.Buffers;

namespace StressTestApp.Server.Core.IO.FileLoader;

public interface IFileLoader
{
    // Accepts a file path. Returns a memory owner containing the file's bytes.
    // The caller is responsible for disposing the memory owner after use.
    ValueTask<(IMemoryOwner<byte>, int)> LoadAsync(string path, int bufferSize, CancellationToken ct = default);
}
