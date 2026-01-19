using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Blogs.Commands;

public class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, bool>
{
    private readonly IBlogRepository _blogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBlogCommandHandler(
        IBlogRepository blogRepository,
        IUnitOfWork unitOfWork)
    {
        _blogRepository = blogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogRepository.GetByIdAsync(request.Id, cancellationToken);
        if (blog == null)
        {
            return false;
        }

        await _blogRepository.DeleteAsync(blog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
