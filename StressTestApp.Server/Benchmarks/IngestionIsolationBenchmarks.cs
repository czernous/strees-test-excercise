using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CsvHelper;
using CsvHelper.Configuration;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Models;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class IngestionIsolationBenchmarks
{
    private const int FileStreamBufferSize = 16 * 1024;

    private readonly FileLoader _fileLoader = new();
    private string _loansPath = string.Empty;
    private CsvConfiguration _csvConfig = null!;
    private CsvConfiguration _parserLikeCsvConfig = null!;
    private CsvConfiguration _parserLikeRawCsvConfig = null!;
    private int _exactRowCount;

    [GlobalSetup]
    public void Setup()
    {
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();
        _loansPath = Path.Combine(csvDirectory, "loans.csv");

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

        _parserLikeCsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            BufferSize = FileStreamBufferSize,
            CacheFields = true,
            IncludePrivateMembers = false,
            ShouldUseConstructorParameters = _ => true,
            ShouldSkipRecord = args => IsBlankRecord(args.Row.Parser.Record),
            ReadingExceptionOccurred = static args =>
            {
                return args.Exception is CsvHelper.TypeConversion.TypeConverterException &&
                       IsBlankRecord(args.Exception.Context?.Parser?.Record);
            }
        };

        _parserLikeRawCsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            BufferSize = FileStreamBufferSize,
            CacheFields = true,
            IncludePrivateMembers = false,
            ShouldUseConstructorParameters = _ => true,
            ShouldSkipRecord = args => string.IsNullOrWhiteSpace(args.Row.Parser.RawRecord),
            ReadingExceptionOccurred = static args =>
            {
                return args.Exception is CsvHelper.TypeConversion.TypeConverterException &&
                       string.IsNullOrWhiteSpace(args.Exception.Context?.Parser?.RawRecord);
            }
        };

        _exactRowCount = Math.Max(0, File.ReadLines(_loansPath).Skip(1).Count());
    }

    [Benchmark]
    public async Task<int> LoadOnlyAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;
        return loaded.Value.BytesRead;
    }

    [Benchmark]
    public async Task<int> DirectCsvHelperParseAsync()
    {
        await using var fileStream = new FileStream(
            _loansPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            FileStreamBufferSize,
            useAsync: true);

        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, FileStreamBufferSize);
        using var csv = CreateMappedCsv(reader);

        var count = 0;
        foreach (var _ in csv.GetRecords<Loan>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> BufferedMemoryStreamParseAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedMappedCsv(owner.Memory, loaded.Value.BytesRead);

        var count = 0;
        foreach (var _ in csv.GetRecords<Loan>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> BufferedMaterializeListWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            records.Add(record);
        }

        return records.Count;
    }

    [Benchmark]
    public async Task<int> ParserLikeMaterializeListWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedParserLikeMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            records.Add(record);
        }

        return records.Count;
    }

    [Benchmark]
    public async Task<int> ParserLikeRawMaterializeListWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedParserLikeRawMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            records.Add(record);
        }

        return records.Count;
    }

    [Benchmark]
    public async Task<int> BufferedMaterializeAndValidateWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            if (((IIntegrityContract)record).Validate() is null)
            {
                records.Add(record);
            }
        }

        return records.Count;
    }

    [Benchmark]
    public async Task<int> ParserLikeMaterializeAndValidateWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedParserLikeMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            if (((IIntegrityContract)record).Validate() is null)
            {
                records.Add(record);
            }
        }

        return records.Count;
    }

    [Benchmark]
    public async Task<int> ParserLikeRawMaterializeAndValidateWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;

        using var csv = CreateBufferedParserLikeRawMappedCsv(owner.Memory, loaded.Value.BytesRead);
        var records = new List<Loan>(_exactRowCount);

        foreach (var record in csv.GetRecords<Loan>())
        {
            if (((IIntegrityContract)record).Validate() is null)
            {
                records.Add(record);
            }
        }
        return records.Count;
    }

    private CsvReader CreateMappedCsv(TextReader reader)
    {
        var csv = new CsvReader(reader, _csvConfig);
        csv.Context.RegisterClassMap<LoanMap>();
        return csv;
    }

    private CsvReader CreateParserLikeMappedCsv(TextReader reader)
    {
        var csv = new CsvReader(reader, _parserLikeCsvConfig);
        csv.Context.RegisterClassMap<LoanMap>();
        return csv;
    }

    private CsvReader CreateParserLikeRawMappedCsv(TextReader reader)
    {
        var csv = new CsvReader(reader, _parserLikeRawCsvConfig);
        csv.Context.RegisterClassMap<LoanMap>();
        return csv;
    }

    private CsvReader CreateBufferedMappedCsv(Memory<byte> memory, int bytesRead)
    {
        var reader = CreateBufferedReader(memory, bytesRead);
        return CreateMappedCsv(reader);
    }

    private CsvReader CreateBufferedParserLikeMappedCsv(Memory<byte> memory, int bytesRead)
    {
        var reader = CreateBufferedReader(memory, bytesRead);
        return CreateParserLikeMappedCsv(reader);
    }

    private CsvReader CreateBufferedParserLikeRawMappedCsv(Memory<byte> memory, int bytesRead)
    {
        var reader = CreateBufferedReader(memory, bytesRead);
        return CreateParserLikeRawMappedCsv(reader);
    }

    private static TextReader CreateBufferedReader(Memory<byte> memory, int bytesRead)
    {
        if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
        {
            throw new InvalidOperationException("Expected array-backed memory pool.");
        }

        var sliced = new ArraySegment<byte>(segment.Array!, segment.Offset, bytesRead);
        var stream = new MemoryStream(sliced.Array!, sliced.Offset, sliced.Count, writable: false);
        return new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, FileStreamBufferSize);
    }

    private static bool IsBlankRecord(string[]? record)
    {
        if (record is null || record.Length == 0)
        {
            return true;
        }

        for (var i = 0; i < record.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(record[i]))
            {
                return false;
            }
        }

        return true;
    }
}


