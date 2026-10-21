namespace Clercq.It.ContentImport;

/// <summary>
/// Stores images referenced by content files and returns their public URL.
/// </summary>
public interface IContentImageStore
{
    Task<string> UploadAsync(string localPath, CancellationToken cancellationToken);
}
