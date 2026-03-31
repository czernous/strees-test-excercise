namespace StressTestApp.Server.Core.IO.FileLoader;

public interface IFileLoader
{
    // Accepts a file path and file buffer size. Returns a tuple containing the file stream and file info.
    // The caller is responsible for disposing the stream after use.
    Task<(Stream, FileInfo)> LoadAsync(string path, int bufferSize, CancellationToken ct = default);
}
