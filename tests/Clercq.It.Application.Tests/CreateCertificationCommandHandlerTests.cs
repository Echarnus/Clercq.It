using Clercq.It.Application.Features.Certifications.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class CreateCertificationCommandHandlerTests
{
    private readonly Mock<ICertificationRepository> _mockCertificationRepository;
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateCertificationCommandHandler _handler;

    public CreateCertificationCommandHandlerTests()
    {
        _mockCertificationRepository = new Mock<ICertificationRepository>();
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new CreateCertificationCommandHandler(
            _mockCertificationRepository.Object,
            _mockObjectStorageService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateCertification_WithCorrectProperties()
    {
        // Arrange
        var imageUrl = "https://storage.example.com/certifications/test-cert.jpg";
        var imageStream = new MemoryStream();
        var issueDate = DateTime.UtcNow.AddMonths(-6);
        var expiryDate = DateTime.UtcNow.AddYears(2);

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);

        var command = new CreateCertificationCommand(
            "AWS Solutions Architect",
            "Amazon Web Services",
            issueDate,
            expiryDate,
            "ABC123XYZ",
            "https://aws.amazon.com/verify/ABC123XYZ",
            "Professional level certification for cloud architecture",
            imageStream,
            "aws-cert.png",
            "image/png"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("AWS Solutions Architect");
        result.Issuer.Should().Be("Amazon Web Services");
        result.IssueDate.Should().Be(issueDate);
        result.ExpiryDate.Should().Be(expiryDate);
        result.CredentialId.Should().Be("ABC123XYZ");
        result.CredentialUrl.Should().Be("https://aws.amazon.com/verify/ABC123XYZ");
        result.Description.Should().Be("Professional level certification for cloud architecture");
        result.Image.Should().Be(imageUrl);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldUploadImageToObjectStorage()
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

        var command = new CreateCertificationCommand(
            "Test Certification",
            "Test Issuer",
            DateTime.UtcNow,
            null,
            "CRED001",
            "https://example.com/verify",
            "Test description",
            imageStream,
            "test-image.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "test-image.jpg",
            imageStream,
            "image/jpeg",
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddCertificationToRepository()
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

        var command = new CreateCertificationCommand(
            "Azure Developer",
            "Microsoft",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(2),
            "AZ204-001",
            "https://microsoft.com/verify/AZ204-001",
            "Azure development certification",
            imageStream,
            "azure-cert.png",
            "image/png"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCertificationRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Certification>(c =>
                c.Title == "Azure Developer" &&
                c.Issuer == "Microsoft" &&
                c.CredentialId == "AZ204-001"),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveChanges()
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

        var command = new CreateCertificationCommand(
            "Test",
            "Test Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "",
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullExpiryDate()
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

        var command = new CreateCertificationCommand(
            "Lifetime Certification",
            "Test Issuer",
            DateTime.UtcNow,
            null, // No expiry date
            "LIFETIME001",
            "https://example.com/verify",
            "This certification never expires",
            imageStream,
            "cert.png",
            "image/png"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ExpiryDate.Should().BeNull();

        _mockCertificationRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Certification>(c => c.ExpiryDate == null),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUploadImageBeforeSavingCertification()
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

        _mockCertificationRepository
            .Setup(x => x.AddAsync(MoqIt.IsAny<Certification>(), MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("AddCertification"))
            .ReturnsAsync(new Certification());

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("SaveChanges"))
            .ReturnsAsync(1);

        var command = new CreateCertificationCommand(
            "Test",
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "",
            imageStream,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callSequence.Should().Equal("UploadImage", "AddCertification", "SaveChanges");
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueIdForCertification()
    {
        // Arrange
        var imageStream1 = new MemoryStream();
        var imageStream2 = new MemoryStream();

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/image.jpg");

        var command1 = new CreateCertificationCommand(
            "Cert 1", "Issuer", DateTime.UtcNow, null, "", "", "",
            imageStream1, "test.jpg", "image/jpeg");

        var command2 = new CreateCertificationCommand(
            "Cert 2", "Issuer", DateTime.UtcNow, null, "", "", "",
            imageStream2, "test.jpg", "image/jpeg");

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        result1.Id.Should().NotBeEmpty();
        result2.Id.Should().NotBeEmpty();
    }
}
