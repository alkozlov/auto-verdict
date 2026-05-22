namespace AutoVerdict.Application.Storage;

public interface IDocumentStorageClient
{
    Task<(byte[] Content, string ContentType)> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
}
