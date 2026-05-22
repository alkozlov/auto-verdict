using Amazon.S3;
using Amazon.S3.Model;
using AutoVerdict.Application.Storage;
using Microsoft.Extensions.Options;

namespace AutoVerdict.Infrastructure.Storage;

public sealed class S3DocumentStorageClient : IDocumentStorageClient, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly S3Options _options;

    public S3DocumentStorageClient(IOptions<S3Options> options)
    {
        _options = options.Value;
        _s3 = new AmazonS3Client(
            _options.AccessKey,
            _options.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = _options.Endpoint,
                ForcePathStyle = true,
            });
    }

    public async Task<(byte[] Content, string ContentType)> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.Bucket,
            Key = storageKey,
        };

        GetObjectResponse response = await _s3.GetObjectAsync(request, cancellationToken);

        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);

        string contentType = response.Headers.ContentType ?? "application/octet-stream";
        return (ms.ToArray(), contentType);
    }

    public void Dispose() => _s3.Dispose();
}
