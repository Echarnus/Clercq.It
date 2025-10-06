using Clercq.It.Application.Features.Blogs.Commands;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class UploadBlogImageCommandValidatorTests
{
    private readonly UploadBlogImageCommandValidator _validator;

    public UploadBlogImageCommandValidatorTests()
    {
        _validator = new UploadBlogImageCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new UploadBlogImageCommand(
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
        var command = new UploadBlogImageCommand(
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
        var command = new UploadBlogImageCommand(
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
    public void Validate_ShouldPass_WhenImageFileNameHasValidExtension(string fileName)
    {
        // Arrange
        var command = new UploadBlogImageCommand(
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
    public void Validate_ShouldFail_WhenImageFileNameHasInvalidExtension(string fileName)
    {
        // Arrange
        var command = new UploadBlogImageCommand(
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
        var command = new UploadBlogImageCommand(
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
    public void Validate_ShouldFail_WhenImageContentTypeIsInvalid(string contentType)
    {
        // Arrange
        var command = new UploadBlogImageCommand(
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
