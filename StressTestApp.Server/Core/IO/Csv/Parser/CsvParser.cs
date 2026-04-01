using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.Csv.Parser.Converters;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StressTestApp.Server.Core.IO.Csv.Parser;


public sealed partial class CsvParser : ICsvParser
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int EstimatedBytesPerRow = 200;
    private const int MaxInitialCapacity = 200_000;
    private const int MinInitialCapacity = 16;
    private readonly CsvConfiguration _csvConfig;
    private readonly IFileLoader _fileLoader;
    private readonly ILogger<CsvParser> _logger;


    public CsvParser(
        IFileLoader fileLoader,
        ILogger<CsvParser> logger)
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
            ShouldUseConstructorParameters = _ => true,
            ShouldSkipRecord = args =>
                {
                    var record = args.Row.Parser.Record;
                    return record is null or { Length: 0 } || record.All(string.IsNullOrWhiteSpace);
                },
            ReadingExceptionOccurred = static args =>
            {
                // Only skip completely blank rows - let other exceptions propagate
                // Consumer decides how to handle missing/invalid fields
                if (args.Exception is CsvHelper.TypeConversion.TypeConverterException &&
                    args.Exception.Context?.Parser?.Record?.All(string.IsNullOrWhiteSpace) == true)
                {
                    return true; // Skip blank rows
                }
                return false; // Throw for any other data quality issues
            }
        };

        _fileLoader = fileLoader;
        _logger = logger;
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping malformed record in {FilePath} at row {Row}.")]
    static partial void LogMalformedRecord(ILogger logger, string filePath, int row);
    public async Task<IReadOnlyList<T>> ParseAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {

        var records = new List<T>(EstimateInitialCapacity(filePath));

        await foreach (var record in GetRecords<T, TMap>(filePath, cancellationToken))
        {
           records.Add(record);
        }
        // reduce capacity if records were filtred out 
        if (records.Count < records.Capacity / 2) records.TrimExcess();
        return records;

    }

    private async IAsyncEnumerable<T> GetRecords<T, TMap>(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TMap : ClassMap<T>
        where T: struct, IIntegrityContract
    {
        ArgumentNullException.ThrowIfNull(_fileLoader);

        var (memoryOwner, bytesRead) = await _fileLoader.LoadAsync(filePath, FileStreamBufferSize, cancellationToken);

        using (memoryOwner)
        {
            // Try to get the underlying array to avoid .ToArray() copy
            if (!MemoryMarshal.TryGetArray(memoryOwner.Memory, out ArraySegment<byte> segment))
            {
                throw new InvalidOperationException("MemoryPool must be array-backed for this implementation.");
            }
            // Use the segment (array + offset + count) to wrap the memory without copying
            var slicedSegment = new ArraySegment<byte>(segment.Array!, segment.Offset, bytesRead);

            using var stream = new MemoryStream(slicedSegment.Array!, slicedSegment.Offset, slicedSegment.Count, writable: false);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, FileStreamBufferSize);
            using var csv = new CsvReader(reader, _csvConfig);

            csv.Context.TypeConverterCache.AddConverter<decimal>(new DecimalConverter());
            csv.Context.RegisterClassMap<TMap>();
            
            int rowCount = 0;
            await foreach (var record in csv.GetRecordsAsync<T>(cancellationToken))
            {
                rowCount++;
                if (record.IsValid)
                {
                    yield return record;
                }
                else
                {
                    LogMalformedRecord(_logger, filePath, rowCount);
                }
            }
        }
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
