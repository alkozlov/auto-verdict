namespace AutoVerdict.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string JwtSecret { get; set; } = null!;
    public string JwtIssuer { get; set; } = "auto-verdict";
    public string JwtAudience { get; set; } = "auto-verdict-api";
    public int JwtExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 30;
    public string GoogleClientId { get; set; } = null!;
    public string GoogleClientSecret { get; set; } = null!;
    public int InitialCredits { get; set; } = 1;
}
