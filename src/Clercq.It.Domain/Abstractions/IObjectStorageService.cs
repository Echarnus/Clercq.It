namespace Clercq.It.Domain.Abstractions;

public interface IObjectStorageService
{
    /// <summary>
    /// Uploads a file to object storage
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="fileContent">File content stream</param>
    /// <param name="contentType">Content type of the file</param>
    /// <param name="isInlineImage">Whether this is an inline image (true) or header image (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(string fileName, Stream fileContent, string contentType, bool isInlineImage = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a file from object storage
    /// </summary>
    /// <param name="fileUrl">URL of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
