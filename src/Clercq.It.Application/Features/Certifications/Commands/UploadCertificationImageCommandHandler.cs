using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class UploadCertificationImageCommandHandler : IRequestHandler<UploadCertificationImageCommand, string>
{
    private readonly IObjectStorageService _objectStorageService;

    public UploadCertificationImageCommandHandler(IObjectStorageService objectStorageService)
    {
        _objectStorageService = objectStorageService;
    }

    public async Task<string> Handle(UploadCertificationImageCommand request, CancellationToken cancellationToken)
    {
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: true,
            cancellationToken);

        return imageUrl;
    }
}
