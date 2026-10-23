using Clercq.It.Application.Common.DTOs;
using MediatR;

namespace Clercq.It.Application.Features.Blogs.Commands;

public record CreateBlogCommand(
    string Title,
    string ShortDescription,
    string LongDescription,
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType,
    string[] Tags
) : IRequest<BlogDto>;
