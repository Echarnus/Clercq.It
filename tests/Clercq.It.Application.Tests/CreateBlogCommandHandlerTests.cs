using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class CreateBlogCommandHandlerTests
{
    private readonly Mock<IBlogRepository> _mockBlogRepository;
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateBlogCommandHandler _handler;

    public CreateBlogCommandHandlerTests()
    {
        _mockBlogRepository = new Mock<IBlogRepository>();
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        _handler = new CreateBlogCommandHandler(
            _mockBlogRepository.Object,
            _mockObjectStorageService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateBlog_WithCorrectProperties()
    {
        // Arrange
        var imageUrl = "https://s3.fr-par.scw.cloud/test-bucket/blog-images/test-image.jpg";
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);

        var command = new CreateBlogCommand(
            "Short description",
            "# Long Description\n\nThis is markdown content.",
            imageStream,
            "test-image.jpg",
            "image/jpeg",
            new[] { "Development", "Testing" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ShortDescription.Should().Be("Short description");
        result.LongDescription.Should().Be("# Long Description\n\nThis is markdown content.");
        result.Image.Should().Be(imageUrl);
        result.Tags.Should().BeEquivalentTo(new[] { "Development", "Testing" });
        result.Id.Should().NotBeEmpty();

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "test-image.jpg",
            imageStream,
            "image/jpeg",
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockBlogRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Blog>(b => 
                b.ShortDescription == "Short description" &&
                b.LongDescription == "# Long Description\n\nThis is markdown content." &&
                b.Image == imageUrl),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetCreatedDateAndPublishDate()
    {
        // Arrange
        var beforeTest = DateTime.UtcNow.AddSeconds(-1);
        var imageUrl = "https://s3.fr-par.scw.cloud/test-bucket/blog-images/test-image.jpg";
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);

        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            imageStream,
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterTest = DateTime.UtcNow.AddSeconds(1);
        result.CreatedDate.Should().BeAfter(beforeTest);
        result.CreatedDate.Should().BeBefore(afterTest);
        result.PublishDate.Should().BeAfter(beforeTest);
        result.PublishDate.Should().BeBefore(afterTest);
    }

    [Fact]
    public async Task Handle_ShouldUploadImageBeforeSavingBlog()
    {
        // Arrange
        var callSequence = new List<string>();
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("UploadImage"))
            .ReturnsAsync("https://example.com/image.jpg");

        _mockBlogRepository
            .Setup(x => x.AddAsync(MoqIt.IsAny<Blog>(), MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("AddBlog"))
            .ReturnsAsync(new Blog());

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("SaveChanges"))
            .ReturnsAsync(1);

        var command = new CreateBlogCommand(
            "Short",
            "Long",
            imageStream,
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callSequence.Should().Equal("UploadImage", "AddBlog", "SaveChanges");
    }

    [Fact]
    public async Task Handle_ShouldHandleMultipleTags()
    {
        // Arrange
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/image.jpg");

        var tags = new[] { "C#", "Testing", "Unit Tests", "Moq", "FluentAssertions" };
        var command = new CreateBlogCommand(
            "Short",
            "Long",
            imageStream,
            "test.jpg",
            "image/jpeg",
            tags
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Tags.Should().BeEquivalentTo(tags);
        
        _mockBlogRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Blog>(b => b.Tags.Values.SequenceEqual(tags)),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
