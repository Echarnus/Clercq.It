using Clercq.It.Application.Features.Certifications.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class UpdateCertificationCommandHandlerTests
{
    private readonly Mock<ICertificationRepository> _mockCertificationRepository;
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateCertificationCommandHandler _handler;

    public UpdateCertificationCommandHandlerTests()
    {
        _mockCertificationRepository = new Mock<ICertificationRepository>();
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new UpdateCertificationCommandHandler(
            _mockCertificationRepository.Object,
            _mockObjectStorageService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCertification_WithCorrectProperties()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/cert.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var issueDate = DateTime.UtcNow.AddMonths(-3);
        var expiryDate = DateTime.UtcNow.AddYears(3);

        var command = new UpdateCertificationCommand(
            certId,
            "AWS Solutions Architect",
            "Amazon Web Services",
            issueDate,
            expiryDate,
            "AWS-001",
            "https://aws.amazon.com/verify/AWS-001",
            "Updated description",
            null,
            null,
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(certId);
        result.Title.Should().Be("AWS Solutions Architect");
        result.Issuer.Should().Be("Amazon Web Services");
        result.IssueDate.Should().Be(issueDate);
        result.ExpiryDate.Should().Be(expiryDate);
        result.CredentialId.Should().Be("AWS-001");
        result.CredentialUrl.Should().Be("https://aws.amazon.com/verify/AWS-001");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCertificationNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(nonExistentId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Certification?)null);

        var command = new UpdateCertificationCommand(
            nonExistentId,
            "Title",
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldUploadNewImage_WhenImageProvided()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/old-image.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var newImageUrl = "https://storage.example.com/new-image.png";
        var imageStream = new MemoryStream();

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(newImageUrl);

        var command = new UpdateCertificationCommand(
            certId,
            "Azure Expert",
            "Microsoft",
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(2),
            "CERT-123",
            "https://example.com/cert",
            "Test cert",
            imageStream,
            "new-cert.png",
            "image/png"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().Be(newImageUrl);

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            "new-cert.png",
            imageStream,
            "image/png",
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepExistingImage_WhenNoImageProvided()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingImageUrl = "https://example.com/existing-image.jpg";
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = existingImageUrl
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var command = new UpdateCertificationCommand(
            certId,
            "Azure Expert",
            "Microsoft",
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(2),
            "CERT-123",
            "https://example.com/cert",
            "Test cert",
            null,
            null,
            null
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
    public async Task Handle_ShouldUpdateExpiryDate()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/cert.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var newExpiryDate = DateTime.UtcNow.AddYears(5);

        var command = new UpdateCertificationCommand(
            certId,
            "Azure Expert",
            "Microsoft",
            DateTime.UtcNow.AddYears(-1),
            newExpiryDate,
            "CERT-123",
            "https://example.com/cert",
            "Test cert",
            null,
            null,
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpiryDate.Should().Be(newExpiryDate);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullExpiryDate()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/cert.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var command = new UpdateCertificationCommand(
            certId,
            "Lifetime Certification",
            "Microsoft",
            DateTime.UtcNow.AddYears(-1),
            null,
            "CERT-123",
            "https://example.com/cert",
            "This certification never expires",
            null,
            null,
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpiryDate.Should().BeNull();

        _mockCertificationRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Certification>(c => c.ExpiryDate == null),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
