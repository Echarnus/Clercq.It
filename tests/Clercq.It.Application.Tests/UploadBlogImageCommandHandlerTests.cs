using Clercq.It.Application.Features.Blogs.Commands;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class UploadBlogImageCommandHandlerTests
{
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly UploadBlogImageCommandHandler _handler;

    public UploadBlogImageCommandHandlerTests()
    {
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _handler = new UploadBlogImageCommandHandler(_mockObjectStorageService.Object);
    }

    [Fact]
    public async Task Handle_ShouldUploadImageAndReturnUrl()
    {
        // Arrange
        var imageUrl = "https://s3.fr-par.scw.cloud/test-bucket/blog-images/inline/test-image.jpg";
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);

        var command = new UploadBlogImageCommand(
            imageStream,
            "test-image.jpg",
            "image/jpeg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be(imageUrl);

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "test-image.jpg",
            imageStream,
            "image/jpeg",
            true,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseInlineImageFlag()
    {
        // Arrange
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                true,
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/inline-image.jpg");

        var command = new UploadBlogImageCommand(
            imageStream,
            "inline.png",
            "image/png"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "inline.png",
            imageStream,
            "image/png",
            true,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHandleDifferentImageFormats()
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
            .ReturnsAsync("https://example.com/image.webp");

        var command = new UploadBlogImageCommand(
            imageStream,
            "diagram.webp",
            "image/webp"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Url.Should().Be("https://example.com/image.webp");
    }
}
