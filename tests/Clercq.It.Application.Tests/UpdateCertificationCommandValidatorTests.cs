using Clercq.It.Application.Features.Certifications.Commands;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class UpdateCertificationCommandValidatorTests
{
    private readonly UpdateCertificationCommandValidator _validator;

    public UpdateCertificationCommandValidatorTests()
    {
        _validator = new UpdateCertificationCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "AWS Certified Solutions Architect",
            "Amazon Web Services",
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(2),
            "AWS-SAA-12345",
            "https://aws.amazon.com/verification/12345",
            "Professional certification for AWS Solutions Architect",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenExpiryDateIsNull()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenImageIsNull()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.Empty,
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceeds200Characters()
    {
        // Arrange
        var longTitle = new string('a', 201);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            longTitle,
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIssuerIsEmpty()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Issuer");
    }

    [Fact]
    public void Validate_ShouldFail_WhenIssuerExceeds200Characters()
    {
        // Arrange
        var longIssuer = new string('a', 201);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            longIssuer,
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Issuer" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIssueDateIsFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            futureDate,
            null,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "IssueDate" &&
            e.ErrorMessage.Contains("future"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpiryDateBeforeIssueDate()
    {
        // Arrange
        var issueDate = DateTime.UtcNow.AddDays(-1);
        var expiryDate = issueDate.AddDays(-1);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            issueDate,
            expiryDate,
            "",
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ExpiryDate" &&
            e.ErrorMessage.Contains("after"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionIsEmpty()
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceeds2000Characters()
    {
        // Arrange
        var longDescription = new string('a', 2001);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            longDescription,
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage.Contains("2000"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCredentialIdExceeds200Characters()
    {
        // Arrange
        var longCredentialId = new string('a', 201);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            longCredentialId,
            "",
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "CredentialId" &&
            e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCredentialUrlExceeds500Characters()
    {
        // Arrange
        var longCredentialUrl = new string('a', 501);
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            longCredentialUrl,
            "Description",
            null,
            null,
            null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "CredentialUrl" &&
            e.ErrorMessage.Contains("500"));
    }

    [Theory]
    [InlineData("cert.txt")]
    [InlineData("cert.pdf")]
    [InlineData("cert.doc")]
    [InlineData("cert")]
    public void Validate_ShouldFail_WhenImageFileNameHasInvalidExtension(string fileName)
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            fileName,
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ImageFileName" &&
            e.ErrorMessage.Contains("extension"));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    public void Validate_ShouldFail_WhenImageContentTypeIsInvalid(string contentType)
    {
        // Arrange
        var command = new UpdateCertificationCommand(
            Guid.NewGuid(),
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            contentType
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageContentType");
    }
}
