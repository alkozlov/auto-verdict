namespace AutoVerdict.Contracts.Configuration;

public sealed class WhitelistOptions
{
    public const string SectionName = "Whitelist";

    public string Emails { get; set; } = "";

    private HashSet<string>? _set;

    public bool Contains(string email)
    {
        _set ??= new HashSet<string>(
            Emails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        return _set.Contains(email);
    }
}
