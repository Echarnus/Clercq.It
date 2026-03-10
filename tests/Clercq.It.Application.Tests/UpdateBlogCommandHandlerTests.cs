using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class UpdateBlogCommandHandlerTests
{
    private readonly Mock<IBlogRepository> _mockBlogRepository;
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateBlogCommandHandler _handler;

    public UpdateBlogCommandHandlerTests()
    {
        _mockBlogRepository = new Mock<IBlogRepository>();
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new UpdateBlogCommandHandler(
            _mockBlogRepository.Object,
            _mockObjectStorageService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateBlog_WithCorrectProperties()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Old description",
            LongDescription = "Old long description",
            Image = "https://example.com/old-image.jpg",
            CreatedDate = DateTime.UtcNow.AddDays(-10),
            PublishDate = DateTime.UtcNow.AddDays(-5),
            Tags = new Tags(new[] { "OldTag" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var imageStream = new MemoryStream();
        var imageUrl = "https://example.com/new-image.jpg";

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);

        var command = new UpdateBlogCommand(
            blogId,
            "New short description",
            "New long description",
            imageStream,
            "new-image.jpg",
            "image/jpeg",
            new[] { "NewTag1", "NewTag2" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ShortDescription.Should().Be("New short description");
        result.LongDescription.Should().Be("New long description");
        result.Image.Should().Be(imageUrl);
        result.Tags.Should().BeEquivalentTo(new[] { "NewTag1", "NewTag2" });

        _mockBlogRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Blog>(b =>
                b.ShortDescription == "New short description" &&
                b.LongDescription == "New long description"),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenBlogNotFound()
    {
        // Arrange
        var blogId = Guid.NewGuid();

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Blog?)null);

        var command = new UpdateBlogCommand(
            blogId,
            "Short description",
            "Long description",
            null,
            null,
            null,
            new[] { "Tag1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _mockBlogRepository.Verify(x => x.UpdateAsync(
            MoqIt.IsAny<Blog>(),
            MoqIt.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUploadNewImage_WhenImageProvided()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Old description",
            LongDescription = "Old long description",
            Image = "https://example.com/old-image.jpg",
            Tags = new Tags(new[] { "Tag1" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var imageStream = new MemoryStream();
        var newImageUrl = "https://example.com/new-image.png";

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(newImageUrl);

        var command = new UpdateBlogCommand(
            blogId,
            "Short description",
            "Long description",
            imageStream,
            "new-image.png",
            "image/png",
            new[] { "Tag1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().Be(newImageUrl);

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "new-image.png",
            imageStream,
            "image/png",
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepExistingImage_WhenNoImageProvided()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingImageUrl = "https://example.com/existing-image.jpg";
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Old description",
            LongDescription = "Old long description",
            Image = existingImageUrl,
            Tags = new Tags(new[] { "Tag1" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var command = new UpdateBlogCommand(
            blogId,
            "New short description",
            "New long description",
            null,
            null,
            null,
            new[] { "Tag1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().Be(existingImageUrl);

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            MoqIt.IsAny<string>(),
            MoqIt.IsAny<Stream>(),
            MoqIt.IsAny<string>(),
            MoqIt.IsAny<bool>(),
            MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTags()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var existingBlog = new Blog
        {
            Id = blogId,
            ShortDescription = "Old description",
            LongDescription = "Old long description",
            Image = "https://example.com/image.jpg",
            Tags = new Tags(new[] { "OldTag1", "OldTag2" })
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlog);

        var newTags = new[] { "C#", "Testing", "DDD" };
        var command = new UpdateBlogCommand(
            blogId,
            "Short description",
            "Long description",
            null,
            null,
            null,
            newTags
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().BeEquivalentTo(newTags);

        _mockBlogRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Blog>(b => b.Tags.Values.SequenceEqual(newTags)),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
