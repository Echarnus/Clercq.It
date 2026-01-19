using MediatR;

namespace Clercq.It.Application.Features.Blogs.Commands;

public record DeleteBlogCommand(Guid Id) : IRequest<bool>;
