using Clercq.It.Application.Features.Blogs.Commands;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class CreateBlogCommandValidatorTests
{
    private readonly CreateBlogCommandValidator _validator;

    public CreateBlogCommandValidatorTests()
    {
        _validator = new CreateBlogCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "Valid short description",
            "# Valid Long Description\n\nThis is markdown content.",
            new MemoryStream(),
            "test-image.jpg",
            "image/jpeg",
            new[] { "Development", "Testing" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenShortDescriptionIsEmpty()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShortDescription");
    }

    [Fact]
    public void Validate_ShouldFail_WhenShortDescriptionExceeds500Characters()
    {
        // Arrange
        var longDescription = new string('a', 501);
        var command = new CreateBlogCommand(
            longDescription,
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "ShortDescription" && 
            e.ErrorMessage.Contains("500"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLongDescriptionIsEmpty()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "Short description",
            "",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LongDescription");
    }

    [Fact]
    public void Validate_ShouldFail_WhenImageStreamIsNull()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            null!,
            "test.jpg",
            "image/jpeg",
            new[] { "Tag1" }
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
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "",
            "image/jpeg",
            new[] { "Tag1" }
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
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            fileName,
            "image/jpeg",
            new[] { "Tag1" }
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
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            fileName,
            "image/jpeg",
            new[] { "Tag1" }
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
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            contentType,
            new[] { "Tag1" }
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
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            contentType,
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageContentType");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsAreNull()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tags");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsAreEmpty()
    {
        // Arrange
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            Array.Empty<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Tags" && 
            e.ErrorMessage.Contains("At least one"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsExceedMaximum()
    {
        // Arrange
        var tags = Enumerable.Range(1, 11).Select(i => $"Tag{i}").ToArray();
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            tags
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Tags" && 
            e.ErrorMessage.Contains("Maximum 10"));
    }

    [Fact]
    public void Validate_ShouldPass_WithExactly10Tags()
    {
        // Arrange
        var tags = Enumerable.Range(1, 10).Select(i => $"Tag{i}").ToArray();
        var command = new CreateBlogCommand(
            "Short description",
            "Long description",
            new MemoryStream(),
            "test.jpg",
            "image/jpeg",
            tags
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
