using MediatR;

namespace Clercq.It.Application.Features.Certifications.Commands;

public record UploadCertificationImageCommand(
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType
) : IRequest<string>;
