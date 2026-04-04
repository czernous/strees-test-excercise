using nietras.SeparatedValues;
using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

/// <summary>
/// Orchestrates file loading and parses typed CSV records through schema descriptors.
/// </summary>
public sealed partial class CsvParser : ICsvParser
{
    private const int FileStreamBufferSize = 16 * 1024; // 16KB
    private const int MinInitialCapacity = 16;

    private static readonly SepReaderOptions ReaderOptions = nietras.SeparatedValues.Sep.New(',').Reader(options => options with
    {
        HasHeader = true,
        Trim = SepTrim.None,
        CultureInfo = CultureInfo.InvariantCulture,
        DisableColCountCheck = true,
        DisableQuotesParsing = true,
        CreateToString = SepToString.PoolPerCol(maximumStringLength: 128)
    });

    private readonly IFileLoader _fileLoader;
    private readonly ILogger<CsvParser> _logger;

    public CsvParser(
        IFileLoader fileLoader,
        ILogger<CsvParser> logger)
    {
        _fileLoader = fileLoader;
        _logger = logger;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Structural failure parsing CSV: {FilePath}. Details: {Details}")]
    static partial void LogParsingFailure(ILogger logger, string filePath, string details, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping malformed record in {FilePath} at row {Row}. Reason: {Reason}")]
    static partial void LogMalformedRecord(ILogger logger, string filePath, int row, string reason);

    public async ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T>(string filePath, CancellationToken ct = default)
        where T : struct, IIntegrityContract
    {
        var fileData = await _fileLoader.LoadAsync(filePath, FileStreamBufferSize, ct);
        return fileData.Bind(data => ParseBuffered<T>(data, filePath, ct));
    }

    private Result<IReadOnlyList<T>, Error> ParseBuffered<T>(
        (IMemoryOwner<byte> Memory, int BytesRead) fileData,
        string filePath,
        CancellationToken ct)
        where T : struct, IIntegrityContract
    {
        using (fileData.Memory)
        {
            try
            {
                using var reader = CreateReader(fileData.Memory.Memory, fileData.BytesRead);
                if (!reader.MoveNext())
                {
                    return Array.Empty<T>();
                }

                var columns = SchemaRegistry.Bind<T>(reader.Header);
                if (!columns.IsSuccess)
                {
                    return columns.Error;
                }

                var records = new List<T>(EstimateInitialCapacity(fileData.Memory.Memory.Span, fileData.BytesRead));

                do
                {
                    ct.ThrowIfCancellationRequested();
                    var row = new Row(reader.Current);
                    if (IsBlankRow(row))
                    {
                        continue;
                    }

                    var result = SchemaRegistry.Parse<T>(row, columns.Value);
                    if (!result.IsSuccess)
                    {
                        LogMalformedRecord(_logger, filePath, row.LineNumber, result.Error.Message);
                        continue;
                    }

                    var validationError = result.Value.Validate();
                    if (validationError is null)
                    {
                        records.Add(result.Value);
                    }
                    else
                    {
                        LogMalformedRecord(_logger, filePath, row.LineNumber, validationError.Value.Message);
                    }
                }
                while (reader.MoveNext());

                TrimIfSparse(records);
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
                return Errors.CsvParserError.CorruptStructure(ex.Message);
            }
        }
    }

    private static SepReader CreateReader(Memory<byte> memory, int bytesRead)
    {
        if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
        {
            throw new InvalidOperationException("High-performance parsing requires array-backed memory pools.");
        }

        var slicedSegment = new ArraySegment<byte>(segment.Array!, segment.Offset, bytesRead);
        var stream = new MemoryStream(slicedSegment.Array!, slicedSegment.Offset, slicedSegment.Count, writable: false);
        var options = ReaderOptions;
        return options.From(stream);
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

    private static bool IsBlankRow(Row row)
    {
        for (var i = 0; i < row.ColumnCount; i++)
        {
            if (!row[i].Span.IsWhiteSpace())
            {
                return false;
            }
        }

        return true;
    }

    private static void TrimIfSparse<T>(List<T> records)
    {
        if (records.Count < records.Capacity / 2)
        {
            records.TrimExcess();
        }
    }
}
