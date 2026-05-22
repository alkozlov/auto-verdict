namespace AutoVerdict.Domain.Entities;

public sealed class UploadedFile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
