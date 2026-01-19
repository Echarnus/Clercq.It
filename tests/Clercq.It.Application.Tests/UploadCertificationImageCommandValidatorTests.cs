using Clercq.It.Application.Features.Certifications.Commands;
using FluentValidation.TestHelper;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class UploadCertificationImageCommandValidatorTests
{
    private readonly UploadCertificationImageCommandValidator _validator;

    public UploadCertificationImageCommandValidatorTests()
    {
        _validator = new UploadCertificationImageCommandValidator();
    }

    #region ImageStream Validation

    [Fact]
    public void Validate_ShouldFail_WhenImageStreamIsNull()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            null!,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageStream)
            .WithErrorMessage("Image is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenImageStreamIsValid()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageStream);
    }

    #endregion

    #region ImageFileName Validation

    [Fact]
    public void Validate_ShouldFail_WhenFileNameIsEmpty()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            string.Empty,
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageFileName)
            .WithErrorMessage("Image file name is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFileNameIsNull()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            null!,
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageFileName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenFileNameIsWhitespace()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "   ",
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageFileName);
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("document.pdf")]
    [InlineData("script.js")]
    [InlineData("style.css")]
    [InlineData("data.json")]
    [InlineData("archive.zip")]
    [InlineData("image.bmp")]
    [InlineData("image.svg")]
    [InlineData("noextension")]
    public void Validate_ShouldFail_WhenFileExtensionIsInvalid(string fileName)
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            fileName,
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageFileName)
            .WithErrorMessage("Invalid image file extension. Only jpg, jpeg, png, gif, and webp are allowed");
    }

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("image.jpeg")]
    [InlineData("image.png")]
    [InlineData("image.gif")]
    [InlineData("image.webp")]
    [InlineData("IMAGE.JPG")]
    [InlineData("IMAGE.JPEG")]
    [InlineData("IMAGE.PNG")]
    [InlineData("IMAGE.GIF")]
    [InlineData("IMAGE.WEBP")]
    [InlineData("my-certification.jpg")]
    [InlineData("aws_cert_2024.png")]
    public void Validate_ShouldPass_WhenFileExtensionIsValid(string fileName)
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            fileName,
            "image/jpeg"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageFileName);
    }

    #endregion

    #region ImageContentType Validation

    [Fact]
    public void Validate_ShouldFail_WhenContentTypeIsEmpty()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            string.Empty
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageContentType)
            .WithErrorMessage("Image content type is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentTypeIsNull()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            null!
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageContentType);
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentTypeIsWhitespace()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            "   "
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageContentType);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    [InlineData("application/pdf")]
    [InlineData("text/html")]
    [InlineData("application/javascript")]
    [InlineData("application/octet-stream")]
    public void Validate_ShouldFail_WhenContentTypeIsNotImage(string contentType)
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            contentType
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageContentType)
            .WithErrorMessage("Invalid image content type. Only image types are allowed");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("image/svg+xml")]
    [InlineData("image/bmp")]
    [InlineData("IMAGE/JPEG")]
    [InlineData("Image/Png")]
    public void Validate_ShouldPass_WhenContentTypeIsImage(string contentType)
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "test.jpg",
            contentType
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageContentType);
    }

    #endregion

    #region Full Validation

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            "aws-certification.png",
            "image/png"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllFieldsAreInvalid()
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            null!,
            "document.pdf",
            "application/pdf"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageStream);
        result.ShouldHaveValidationErrorFor(x => x.ImageFileName);
        result.ShouldHaveValidationErrorFor(x => x.ImageContentType);
    }

    [Theory]
    [InlineData("certification.jpg", "image/jpeg")]
    [InlineData("certification.jpeg", "image/jpeg")]
    [InlineData("certification.png", "image/png")]
    [InlineData("certification.gif", "image/gif")]
    [InlineData("certification.webp", "image/webp")]
    public void Validate_ShouldPass_ForCommonImageCombinations(string fileName, string contentType)
    {
        // Arrange
        var command = new UploadCertificationImageCommand(
            new MemoryStream(),
            fileName,
            contentType
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
