using System.IdentityModel.Tokens.Jwt;
using AutoVerdict.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Api.Tests;

public sealed class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ExpiresInThirtyMinutes()
    {
        var service = new JwtService(Options.Create(new AuthOptions
        {
            JwtSecret = "test-secret-that-is-long-enough-for-hs256!!",
            JwtExpirationMinutes = 30,
        }));

        var before = DateTime.UtcNow;
        var token = service.GenerateToken(Guid.NewGuid(), "user@example.com");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expected = before.AddMinutes(30);
        Assert.InRange(jwt.ValidTo, expected.AddSeconds(-30), expected.AddSeconds(90));
    }
}
