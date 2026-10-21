using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Clercq.It.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clercq.It.ContentImport;

/// <summary>
/// Uploads content images to the blog images bucket under content/{hash}/{file}.
/// The key depends on the file bytes, so an unchanged image is never uploaded twice.
/// </summary>
public sealed class S3ContentImageStore(IAmazonS3 s3, IOptions<ObjectStorageSettings> settings, ILogger<S3ContentImageStore> logger)
    : IContentImageStore
{
    private readonly ObjectStorageSettings _settings = settings.Value;

    public async Task<string> UploadAsync(string localPath, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(localPath, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant();
        var key = $"content/{hash}/{Path.GetFileName(localPath)}";
        var url = $"{_settings.Endpoint}/{_settings.BucketName}/{key}";

        if (await ExistsAsync(key, cancellationToken))
        {
            return url;
        }

        using var stream = new MemoryStream(bytes);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = ContentTypeFor(localPath),
            CannedACL = S3CannedACL.PublicRead
        }, cancellationToken);
        logger.LogInformation("Uploaded {File} to {Key}", localPath, key);
        return url;
    }

    private async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await s3.GetObjectMetadataAsync(_settings.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream"
    };
}
