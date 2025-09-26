using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.ValueObjects;

public class TagsTests
{
    [Fact]
    public void Tags_ShouldInitializeWithEmptyArray()
    {
        // Act
        var tags = new Tags(Array.Empty<string>());

        // Assert
        tags.Values.Should().BeEmpty();
        tags.Values.Should().NotBeNull();
    }

    [Fact]
    public void Tags_ShouldInitializeWithValues()
    {
        // Arrange
        var tagValues = new[] { "Development", "Testing", "C#" };

        // Act
        var tags = new Tags(tagValues);

        // Assert
        tags.Values.Should().BeEquivalentTo(tagValues);
    }

    [Fact]
    public void Tags_ShouldHandleSingleTag()
    {
        // Arrange
        var tagValues = new[] { "Programming" };

        // Act
        var tags = new Tags(tagValues);

        // Assert
        tags.Values.Should().HaveCount(1);
        tags.Values.Should().Contain("Programming");
    }

    [Fact]
    public void Tags_ShouldHandleDuplicateTags()
    {
        // Arrange
        var tagValues = new[] { "Frontend", "React", "Frontend", "JavaScript", "React" };

        // Act
        var tags = new Tags(tagValues);

        // Assert
        tags.Values.Should().BeEquivalentTo(tagValues);
        tags.Values.Should().HaveCount(5); // Duplicates preserved
    }

    [Fact]
    public void Tags_ShouldHandleNullArray()
    {
        // Act
        var tags = new Tags(null!);

        // Assert
        tags.Values.Should().BeNull();
    }

    [Theory]
    [InlineData("Backend", "API", "REST")]
    [InlineData("Frontend", "UI", "UX")]
    [InlineData("DevOps", "Docker", "Kubernetes", "CI/CD")]
    public void Tags_ShouldStoreMultipleTags(params string[] tagValues)
    {
        // Act
        var tags = new Tags(tagValues);

        // Assert
        tags.Values.Should().BeEquivalentTo(tagValues);
    }
}