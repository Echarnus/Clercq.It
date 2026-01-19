using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Features.Blogs.Commands;

public class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, BlogDto?>
{
    private readonly IBlogRepository _blogRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBlogCommandHandler(
        IBlogRepository blogRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _blogRepository = blogRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BlogDto?> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogRepository.GetByIdAsync(request.Id, cancellationToken);
        if (blog == null)
        {
            return null;
        }

        // Update image if provided
        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName) && !string.IsNullOrEmpty(request.ImageContentType))
        {
            var imageUrl = await _objectStorageService.UploadFileAsync(
                request.ImageFileName,
                request.ImageStream,
                request.ImageContentType,
                isInlineImage: false,
                cancellationToken);
            blog.Image = imageUrl;
        }

        // Update other properties
        blog.ShortDescription = request.ShortDescription;
        blog.LongDescription = request.LongDescription;
        blog.Tags = new Tags(request.Tags);

        await _blogRepository.UpdateAsync(blog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
