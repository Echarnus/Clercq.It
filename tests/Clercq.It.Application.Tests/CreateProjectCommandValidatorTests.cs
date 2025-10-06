using Clercq.It.Application.Features.Projects.Commands;

namespace Clercq.It.Application.Tests.Features.Projects.Commands;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator;

    public CreateProjectCommandValidatorTests()
    {
        _validator = new CreateProjectCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Test Project",
            "Valid short description",
            "# Valid Long Description\n\nThis is markdown content.",
            "https://example.com/image.jpg",
            DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddMonths(-3),
            true,
            new[] { "C#", "React" }
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
        var command = new CreateProjectCommand(
            "",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
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
        var command = new CreateProjectCommand(
            longTitle,
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
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
    public void Validate_ShouldFail_WhenShortDescriptionIsEmpty()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
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
        var command = new CreateProjectCommand(
            "Title",
            longDescription,
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
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
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LongDescription");
    }

    [Fact]
    public void Validate_ShouldFail_WhenImageUrlIsEmpty()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    [Theory]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("http://example.com/image.png")]
    [InlineData("https://s3.amazonaws.com/bucket/image.webp")]
    public void Validate_ShouldPass_WhenImageUrlIsValid(string imageUrl)
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            imageUrl,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("//example.com/image.jpg")]
    [InlineData("example.com/image.jpg")]
    public void Validate_ShouldFail_WhenImageUrlIsInvalid(string imageUrl)
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            imageUrl,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(-1);
        
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            startDate,
            endDate,
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "EndDate" && 
            e.ErrorMessage.Contains("greater than or equal"));
    }

    [Fact]
    public void Validate_ShouldPass_WhenEndDateEqualsStartDate()
    {
        // Arrange
        var date = DateTime.UtcNow;
        
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            date,
            date,
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSkillsAreNull()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Skills");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSkillsAreEmpty()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            Array.Empty<string>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Skills" && 
            e.ErrorMessage.Contains("At least one"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSkillsExceedMaximum()
    {
        // Arrange
        var skills = Enumerable.Range(1, 21).Select(i => $"Skill{i}").ToArray();
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            skills
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Skills" && 
            e.ErrorMessage.Contains("Maximum 20"));
    }

    [Fact]
    public void Validate_ShouldPass_WithExactly20Skills()
    {
        // Arrange
        var skills = Enumerable.Range(1, 20).Select(i => $"Skill{i}").ToArray();
        var command = new CreateProjectCommand(
            "Title",
            "Short description",
            "Long description",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            skills
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
