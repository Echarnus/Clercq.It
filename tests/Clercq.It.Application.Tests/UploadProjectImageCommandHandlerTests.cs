using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Tests.Features.Projects.Commands;

public class UploadProjectImageCommandHandlerTests
{
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly UploadProjectImageCommandHandler _handler;

    public UploadProjectImageCommandHandlerTests()
    {
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _handler = new UploadProjectImageCommandHandler(_mockObjectStorageService.Object);
    }

    [Fact]
    public async Task Handle_ShouldUploadImageAndReturnUrl()
    {
        // Arrange
        var expectedUrl = "https://s3.fr-par.scw.cloud/test-bucket/project-images/test-image.jpg";
        var imageStream = new MemoryStream();
        
        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                "test-image.jpg",
                imageStream,
                "image/jpeg",
                false,
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var command = new UploadProjectImageCommand(
            imageStream,
            "test-image.jpg",
            "image/jpeg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be(expectedUrl);
        
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "test-image.jpg",
            imageStream,
            "image/jpeg",
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseHeaderImageMode()
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

        var command = new UploadProjectImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            MoqIt.IsAny<string>(),
            MoqIt.IsAny<Stream>(),
            MoqIt.IsAny<string>(),
            false, // Should be header image, not inline
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
