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
        
        var certification = new Certification
        {
            Id = Guid.NewGuid(),
            Title = "AWS Certified Solutions Architect",
            Issuer = "Amazon Web Services",
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            CredentialId = "AWS-SAA-12345",
            CredentialUrl = "https://aws.amazon.com/verification/12345",
            Description = "Professional certification for AWS Solutions Architect",
            Image = "https://example.com/cert.jpg"
        };

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
        var cert = new Certification
        {
            Id = Guid.NewGuid(),
            Title = "Test Certification",
            Issuer = "Test Issuer",
            IssueDate = DateTime.UtcNow,
            ExpiryDate = null,
            CredentialId = "",
            CredentialUrl = "",
            Description = "Test description",
            Image = "https://example.com/cert.jpg"
        };

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
        return new Certification
        {
            Id = Guid.NewGuid(),
            Title = title,
            Issuer = "Test Issuer",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = hasExpiry ? DateTime.UtcNow.AddYears(2) : null,
            CredentialId = "TEST-12345",
            CredentialUrl = "https://example.com/verification/12345",
            Description = $"Description for {title}",
            Image = $"https://example.com/{title.ToLower().Replace(" ", "-")}.jpg"
        };
    }
}
