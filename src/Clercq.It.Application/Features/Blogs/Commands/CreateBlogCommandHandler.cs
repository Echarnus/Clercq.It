using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;
using MediatR;

namespace Clercq.It.Application.Features.Blogs.Commands;

public class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, BlogDto>
{
    private readonly IBlogRepository _blogRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBlogCommandHandler(
        IBlogRepository blogRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _blogRepository = blogRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BlogDto> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        // Upload image to object storage (header image, not inline)
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: false,
            cancellationToken);

        // Create blog entity
        var blog = Blog.Create(
            request.Title,
            request.ShortDescription,
            request.LongDescription,
            imageUrl,
            new Tags(request.Tags),
            DateTime.UtcNow);

        // Add to repository
        await _blogRepository.AddAsync(blog, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return DTO
        return new BlogDto(
            blog.Id,
            blog.CreatedDate,
            blog.PublishDate,
            blog.Title,
            blog.ShortDescription,
            blog.LongDescription,
            blog.Image,
            blog.Tags.Values
        );
    }
}
