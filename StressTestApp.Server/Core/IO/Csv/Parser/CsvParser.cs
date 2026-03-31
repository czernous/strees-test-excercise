using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.FileLoader;
using System.Globalization;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

public sealed class CsvParser : ICsvParser
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int EstimatedBytesPerRow = 200;
    private const int MaxInitialCapacity = 200_000;
    private const int MinInitialCapacity = 16;

    private readonly CsvConfiguration _csvConfig;
    private readonly IFileLoader _fileLoader;

    public CsvParser(IFileLoader fileLoader)
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

        _fileLoader = fileLoader;
    }

    public async Task<IReadOnlyList<T>> ParseAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where TMap : ClassMap<T>
    {
        ArgumentNullException.ThrowIfNull(_fileLoader);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }
            var (fileStream, fileInfo) = await _fileLoader.LoadAsync(filePath, FileStreamBufferSize, cancellationToken);

            using (fileStream)
            using (var streamReader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true))
            using (var csv = new CsvReader(streamReader, _csvConfig))
            {
                csv.Context.RegisterClassMap<TMap>();

                var initialCapacity = EstimateInitialCapacity(fileInfo);

                var records = new List<T>(capacity: initialCapacity);

                await foreach (var record in csv.GetRecordsAsync<T>(cancellationToken))
                {
                    records.Add(record);
                }

                return records;
            }
    }

    private static int EstimateInitialCapacity(FileInfo fileInfo)
    {
        try
        {
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
