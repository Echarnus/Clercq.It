using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Blogs.Queries;

public record GetBlogByIdQuery(Guid Id) : IRequest<BlogDto?>;
