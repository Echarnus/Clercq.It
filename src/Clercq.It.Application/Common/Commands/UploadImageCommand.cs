using MediatR;

namespace Clercq.It.Application.Common.Commands;

public record UploadImageCommand(
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType
) : IRequest<UploadImageResult>;

public record UploadImageResult(string Url);
