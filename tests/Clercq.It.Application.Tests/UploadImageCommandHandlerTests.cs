using Clercq.It.Application.Common.Commands;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Tests.Common.Commands;

public class UploadImageCommandHandlerTests
{
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly UploadImageCommandHandler _handler;

    public UploadImageCommandHandlerTests()
    {
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _handler = new UploadImageCommandHandler(_mockObjectStorageService.Object);
    }

    [Fact]
    public async Task Handle_ShouldUploadImageAndReturnUrl()
    {
        // Arrange
        var expectedUrl = "https://storage.example.com/images/test.jpg";
        var imageStream = new MemoryStream();
        var command = new UploadImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                "test.jpg",
                imageStream,
                "image/jpeg",
                true,
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be(expectedUrl);
        
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "test.jpg",
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
        var command = new UploadImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/images/test.jpg");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            MoqIt.IsAny<string>(),
            MoqIt.IsAny<Stream>(),
            MoqIt.IsAny<string>(),
            true, // Should use isInlineImage: true
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
