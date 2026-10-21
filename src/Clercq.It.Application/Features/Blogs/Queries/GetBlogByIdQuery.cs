using Clercq.It.Application.Common.DTOs;
using MediatR;

namespace Clercq.It.Application.Features.Blogs.Queries;

public record GetBlogByIdQuery(Guid Id) : IRequest<BlogDto?>;
