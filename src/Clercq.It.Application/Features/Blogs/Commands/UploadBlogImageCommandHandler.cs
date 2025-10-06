using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Blogs.Commands;

public class UploadBlogImageCommandHandler : IRequestHandler<UploadBlogImageCommand, UploadBlogImageResult>
{
    private readonly IObjectStorageService _objectStorageService;

    public UploadBlogImageCommandHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<UploadBlogImageResult> Handle(UploadBlogImageCommand request, CancellationToken cancellationToken)
    {
        // Upload image to object storage with "inline" prefix to distinguish from header images
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: true,
            cancellationToken);

        return new UploadBlogImageResult(imageUrl);
    }
}
