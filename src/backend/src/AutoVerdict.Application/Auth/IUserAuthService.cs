using AutoVerdict.Domain.Entities;

namespace AutoVerdict.Application.Auth;

public interface IUserAuthService
{
    Task<User> FindOrCreateAsync(
        string provider,
        string providerUserId,
        string email,
        string? displayName,
        CancellationToken cancellationToken = default);
}
