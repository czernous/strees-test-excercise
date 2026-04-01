using StressTestApp.Server.Shared.Contracts;

namespace StressTestApp.Server.Shared.Models;

public record struct Portfolio(
    string Id,
    string Name,
    string Country,
    string Ccy
) : IIntegrityContract
{
    public readonly bool IsValid => !string.IsNullOrWhiteSpace(Id) &&
                           !string.IsNullOrWhiteSpace(Name) &&
                           !string.IsNullOrWhiteSpace(Country) &&
                           !string.IsNullOrWhiteSpace(Ccy);
}
