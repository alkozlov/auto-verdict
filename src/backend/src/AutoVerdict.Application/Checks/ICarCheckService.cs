using AutoVerdict.Domain.Entities;

namespace AutoVerdict.Application.Checks;

public interface ICarCheckService
{
    /// <summary>
    /// Creates a <see cref="CarCheck"/> and inserts an outbox message.
    /// Credits are charged only after processing completes successfully.
    /// </summary>
    Task<CarCheck> CreateAsync(
        Guid userId,
        string listingUrl,
        CancellationToken cancellationToken = default);
}
