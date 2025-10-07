using Clercq.It.Application.Features.Certifications.Commands;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class CreateCertificationCommandValidatorTests
{
    private readonly CreateCertificationCommandValidator _validator;

    public CreateCertificationCommandValidatorTests()
    {
        _validator = new CreateCertificationCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateCertificationCommand(
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
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "",
            "Amazon Web Services",
            DateTime.UtcNow,
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
        var command = new CreateCertificationCommand(
            longTitle,
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
        var command = new CreateCertificationCommand(
            "Certification Title",
            "",
            DateTime.UtcNow,
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Issuer");
    }

    [Fact]
    public void Validate_ShouldFail_WhenIssueDateIsInFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            futureDate,
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
    public void Validate_ShouldFail_WhenExpiryDateIsBeforeIssueDate()
    {
        // Arrange
        var issueDate = DateTime.UtcNow;
        var expiryDate = issueDate.AddDays(-1);
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            issueDate,
            expiryDate,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
    public void Validate_ShouldPass_WhenExpiryDateIsNull()
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow.AddDays(-1),
            null,
            "",
            "",
            "Description",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionIsEmpty()
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "",
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            longDescription,
            new MemoryStream(),
            "cert.jpg",
            "image/jpeg"
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
    public void Validate_ShouldFail_WhenImageStreamIsNull()
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow,
            null,
            "",
            "",
            "Description",
            null!,
            "cert.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageStream");
    }

    [Theory]
    [InlineData("cert.jpg")]
    [InlineData("cert.jpeg")]
    [InlineData("cert.png")]
    [InlineData("cert.gif")]
    [InlineData("cert.webp")]
    [InlineData("CERT.JPG")]
    public void Validate_ShouldPass_WhenImageFileNameHasValidExtension(string fileName)
    {
        // Arrange
        var command = new CreateCertificationCommand(
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
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("cert.txt")]
    [InlineData("cert.pdf")]
    [InlineData("cert.doc")]
    [InlineData("cert")]
    public void Validate_ShouldFail_WhenImageFileNameHasInvalidExtension(string fileName)
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow,
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
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public void Validate_ShouldPass_WhenImageContentTypeIsValid(string contentType)
    {
        // Arrange
        var command = new CreateCertificationCommand(
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
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    [InlineData("")]
    [InlineData("video/mp4")]
    public void Validate_ShouldFail_WhenImageContentTypeIsInvalid(string contentType)
    {
        // Arrange
        var command = new CreateCertificationCommand(
            "Certification Title",
            "Issuer",
            DateTime.UtcNow,
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
