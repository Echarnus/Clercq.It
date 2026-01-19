using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Blogs.Queries;

public class GetBlogByIdQueryHandler : IRequestHandler<GetBlogByIdQuery, BlogDto?>
{
    private readonly IBlogRepository _blogRepository;

    public GetBlogByIdQueryHandler(IBlogRepository blogRepository)
    {
        _blogRepository = blogRepository;
    }

    public async Task<BlogDto?> Handle(GetBlogByIdQuery request, CancellationToken cancellationToken)
    {
        var blog = await _blogRepository.GetByIdAsync(request.Id, cancellationToken);

        if (blog == null)
        {
            return null;
        }

        return new BlogDto(
            blog.Id,
            blog.CreatedDate,
            blog.PublishDate,
            blog.ShortDescription,
            blog.LongDescription,
            blog.Image,
            blog.Tags.Values
        );
    }
}
