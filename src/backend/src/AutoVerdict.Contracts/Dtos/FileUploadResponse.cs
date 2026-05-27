namespace AutoVerdict.Contracts.Dtos;

public sealed record FileUploadResponse(
    string StorageKey,
    string ContentType,
    long FileSizeBytes);
