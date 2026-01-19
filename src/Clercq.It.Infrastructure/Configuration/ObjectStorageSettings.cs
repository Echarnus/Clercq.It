namespace Clercq.It.Infrastructure.Configuration;

public class ObjectStorageSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Use path-style URLs instead of virtual-hosted style.
    /// Required for MinIO and some S3-compatible services.
    /// When true: http://endpoint/bucket/key
    /// When false: http://bucket.endpoint/key
    /// </summary>
    public bool UsePathStyle { get; set; } = false;
}
