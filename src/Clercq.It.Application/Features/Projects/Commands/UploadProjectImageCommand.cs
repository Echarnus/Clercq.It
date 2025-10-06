using MediatR;

namespace Clercq.It.Application.Features.Projects.Commands;

public record UploadProjectImageCommand(
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType
) : IRequest<UploadProjectImageResult>;

public record UploadProjectImageResult(string Url);
