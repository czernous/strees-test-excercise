using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Shared.Contracts;

/// <summary>
/// Defines a contract for entities that require internal integrity validation 
/// before being processed by the domain.
/// </summary>
public interface IIntegrityContract
{
    /// <summary>
    /// Validates the record's state. 
    /// </summary>
    /// <returns>
    /// A specific <see cref="Error"/> if validation fails; 
    /// otherwise, <see langword="null"/> to indicate success.
    /// </returns>
    Error? Validate();
}