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
    /// The caller is responsible for disposing the returned stream after use.
    /// </remarks>
    public Task<(Stream, FileInfo)> LoadAsync(string path, int bufferSize, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be greater than zero.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }


        Stream fileStream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            useAsync: true);

        var fileInfo = new FileInfo(path);

        return Task.FromResult((fileStream, fileInfo));
    }
}
