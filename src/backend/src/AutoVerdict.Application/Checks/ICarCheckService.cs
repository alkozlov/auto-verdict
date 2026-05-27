using AutoVerdict.Domain.Entities;

namespace AutoVerdict.Application.Checks;

public interface ICarCheckService
{
    Task<CarCheck> CreateAsync(
        Guid userId,
        Guid checkId,
        string description,
        string? listingUrl,
        string[] userImageKeys,
        CancellationToken cancellationToken = default);
}
