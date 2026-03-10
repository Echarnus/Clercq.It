using Clercq.It.Application.Features.Blogs.Commands;

namespace Clercq.It.Application.Tests.Features.Blogs.Commands;

public class UpdateBlogCommandValidatorTests
{
    private readonly UpdateBlogCommandValidator _validator;

    public UpdateBlogCommandValidatorTests()
    {
        _validator = new UpdateBlogCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
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
    public void Validate_ShouldPass_WhenImageIsNull()
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            "Valid short description",
            "Valid long description",
            null,
            null,
            null,
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.Empty,
            "Short description",
            "Long description",
            null,
            null,
            null,
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_ShouldFail_WhenShortDescriptionIsEmpty()
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            "",
            "Long description",
            null,
            null,
            null,
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
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            longDescription,
            "Long description",
            null,
            null,
            null,
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
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            "Short description",
            "",
            null,
            null,
            null,
            new[] { "Tag1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LongDescription");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsAreEmpty()
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            "Short description",
            "Long description",
            null,
            null,
            null,
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
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
            "Short description",
            "Long description",
            null,
            null,
            null,
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

    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.pdf")]
    [InlineData("test.doc")]
    [InlineData("test")]
    [InlineData("test.exe")]
    public void Validate_ShouldFail_WhenImageFileNameHasInvalidExtension(string fileName)
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
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
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    public void Validate_ShouldFail_WhenImageContentTypeIsInvalid(string contentType)
    {
        // Arrange
        var command = new UpdateBlogCommand(
            Guid.NewGuid(),
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
}
