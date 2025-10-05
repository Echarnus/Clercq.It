using Amazon.S3;
using Amazon.S3.Model;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Clercq.It.Infrastructure.Services;

public class ObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3? _s3Client;
    private readonly ObjectStorageSettings _settings;

    public ObjectStorageService(IOptions<ObjectStorageSettings> settings, IAmazonS3? s3Client = null)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<string> UploadFileAsync(string fileName, Stream fileContent, string contentType, CancellationToken cancellationToken = default)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("Object storage is not configured. Please configure ObjectStorage settings in appsettings.json");
        }
        
        var key = $"blog-images/{Guid.NewGuid()}/{fileName}";
        
        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = fileContent,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        
        // Return the public URL
        return $"{_settings.Endpoint}/{_settings.BucketName}/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("Object storage is not configured. Please configure ObjectStorage settings in appsettings.json");
        }
        
        // Extract key from URL
        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/');
        
        // Remove bucket name from the beginning if present
        if (key.StartsWith(_settings.BucketName + "/"))
        {
            key = key.Substring(_settings.BucketName.Length + 1);
        }

        var request = new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }
}
