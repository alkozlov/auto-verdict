using AutoVerdict.Domain.Entities;

namespace AutoVerdict.Application.Checks;

public interface ICarCheckService
{
    /// <summary>
    /// Atomically deducts one credit, creates a <see cref="CarCheck"/>, and inserts an outbox message.
    /// </summary>
    /// <exception cref="InsufficientCreditsException">Thrown when the user's credit balance is zero.</exception>
    Task<CarCheck> CreateAsync(
        Guid userId,
        string vehicleIdentifier,
        string documentStorageKey,
        CancellationToken cancellationToken = default);
}
