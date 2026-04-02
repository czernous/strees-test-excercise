using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

/// <summary>
/// Buffered CSV parser for reference-data ingestion.
/// It keeps file loading separate from parsing while sizing the hot-path list from the buffered payload
/// to avoid large reallocation spikes on the loan dataset.
/// </summary>
public sealed partial class CsvParser : ICsvParser
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int MinInitialCapacity = 16;

    private readonly CsvConfiguration _csvConfig;
    private readonly IFileLoader _fileLoader;
    private readonly ILogger<CsvParser> _logger;
    public CsvParser(
        IFileLoader fileLoader,
        ILogger<CsvParser> logger)
    {
        _fileLoader = fileLoader;
        _logger = logger;

        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            BufferSize = FileStreamBufferSize,
            CacheFields = true,
            IncludePrivateMembers = false,
            ShouldUseConstructorParameters = _ => true
        };
    }

    #region Telemetry

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping malformed record in {FilePath} at row {Row}. Reason: {Reason}")]
    static partial void LogMalformedRecord(ILogger logger, string filePath, int row, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Structural failure parsing CSV: {FilePath}. Details: {Details}")]
    static partial void LogParsingFailure(ILogger logger, string filePath, string details, Exception ex);

    #endregion

    public async ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T, TMap>(string filePath, CancellationToken ct = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {
        var fileData = await _fileLoader.LoadAsync(filePath, FileStreamBufferSize, ct);
        return fileData.Bind(data => EnumerateRecords<T, TMap>(data, filePath, ct));
    }

    private Result<IReadOnlyList<T>, Error> EnumerateRecords<T, TMap>(
        (IMemoryOwner<byte> MemoryOwner, int BytesRead) fileData,
        string filePath,
        CancellationToken ct = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {
        // The hot loan file is already fully buffered, so counting line breaks here is cheaper than
        // letting List<T> reallocate through a badly underestimated capacity heuristic.
        var records = new List<T>(EstimateInitialCapacity(fileData.MemoryOwner.Memory.Span, fileData.BytesRead));

        try
        {
            foreach (var record in GetRecords<T, TMap>(fileData, filePath, ct))
            {
                records.Add(record);
            }

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

    private IEnumerable<T> GetRecords<T, TMap>(
        (IMemoryOwner<byte> MemoryOwner, int BytesRead) fileData,
        string filePath,
        CancellationToken ct = default)
        where TMap : ClassMap<T>
        where T : struct, IIntegrityContract
    {
        using (fileData.MemoryOwner)
        {
            if (!MemoryMarshal.TryGetArray(fileData.MemoryOwner.Memory, out ArraySegment<byte> segment))
            {
                throw new InvalidOperationException("High-performance parsing requires array-backed memory pools.");
            }

            var slicedSegment = new ArraySegment<byte>(segment.Array!, segment.Offset, fileData.BytesRead);

            using var stream = new MemoryStream(slicedSegment.Array!, slicedSegment.Offset, slicedSegment.Count, writable: false);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, FileStreamBufferSize);
            using var csv = new CsvReader(reader, _csvConfig);
            csv.Context.RegisterClassMap<TMap>();

            int rowCount = 0;
            foreach (var record in csv.GetRecords<T>())
            {
                ct.ThrowIfCancellationRequested();
                rowCount++;

                var validationError = ValidateRecord(record);

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

    private static int EstimateInitialCapacity(ReadOnlySpan<byte> fileData, int bytesRead)
    {
        if (bytesRead <= 0)
        {
            return MinInitialCapacity;
        }

        var lineCount = 0;
        for (var i = 0; i < bytesRead; i++)
        {
            if (fileData[i] == (byte)'\n')
            {
                lineCount++;
            }
        }

        if (bytesRead > 0 && fileData[bytesRead - 1] != (byte)'\n')
        {
            lineCount++;
        }

        var dataRowCount = Math.Max(0, lineCount - 1);
        return Math.Max(dataRowCount, MinInitialCapacity);
    }

    private static Error? ValidateRecord<T>(T record)
        where T : struct, IIntegrityContract =>
        record.Validate();}



