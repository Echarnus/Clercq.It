using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Common.Commands;

public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, UploadImageResult>
{
    private readonly IObjectStorageService _objectStorageService;

    public UploadImageCommandHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<UploadImageResult> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: true,
            cancellationToken);

        return new UploadImageResult(imageUrl);
    }
}
