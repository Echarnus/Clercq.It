using Clercq.It.Application.Features.Certifications.Commands;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class UploadCertificationImageCommandHandlerTests
{
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly UploadCertificationImageCommandHandler _handler;

    public UploadCertificationImageCommandHandlerTests()
    {
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _handler = new UploadCertificationImageCommandHandler(_mockObjectStorageService.Object);
    }

    [Fact]
    public async Task Handle_ShouldUploadImage_AndReturnUrl()
    {
        // Arrange
        var expectedUrl = "https://s3.fr-par.scw.cloud/test-bucket/certifications/test-cert.jpg";
        var imageStream = new MemoryStream();

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var command = new UploadCertificationImageCommand(
            imageStream,
            "test-cert.jpg",
            "image/jpeg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task Handle_ShouldCallObjectStorageService_WithCorrectParameters()
    {
        // Arrange
        var imageStream = new MemoryStream();
        var fileName = "aws-certification.png";
        var contentType = "image/png";

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/image.png");

        var command = new UploadCertificationImageCommand(
            imageStream,
            fileName,
            contentType
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            fileName,
            imageStream,
            contentType,
            true, // isInlineImage should be true for certification images
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetInlineImageToTrue()
    {
        // Arrange
        var imageStream = new MemoryStream();
        var capturedIsInline = false;

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, bool, CancellationToken>((_, _, _, isInline, _) => capturedIsInline = isInline)
            .ReturnsAsync("https://example.com/image.jpg");

        var command = new UploadCertificationImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedIsInline.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var imageStream = new MemoryStream();
        var expectedToken = new CancellationTokenSource().Token;
        CancellationToken capturedToken = default;

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, bool, CancellationToken>((_, _, _, _, token) => capturedToken = token)
            .ReturnsAsync("https://example.com/image.jpg");

        var command = new UploadCertificationImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, expectedToken);

        // Assert
        capturedToken.Should().Be(expectedToken);
    }

    [Theory]
    [InlineData("certification.jpg", "image/jpeg")]
    [InlineData("certification.png", "image/png")]
    [InlineData("certification.gif", "image/gif")]
    [InlineData("certification.webp", "image/webp")]
    public async Task Handle_ShouldAcceptVariousImageFormats(string fileName, string contentType)
    {
        // Arrange
        var imageStream = new MemoryStream();
        var expectedUrl = $"https://example.com/{fileName}";

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                fileName,
                imageStream,
                contentType,
                true,
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var command = new UploadCertificationImageCommand(
            imageStream,
            fileName,
            contentType
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyString_WhenStorageReturnsEmpty()
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
            .ReturnsAsync(string.Empty);

        var command = new UploadCertificationImageCommand(
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
