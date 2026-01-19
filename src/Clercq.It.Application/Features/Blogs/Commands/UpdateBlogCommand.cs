using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Blogs.Commands;

public record UpdateBlogCommand(
    Guid Id,
    string ShortDescription,
    string LongDescription,
    Stream? ImageStream,
    string? ImageFileName,
    string? ImageContentType,
    string[] Tags
) : IRequest<BlogDto?>;
