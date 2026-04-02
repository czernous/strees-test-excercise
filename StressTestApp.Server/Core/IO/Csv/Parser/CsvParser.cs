using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.Csv.Parser.Converters;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

/// <summary>
/// A high-performance, memory-optimized CSV parser designed for large-scale stress test data.
/// Employs <see cref="IAsyncEnumerable{T}"/> for streaming and <see cref="MemoryMarshal"/> 
/// to achieve zero-copy parsing from pooled byte buffers.
/// </summary>
public sealed partial class CsvParser : ICsvParser
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int EstimatedBytesPerRow = 200;
    private const int MaxInitialCapacity = 200_000;
    private const int MinInitialCapacity = 16;

    private readonly CsvConfiguration _csvConfig;
    private readonly IFileLoader _fileLoader;
    private readonly ILogger<CsvParser> _logger;
    private readonly DecimalConverter _decimalConverter;

    /// <summary>
    /// Initializes the parser with pooled file loading capabilities and specialized type converters.
    /// </summary>
    /// <param name="fileLoader">The service responsible for asynchronous, pooled file IO.</param>
    /// <param name="logger">Structured logger for diagnostic and malformed record reporting.</param>
    /// <param name="decimalConverter">DI-injected converter for high-precision financial data parsing.</param>
    public CsvParser(
        IFileLoader fileLoader,
        ILogger<CsvParser> logger,
        DecimalConverter decimalConverter)
    {
        _fileLoader = fileLoader;
        _logger = logger;
        _decimalConverter = decimalConverter;

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

            // Optimization: Filter out blank lines and whitespace-only rows at the parser level.
            ShouldSkipRecord = args =>
            {
                var record = args.Row.Parser.Record;
                return record is null or { Length: 0 } || record.All(string.IsNullOrWhiteSpace);
            },

            // Resilience: Only halt on structural corruption; skip insignificant conversion errors on blank lines.
            ReadingExceptionOccurred = static args =>
            {
                return args.Exception is CsvHelper.TypeConversion.TypeConverterException &&
                       args.Exception.Context?.Parser?.Record?.All(string.IsNullOrWhiteSpace) == true;
            }
        };
    }

    #region Telemetry

    /// <summary>
    /// Logs a warning when a record fails the domain-specific <see cref="IIntegrityContract"/> check.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping malformed record in {FilePath} at row {Row}. Reason: {Reason}")]
    static partial void LogMalformedRecord(ILogger logger, string filePath, int row, string reason);

    /// <summary>
    /// Logs a critical failure when the CSV structure is unreadable or the schema is mismatched.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Structural failure parsing CSV: {FilePath}. Details: {Details}")]
    static partial void LogParsingFailure(ILogger logger, string filePath, string details, Exception ex);

    #endregion

    /// <inheritdoc/>
    /// <remarks>
    /// This method follows a functional pipeline: Load -> Bind -> Enumerate.
    /// It ensures that file IO failures are propagated as <see cref="Error"/> objects without throwing exceptions.
    /// </remarks>
    public async ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T, TMap>(string filePath, CancellationToken ct = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {
        // Functional bind: If LoadAsync succeeds, move to EnumerateRecords. If it fails, return the Error.
        return await (await _fileLoader.LoadAsync(filePath, FileStreamBufferSize, ct))
            .Bind(data => EnumerateRecords<T, TMap>(data, filePath, ct).AsTask());
    }

    /// <summary>
    /// Orchestrates the collection of records from the stream into a pre-sized list.
    /// </summary>
    private async ValueTask<Result<IReadOnlyList<T>, Error>> EnumerateRecords<T, TMap>(
        (IMemoryOwner<byte> MemoryOwner, int BytesRead) fileData,
        string filePath,
        CancellationToken ct = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {
        // Pre-size the list based on file metadata to minimize LOH fragmentation and re-allocations.
        var records = new List<T>(EstimateInitialCapacity(filePath));

        try
        {
            await foreach (var record in GetRecords<T, TMap>(fileData, filePath, ct))
            {
                records.Add(record);
            }

            // Memory Management: Reclaim unused capacity if filtering was significant.
            if (records.Count < records.Capacity / 2)
            {
                records.TrimExcess();
            }

            return records;
        }
        catch (OperationCanceledException)
        {
            return Error.Create(
                ErrorCode.System.CancellationRequested,
                "CSV parsing was cancelled.");
        }
        catch (Exception ex)
        {
            LogParsingFailure(_logger, filePath, ex.Message, ex);
            return CsvParserError.CorruptStructure(ex.Message);
        }
    }

    /// <summary>
    /// Bridges the pooled <see cref="IMemoryOwner{T}"/> to the CSV reader logic via <see cref="MemoryMarshal"/>.
    /// </summary>
    private async IAsyncEnumerable<T> GetRecords<T, TMap>(
        (IMemoryOwner<byte> MemoryOwner, int BytesRead) fileData,
        string filePath,
        [EnumeratorCancellation] CancellationToken ct = default)
        where TMap : ClassMap<T>
        where T : struct, IIntegrityContract
    {
        // Return memory to pool immediately upon exhaustion of the enumerable.
        using (fileData.MemoryOwner)
        {
            if (!MemoryMarshal.TryGetArray(fileData.MemoryOwner.Memory, out ArraySegment<byte> segment))
            {
                throw new InvalidOperationException("High-performance parsing requires array-backed memory pools.");
            }

            var slicedSegment = new ArraySegment<byte>(segment.Array!, segment.Offset, fileData.BytesRead);

            using var stream = new MemoryStream(slicedSegment.Array!, slicedSegment.Offset, slicedSegment.Count, writable: false);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, FileStreamBufferSize);
            using var csv = new CsvReader(reader, _csvConfig);

            // Register specialized domain converters.
            csv.Context.TypeConverterCache.AddConverter<decimal>(_decimalConverter);
            csv.Context.RegisterClassMap<TMap>();

            int rowCount = 0;
            await foreach (var record in csv.GetRecordsAsync<T>(ct))
            {
                rowCount++;

                // Access the explicit interface method to maintain encapsulation.
                var validationError = ((IIntegrityContract)record).Validate();

                if (validationError is null)
                {
                    yield return record;
                }
                else
                {
                    LogMalformedRecord(_logger, filePath, rowCount, validationError.Value.Message);
                }
            }
        }
    }

    /// <summary>
    /// Heuristically estimates the required capacity for the record list to optimize memory usage.
    /// </summary>
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
