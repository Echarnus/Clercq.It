using MediatR;

namespace Clercq.It.Application.Features.Blogs.Commands;

public record UploadBlogImageCommand(
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType
) : IRequest<UploadBlogImageResult>;

public record UploadBlogImageResult(string Url);
