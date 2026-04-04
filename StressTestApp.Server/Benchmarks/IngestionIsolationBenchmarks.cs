using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using nietras.SeparatedValues;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Models;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class IngestionIsolationBenchmarks
{
    private const int FileStreamBufferSize = 16 * 1024;

    private readonly FileLoader _fileLoader = new();
    private string _loansPath = string.Empty;
    private SepReaderOptions _readerOptions;
    private int _exactRowCount;

    [GlobalSetup]
    public void Setup()
    {
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();
        _loansPath = Path.Combine(csvDirectory, "loans.csv");
        _readerOptions = Sep.New(',').Reader(options => options with
        {
            HasHeader = true,
            Trim = SepTrim.All,
            CultureInfo = CultureInfo.InvariantCulture,
            DisableColCountCheck = true
        });

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
    public int DirectSepParseAsync()
    {
        using var stream = new FileStream(
            _loansPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            FileStreamBufferSize,
            useAsync: false);

        var options = _readerOptions;
        using var reader = options.From(stream);
        return CountRows(reader);
    }

    [Benchmark]
    public async Task<int> BufferedSepParseAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;
        using var reader = CreateBufferedReader(owner.Memory, loaded.Value.BytesRead);
        return CountRows(reader);
    }

    [Benchmark]
    public async Task<int> BufferedSepMaterializeListWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;
        using var reader = CreateBufferedReader(owner.Memory, loaded.Value.BytesRead);
        return MaterializeLoans(reader, validate: false);
    }

    [Benchmark]
    public async Task<int> BufferedSepMaterializeAndValidateWithExactCapacityAsync()
    {
        var loaded = await _fileLoader.LoadAsync(_loansPath, FileStreamBufferSize, CancellationToken.None);
        using var owner = loaded.Value.Memory;
        using var reader = CreateBufferedReader(owner.Memory, loaded.Value.BytesRead);
        return MaterializeLoans(reader, validate: true);
    }

    private int CountRows(SepReader reader)
    {
        if (!reader.MoveNext())
        {
            return 0;
        }

        var count = 0;
        do
        {
            var row = reader.Current;
            if (!IsBlankRow(row))
            {
                count++;
            }
        }
        while (reader.MoveNext());

        return count;
    }

    private int MaterializeLoans(SepReader reader, bool validate)
    {
        if (!reader.MoveNext())
        {
            return 0;
        }

        var header = reader.Header;
        var idIndex = header.IndexOf("Loan_ID");
        var portIdIndex = header.IndexOf("Port_ID");
        var originalAmountIndex = header.IndexOf("OriginalLoanAmount");
        var outstandingAmountIndex = header.IndexOf("OutstandingAmount");
        var collateralValueIndex = header.IndexOf("CollateralValue");
        var creditRatingIndex = header.IndexOf("CreditRating");

        var records = new List<Loan>(_exactRowCount);
        do
        {
            var row = reader.Current;
            if (IsBlankRow(row))
            {
                continue;
            }

            if (!TryCreateLoan(row, idIndex, portIdIndex, originalAmountIndex, outstandingAmountIndex, collateralValueIndex, creditRatingIndex, out var loan))
            {
                continue;
            }

            if (!validate || ((IIntegrityContract)loan).Validate() is null)
            {
                records.Add(loan);
            }
        }
        while (reader.MoveNext());

        return records.Count;
    }

    private SepReader CreateBufferedReader(Memory<byte> memory, int bytesRead)
    {
        if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
        {
            throw new InvalidOperationException("Expected array-backed memory pool.");
        }

        var sliced = new ArraySegment<byte>(segment.Array!, segment.Offset, bytesRead);
        var stream = new MemoryStream(sliced.Array!, sliced.Offset, sliced.Count, writable: false);
        var options = _readerOptions;
        return options.From(stream);
    }

    private static bool TryCreateLoan(
        SepReader.Row row,
        int idIndex,
        int portIdIndex,
        int originalAmountIndex,
        int outstandingAmountIndex,
        int collateralValueIndex,
        int creditRatingIndex,
        out Loan loan)
    {
        loan = default;

        if (!row[idIndex].TryParse<int>(out var id) ||
            !row[portIdIndex].TryParse<int>(out var portId) ||
            !row[originalAmountIndex].TryParse<decimal>(out var originalAmount) ||
            !row[outstandingAmountIndex].TryParse<decimal>(out var outstandingAmount) ||
            !row[collateralValueIndex].TryParse<decimal>(out var collateralValue))
        {
            return false;
        }

        loan = new Loan(
            id,
            portId,
            originalAmount,
            outstandingAmount,
            collateralValue,
            row[creditRatingIndex].ToString());

        return true;
    }

    private static bool IsBlankRow(SepReader.Row row)
    {
        for (var i = 0; i < row.ColCount; i++)
        {
            if (!row[i].Span.IsWhiteSpace())
            {
                return false;
            }
        }

        return true;
    }
}
