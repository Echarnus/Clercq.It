using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Blogs.Queries;

public class GetAllBlogsQueryHandler : IRequestHandler<GetAllBlogsQuery, IEnumerable<BlogDto>>
{
    private readonly IBlogRepository _blogRepository;

    public GetAllBlogsQueryHandler(IBlogRepository blogRepository)
    {
        _blogRepository = blogRepository;
    }

    public async Task<IEnumerable<BlogDto>> Handle(GetAllBlogsQuery request, CancellationToken cancellationToken)
    {
        var blogs = await _blogRepository.GetAllAsync(cancellationToken);
        
        return blogs.Select(b => new BlogDto(
            b.Id,
            b.CreatedDate,
            b.PublishDate,
            b.ShortDescription,
            b.LongDescription,
            b.Image,
            b.Tags.Values
        ));
    }
}