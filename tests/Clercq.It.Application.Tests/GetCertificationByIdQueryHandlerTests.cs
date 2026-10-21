using Clercq.It.Application.Features.Certifications.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Application.Tests.Features.Certifications.Queries;

public class GetCertificationByIdQueryHandlerTests
{
    private readonly Mock<ICertificationRepository> _mockCertificationRepository;
    private readonly GetCertificationByIdQueryHandler _handler;

    public GetCertificationByIdQueryHandlerTests()
    {
        _mockCertificationRepository = new Mock<ICertificationRepository>();
        _handler = new GetCertificationByIdQueryHandler(_mockCertificationRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCertificationDto_WhenCertificationExists()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var issueDate = DateTime.UtcNow.AddYears(-1);
        var expiryDate = DateTime.UtcNow.AddYears(2);

        var existingCert = Aggregates.Certification(
            id: certId,
            title: "Azure Expert",
            issuer: "Microsoft",
            issueDate: issueDate,
            expiryDate: expiryDate,
            credentialId: "CERT-123",
            credentialUrl: "https://example.com/cert",
            description: "Test cert",
            image: "https://example.com/cert.jpg");

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var query = new GetCertificationByIdQuery(certId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(certId);
        result.Title.Should().Be("Azure Expert");
        result.Issuer.Should().Be("Microsoft");
        result.IssueDate.Should().Be(issueDate);
        result.ExpiryDate.Should().Be(expiryDate);
        result.CredentialId.Should().Be("CERT-123");
        result.CredentialUrl.Should().Be("https://example.com/cert");
        result.Description.Should().Be("Test cert");
        result.Image.Should().Be("https://example.com/cert.jpg");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCertificationNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(nonExistentId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Certification?)null);

        var query = new GetCertificationByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapNullExpiryDateCorrectly()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = Aggregates.Certification(
            id: certId,
            title: "Lifetime Certification",
            issuer: "Microsoft",
            issueDate: DateTime.UtcNow.AddYears(-1),
            expiryDate: null,
            credentialId: "LIFETIME-001",
            credentialUrl: "https://example.com/cert",
            description: "This certification never expires",
            image: "https://example.com/cert.jpg");

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var query = new GetCertificationByIdQuery(certId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpiryDate.Should().BeNull();
        result.Title.Should().Be("Lifetime Certification");
    }
}
