using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Shared.Models;

public readonly record struct Portfolio(
    string Id,
    string Name,
    string Country,
    string Ccy
): IIntegrityContract
{
    /// <summary>
    /// Performs a deep integrity check on the portfolio record.
    /// </summary>
    /// <returns>
    /// Success if all required fields are present; 
    /// otherwise, a specific <see cref="Error"/> detailing the missing field.
    /// </returns>
    Error? IIntegrityContract.Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
          return Error.Validation("Portfolio Id is required.");

        if (string.IsNullOrWhiteSpace(Name))
            return Error.Validation("Portfolio Name is required.");

        if (string.IsNullOrWhiteSpace(Country))
            return Error.Validation("Country/Region is required.");

        if (string.IsNullOrWhiteSpace(Ccy))
            return Error.Validation("Currency code is required.");

        return null;
    }

    public (Portfolio?, Error?) Create() => ((IIntegrityContract)this).Validate() is Error error
            ? (null, error)
            : (this, null);
}