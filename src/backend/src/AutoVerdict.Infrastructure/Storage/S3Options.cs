namespace AutoVerdict.Infrastructure.Storage;

public sealed class S3Options
{
    public const string SectionName = "S3";
    public string Endpoint { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Bucket { get; set; } = "auto-verdict-local";
}
