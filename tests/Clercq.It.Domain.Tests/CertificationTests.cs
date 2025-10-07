using Clercq.It.Domain.Entities;

namespace Clercq.It.Domain.Tests.Entities;

public class CertificationTests
{
    [Fact]
    public void Certification_ShouldInitializeWithDefaultValues()
    {
        // Act
        var certification = new Certification();

        // Assert
        certification.Id.Should().BeEmpty();
        certification.Title.Should().BeEmpty();
        certification.Issuer.Should().BeEmpty();
        certification.CredentialId.Should().BeEmpty();
        certification.CredentialUrl.Should().BeEmpty();
        certification.Description.Should().BeEmpty();
        certification.Image.Should().BeEmpty();
        certification.IssueDate.Should().Be(default(DateTime));
        certification.ExpiryDate.Should().BeNull();
    }

    [Fact]
    public void Certification_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var issueDate = DateTime.UtcNow.AddYears(-1);
        var expiryDate = DateTime.UtcNow.AddYears(2);

        // Act
        var certification = new Certification
        {
            Id = id,
            Title = "AWS Certified Solutions Architect",
            Issuer = "Amazon Web Services",
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            CredentialId = "AWS-SAA-12345",
            CredentialUrl = "https://aws.amazon.com/verification/12345",
            Description = "Professional certification for AWS Solutions Architect",
            Image = "https://example.com/cert.jpg"
        };

        // Assert
        certification.Id.Should().Be(id);
        certification.Title.Should().Be("AWS Certified Solutions Architect");
        certification.Issuer.Should().Be("Amazon Web Services");
        certification.IssueDate.Should().Be(issueDate);
        certification.ExpiryDate.Should().Be(expiryDate);
        certification.CredentialId.Should().Be("AWS-SAA-12345");
        certification.CredentialUrl.Should().Be("https://aws.amazon.com/verification/12345");
        certification.Description.Should().Be("Professional certification for AWS Solutions Architect");
        certification.Image.Should().Be("https://example.com/cert.jpg");
    }

    [Fact]
    public void Certification_ShouldHandleNullExpiryDate()
    {
        // Act
        var certification = new Certification
        {
            Title = "Lifetime Certification",
            Issuer = "Test Issuer",
            IssueDate = DateTime.UtcNow,
            ExpiryDate = null,
            Description = "A certification that never expires"
        };

        // Assert
        certification.ExpiryDate.Should().BeNull();
    }

    [Fact]
    public void Certification_ShouldHandleOptionalCredentials()
    {
        // Act
        var certification = new Certification
        {
            Title = "Test Certification",
            Issuer = "Test Issuer",
            IssueDate = DateTime.UtcNow,
            Description = "Test description",
            Image = "https://example.com/cert.jpg",
            CredentialId = "",
            CredentialUrl = ""
        };

        // Assert
        certification.CredentialId.Should().BeEmpty();
        certification.CredentialUrl.Should().BeEmpty();
    }
}
