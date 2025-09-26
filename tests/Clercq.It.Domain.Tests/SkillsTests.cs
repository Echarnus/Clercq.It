using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.ValueObjects;

public class SkillsTests
{
    [Fact]
    public void Skills_ShouldInitializeWithEmptyArray()
    {
        // Act
        var skills = new Skills(Array.Empty<string>());

        // Assert
        skills.Values.Should().BeEmpty();
        skills.Values.Should().NotBeNull();
    }

    [Fact]
    public void Skills_ShouldInitializeWithValues()
    {
        // Arrange
        var skillValues = new[] { "C#", "React", "Docker" };

        // Act
        var skills = new Skills(skillValues);

        // Assert
        skills.Values.Should().BeEquivalentTo(skillValues);
    }

    [Fact]
    public void Skills_ShouldHandleSingleSkill()
    {
        // Arrange
        var skillValues = new[] { "TypeScript" };

        // Act
        var skills = new Skills(skillValues);

        // Assert
        skills.Values.Should().HaveCount(1);
        skills.Values.Should().Contain("TypeScript");
    }

    [Fact]
    public void Skills_ShouldHandleDuplicateSkills()
    {
        // Arrange
        var skillValues = new[] { "C#", "React", "C#", "Docker", "React" };

        // Act
        var skills = new Skills(skillValues);

        // Assert
        skills.Values.Should().BeEquivalentTo(skillValues);
        skills.Values.Should().HaveCount(5); // Duplicates preserved
    }

    [Fact]
    public void Skills_ShouldHandleNullArray()
    {
        // Act
        var skills = new Skills(null!);

        // Assert
        skills.Values.Should().BeNull();
    }

    [Theory]
    [InlineData("JavaScript", "TypeScript", "React")]
    [InlineData("Python", "Django", "Flask")]
    [InlineData("Java", "Spring Boot")]
    public void Skills_ShouldStoreMultipleSkills(params string[] skillValues)
    {
        // Act
        var skills = new Skills(skillValues);

        // Assert
        skills.Values.Should().BeEquivalentTo(skillValues);
    }
}