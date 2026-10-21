using Clercq.It.Domain.Entities;

namespace Clercq.It.Domain.Tests.Entities;

public class CertificationTests
{
    private static readonly DateTime Issued = new(2023, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Certification NewCertification() =>
        new(Guid.NewGuid(), "AZ-204", "Microsoft", Issued, null, "", "", "", "img");

    [Fact]
    public void Constructor_WithValidValues_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var certification = new Certification(id, "AZ-204", "Microsoft", Issued, Issued.AddYears(1), "ABC",
            "https://learn.microsoft.com/c", "Developer", "img");

        // Assert
        certification.Id.Should().Be(id);
        certification.Title.Should().Be("AZ-204");
        certification.Issuer.Should().Be("Microsoft");
        certification.IssueDate.Should().Be(Issued);
        certification.ExpiryDate.Should().Be(Issued.AddYears(1));
        certification.CredentialId.Should().Be("ABC");
        certification.CredentialUrl.Should().Be("https://learn.microsoft.com/c");
        certification.Description.Should().Be("Developer");
        certification.Image.Should().Be("img");
    }

    [Fact]
    public void Create_WithoutExpiry_AssignsNewId()
    {
        // Act
        var certification = Certification.Create("AZ-204", "Microsoft", Issued, null, "", "", "", "img");

        // Assert
        certification.Id.Should().NotBeEmpty();
        certification.ExpiryDate.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Microsoft")]
    [InlineData("AZ-204", " ")]
    public void Constructor_WithBlankTitleOrIssuer_Throws(string title, string issuer)
    {
        // Act
        var act = () => new Certification(Guid.NewGuid(), title, issuer, Issued, null, "", "", "", "img");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithExpiryBeforeIssue_ThrowsAndKeepsState()
    {
        // Arrange
        var certification = NewCertification();

        // Act
        var act = () => certification.Update("New", "Microsoft", Issued, Issued.AddDays(-1), "", "", "");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*cannot expire before it was issued*");
        certification.Title.Should().Be("AZ-204");
    }

    [Fact]
    public void Update_WithValidValues_ReplacesContent()
    {
        // Arrange
        var certification = NewCertification();

        // Act
        certification.Update("AZ-305", "Microsoft Learn", Issued.AddDays(1), Issued.AddYears(2), "X", "https://x", "Architect");

        // Assert
        certification.Title.Should().Be("AZ-305");
        certification.Issuer.Should().Be("Microsoft Learn");
        certification.IssueDate.Should().Be(Issued.AddDays(1));
        certification.ExpiryDate.Should().Be(Issued.AddYears(2));
        certification.CredentialId.Should().Be("X");
        certification.CredentialUrl.Should().Be("https://x");
        certification.Description.Should().Be("Architect");
    }

    [Fact]
    public void ChangeImage_WithUrl_ReplacesImage()
    {
        // Arrange
        var certification = NewCertification();

        // Act
        certification.ChangeImage("https://example.com/c.png");

        // Assert
        certification.Image.Should().Be("https://example.com/c.png");
    }
}
