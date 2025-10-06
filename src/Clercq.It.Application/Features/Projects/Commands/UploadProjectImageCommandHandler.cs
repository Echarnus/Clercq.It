using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Projects.Commands;

public class UploadProjectImageCommandHandler : IRequestHandler<UploadProjectImageCommand, UploadProjectImageResult>
{
    private readonly IObjectStorageService _objectStorageService;

    public UploadProjectImageCommandHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<UploadProjectImageResult> Handle(UploadProjectImageCommand request, CancellationToken cancellationToken)
    {
        // Upload image to object storage (project header image, not inline)
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: false,
            cancellationToken);

        return new UploadProjectImageResult(imageUrl);
    }
}
