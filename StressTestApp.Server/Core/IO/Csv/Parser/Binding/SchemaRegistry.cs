namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

using nietras.SeparatedValues;
using StressTestApp.Server.Core.IO.Csv.Parser.Schemas;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

/// <summary>
/// Resolves generated schema binders and parsers for supported CSV contract types.
/// </summary>
internal static class SchemaRegistry
{
    public static Result<ColumnMap, Error> Bind<T>(SepReaderHeader header)
        where T : struct, IIntegrityContract =>
        Cache<T>.Binder is { } binder
            ? binder(header)
            : Error.Validation("Parser.UnsupportedType", $"No parser for {typeof(T).Name}");

    public static Result<T, Error> Parse<T>(Row row, in ColumnMap columns)
        where T : struct, IIntegrityContract =>
        Cache<T>.Parser is { } parser
            ? parser(row, columns)
            : Error.Validation("Parser.UnsupportedType", $"No parser for {typeof(T).Name}");

    private static class Cache<T>
        where T : struct, IIntegrityContract
    {
        internal static readonly Func<SepReaderHeader, Result<ColumnMap, Error>>? Binder = CreateBinder();
        internal static readonly BoundParser<T>? Parser = CreateParser();

        private static Func<SepReaderHeader, Result<ColumnMap, Error>>? CreateBinder() =>
            typeof(T) switch
            {
                Type t when t == typeof(Loan) => (Func<SepReaderHeader, Result<ColumnMap, Error>>)(object)(Func<SepReaderHeader, Result<ColumnMap, Error>>)LoanSchema.Bind,
                Type t when t == typeof(Portfolio) => (Func<SepReaderHeader, Result<ColumnMap, Error>>)(object)(Func<SepReaderHeader, Result<ColumnMap, Error>>)PortfolioSchema.Bind,
                Type t when t == typeof(Rating) => (Func<SepReaderHeader, Result<ColumnMap, Error>>)(object)(Func<SepReaderHeader, Result<ColumnMap, Error>>)RatingSchema.Bind,
                _ => null
            };

        private static BoundParser<T>? CreateParser() =>
            typeof(T) switch
            {
                Type t when t == typeof(Loan) => (BoundParser<T>)(object)(BoundParser<Loan>)LoanSchema.ParseBound,
                Type t when t == typeof(Portfolio) => (BoundParser<T>)(object)(BoundParser<Portfolio>)PortfolioSchema.ParseBound,
                Type t when t == typeof(Rating) => (BoundParser<T>)(object)(BoundParser<Rating>)RatingSchema.ParseBound,
                _ => null
            };
    }
}
