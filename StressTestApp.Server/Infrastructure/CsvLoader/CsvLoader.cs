using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Infrastructure.CsvLoader.Interfaces;
using System.Globalization;

namespace StressTestApp.Server.Infrastructure.CsvLoader;

public sealed class CsvLoader : ICsvDataLoader
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int EstimatedBytesPerRow = 200;
    private const int MaxInitialCapacity = 200_000;
    private const int MinInitialCapacity = 16;

    private readonly CsvConfiguration _csvConfig;

    public CsvLoader()
    {
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            BufferSize = FileStreamBufferSize,
            CacheFields = true,
            IncludePrivateMembers = true,
            ShouldUseConstructorParameters = _ => true
        };
    }

    public async Task<IReadOnlyList<T>> LoadCsvAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where TMap : ClassMap<T>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        var initialCapacity = EstimateInitialCapacity(filePath);

        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: FileStreamBufferSize,
            useAsync: true);

        using var streamReader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(streamReader, _csvConfig);

        csv.Context.RegisterClassMap<TMap>();

        var records = new List<T>(capacity: initialCapacity);

        await foreach (var record in csv.GetRecordsAsync<T>(cancellationToken))
        {
            records.Add(record);
        }

        return records;
    }

    private static int EstimateInitialCapacity(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length <= 0) return MinInitialCapacity;

            var estimatedRows = (int)(fileInfo.Length / EstimatedBytesPerRow);
            return Math.Clamp(estimatedRows, MinInitialCapacity, MaxInitialCapacity);
        }
        catch
        {
            return MinInitialCapacity;
        }
    }
}
