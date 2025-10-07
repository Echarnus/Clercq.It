using Clercq.It.Application.Common.Commands;

namespace Clercq.It.Application.Tests.Common.Commands;

public class UploadImageCommandValidatorTests
{
    private readonly UploadImageCommandValidator _validator;

    public UploadImageCommandValidatorTests()
    {
        _validator = new UploadImageCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new UploadImageCommand(
            new MemoryStream(),
            "test-image.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenImageStreamIsNull()
    {
        // Arrange
        var command = new UploadImageCommand(
            null!,
            "test.jpg",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageStream");
    }

    [Fact]
    public void Validate_ShouldFail_WhenImageFileNameIsEmpty()
    {
        // Arrange
        var command = new UploadImageCommand(
            new MemoryStream(),
            "",
            "image/jpeg"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageFileName");
    }

    [Theory]
    [InlineData("test.jpg")]
    [InlineData("test.jpeg")]
    [InlineData("test.png")]
    [InlineData("test.gif")]
    [InlineData("test.webp")]
    [InlineData("TEST.JPG")]
    [InlineData("TEST.JPEG")]
    public void Validate_ShouldPass_WhenImageFileNameHasValidExtension(string fileName)
    {
        // Arrange
        var command = new UploadImageCommand(
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
    [InlineData("test.txt")]
    [InlineData("test.pdf")]
    [InlineData("test.doc")]
    [InlineData("test")]
    [InlineData("test.exe")]
    public void Validate_ShouldFail_WhenImageFileNameHasInvalidExtension(string fileName)
    {
        // Arrange
        var command = new UploadImageCommand(
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
        var command = new UploadImageCommand(
            new MemoryStream(),
            "test.jpg",
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
        var command = new UploadImageCommand(
            new MemoryStream(),
            "test.jpg",
            contentType
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageContentType");
    }
}
