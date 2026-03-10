using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class DeleteBlogCommandHandlerTests
{
    private readonly Mock<IBlogRepository> _mockBlogRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DeleteBlogCommandHandler _handler;

    public DeleteBlogCommandHandlerTests()
    {
        _mockBlogRepository = new Mock<IBlogRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new DeleteBlogCommandHandler(
            _mockBlogRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenBlogExists()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Blog to delete",
            LongDescription = "Long description",
            Image = "https://example.com/image.jpg",
            Tags = new Tags(new[] { "Tag1" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var command = new DeleteBlogCommand(blogId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _mockBlogRepository.Verify(x => x.DeleteAsync(
            MoqIt.Is<Blog>(b => b.Id == blogId),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenBlogNotFound()
    {
        // Arrange
        var blogId = Guid.NewGuid();

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Blog?)null);

        var command = new DeleteBlogCommand(blogId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockBlogRepository.Verify(x => x.DeleteAsync(
            MoqIt.IsAny<Blog>(),
            MoqIt.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteOnRepository()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Blog to delete",
            LongDescription = "Long description",
            Image = "https://example.com/image.jpg",
            Tags = new Tags(new[] { "Tag1" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var command = new DeleteBlogCommand(blogId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockBlogRepository.Verify(x => x.DeleteAsync(
            existingBlog,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
