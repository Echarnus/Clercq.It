using Clercq.It.Application.Features.Certifications.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Application.Tests.Features.Certifications.Queries;

public class GetAllCertificationsQueryHandlerTests
{
    private readonly Mock<ICertificationRepository> _mockCertificationRepository;
    private readonly GetAllCertificationsQueryHandler _handler;

    public GetAllCertificationsQueryHandlerTests()
    {
        _mockCertificationRepository = new Mock<ICertificationRepository>();
        _handler = new GetAllCertificationsQueryHandler(_mockCertificationRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllCertifications_WhenCertificationsExist()
    {
        // Arrange
        var certifications = new List<Certification>
        {
            CreateTestCertification("AWS Certified Solutions Architect"),
            CreateTestCertification("Microsoft Azure Administrator"),
            CreateTestCertification("Google Cloud Professional")
        };

        _mockCertificationRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(certifications);

        var query = new GetAllCertificationsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var resultList = result.ToList();
        resultList[0].Title.Should().Be("AWS Certified Solutions Architect");
        resultList[1].Title.Should().Be("Microsoft Azure Administrator");
        resultList[2].Title.Should().Be("Google Cloud Professional");

        _mockCertificationRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoCertificationsExist()
    {
        // Arrange
        _mockCertificationRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Certification>());

        var query = new GetAllCertificationsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockCertificationRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapCertificationPropertiesCorrectly()
    {
        // Arrange
        var issueDate = DateTime.UtcNow.AddYears(-1);
        var expiryDate = DateTime.UtcNow.AddYears(2);

        var certification = Aggregates.Certification(
            id: Guid.NewGuid(),
            title: "AWS Certified Solutions Architect",
            issuer: "Amazon Web Services",
            issueDate: issueDate,
            expiryDate: expiryDate,
            credentialId: "AWS-SAA-12345",
            credentialUrl: "https://aws.amazon.com/verification/12345",
            description: "Professional certification for AWS Solutions Architect",
            image: "https://example.com/cert.jpg");

        _mockCertificationRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Certification> { certification });

        var query = new GetAllCertificationsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(certification.Id);
        dto.Title.Should().Be("AWS Certified Solutions Architect");
        dto.Issuer.Should().Be("Amazon Web Services");
        dto.IssueDate.Should().Be(issueDate);
        dto.ExpiryDate.Should().Be(expiryDate);
        dto.CredentialId.Should().Be("AWS-SAA-12345");
        dto.CredentialUrl.Should().Be("https://aws.amazon.com/verification/12345");
        dto.Description.Should().Be("Professional certification for AWS Solutions Architect");
        dto.Image.Should().Be("https://example.com/cert.jpg");
    }

    [Fact]
    public async Task Handle_ShouldHandleCertificationsWithNullExpiryDate()
    {
        // Arrange
        var cert1 = CreateTestCertification("Lifetime Cert", hasExpiry: false);
        var cert2 = CreateTestCertification("Expiring Cert", hasExpiry: true);

        _mockCertificationRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Certification> { cert1, cert2 });

        var query = new GetAllCertificationsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList[0].ExpiryDate.Should().BeNull();
        resultList[1].ExpiryDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldHandleCertificationsWithOptionalCredentials()
    {
        // Arrange
        var cert = Aggregates.Certification(
            id: Guid.NewGuid(),
            title: "Test Certification",
            issuer: "Test Issuer",
            issueDate: DateTime.UtcNow,
            expiryDate: null,
            credentialId: "",
            credentialUrl: "",
            description: "Test description",
            image: "https://example.com/cert.jpg");

        _mockCertificationRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Certification> { cert });

        var query = new GetAllCertificationsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.CredentialId.Should().BeEmpty();
        dto.CredentialUrl.Should().BeEmpty();
    }

    private static Certification CreateTestCertification(string title, bool hasExpiry = true)
    {
        return Aggregates.Certification(
            id: Guid.NewGuid(),
            title: title,
            issuer: "Test Issuer",
            issueDate: DateTime.UtcNow.AddYears(-1),
            expiryDate: hasExpiry ? DateTime.UtcNow.AddYears(2) : null,
            credentialId: "TEST-12345",
            credentialUrl: "https://example.com/verification/12345",
            description: $"Description for {title}",
            image: $"https://example.com/{title.ToLower().Replace(" ", "-")}.jpg");
    }
}
