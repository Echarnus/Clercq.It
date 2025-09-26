using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.Entities;

public class ProjectTests
{
    [Fact]
    public void Project_ShouldInitializeWithDefaultValues()
    {
        // Act
        var project = new Project();

        // Assert
        project.Id.Should().BeEmpty();
        project.Title.Should().BeEmpty();
        project.ShortDescription.Should().BeEmpty();
        project.LongDescription.Should().BeEmpty();
        project.Image.Should().BeEmpty();
        project.Featured.Should().BeFalse();
        project.Skills.Should().NotBeNull();
        project.Skills.Values.Should().BeEmpty();
    }

    [Fact]
    public void Project_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow.AddMonths(-3);
        var skills = new Skills(new[] { "C#", "React", "Docker" });

        // Act
        var project = new Project
        {
            Id = id,
            Title = "Test Project",
            ShortDescription = "Short description",
            LongDescription = "Long description",
            StartDate = startDate,
            EndDate = endDate,
            Image = "https://example.com/image.jpg",
            Featured = true,
            Skills = skills
        };

        // Assert
        project.Id.Should().Be(id);
        project.Title.Should().Be("Test Project");
        project.ShortDescription.Should().Be("Short description");
        project.LongDescription.Should().Be("Long description");
        project.StartDate.Should().Be(startDate);
        project.EndDate.Should().Be(endDate);
        project.Image.Should().Be("https://example.com/image.jpg");
        project.Featured.Should().BeTrue();
        project.Skills.Should().Be(skills);
        project.Skills.Values.Should().BeEquivalentTo(new[] { "C#", "React", "Docker" });
    }

    [Fact]
    public void Project_ShouldHandleNullSkills()
    {
        // Act
        var project = new Project
        {
            Skills = null!
        };

        // Assert
        project.Skills.Should().BeNull();
    }

    [Fact]
    public void Project_ShouldHandleEmptySkills()
    {
        // Arrange
        var emptySkills = new Skills(Array.Empty<string>());

        // Act
        var project = new Project
        {
            Skills = emptySkills
        };

        // Assert
        project.Skills.Should().NotBeNull();
        project.Skills.Values.Should().BeEmpty();
    }
}
