using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.Entities;

public class ProjectTests
{
    private static readonly DateTime Start = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Project NewProject() =>
        new(Guid.NewGuid(), "Title", "", "Short", "Long", "", Start, Start.AddMonths(3), false, new Skills(["C#"]));

    [Fact]
    public void Constructor_WithValidValues_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var project = new Project(id, "Title", "Acme", "Short", "Long", "img", Start, Start.AddMonths(1), true,
            new Skills(["C#", "SQL"]));

        // Assert
        project.Id.Should().Be(id);
        project.Title.Should().Be("Title");
        project.Company.Should().Be("Acme");
        project.ShortDescription.Should().Be("Short");
        project.LongDescription.Should().Be("Long");
        project.Image.Should().Be("img");
        project.StartDate.Should().Be(Start);
        project.EndDate.Should().Be(Start.AddMonths(1));
        project.Featured.Should().BeTrue();
        project.Skills.Values.Should().Equal("C#", "SQL");
    }

    [Fact]
    public void Create_WithValidValues_AssignsNewId()
    {
        // Act
        var project = Project.Create("Title", "", "Short", "Long", "", Start, Start, false, new Skills([]));

        // Assert
        project.Id.Should().NotBeEmpty();
        project.EndDate.Should().Be(project.StartDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_WithBlankTitle_Throws(string title)
    {
        // Act
        var act = () => new Project(Guid.NewGuid(), title, "", "Short", "Long", "", Start, Start, false, new Skills([]));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithEndBeforeStart_ThrowsAndKeepsState()
    {
        // Arrange
        var project = NewProject();

        // Act
        var act = () => project.Update("New", "", "Short", "Long", Start, Start.AddDays(-1), false, new Skills([]));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*cannot end before it starts*");
        project.Title.Should().Be("Title");
    }

    [Fact]
    public void Update_WithValidValues_ReplacesContent()
    {
        // Arrange
        var project = NewProject();

        // Act
        project.Update("New", "New company", "New short", "New long", Start.AddDays(1), Start.AddDays(2), true, new Skills(["Go"]));

        // Assert
        project.Title.Should().Be("New");
        project.Company.Should().Be("New company");
        project.ShortDescription.Should().Be("New short");
        project.LongDescription.Should().Be("New long");
        project.StartDate.Should().Be(Start.AddDays(1));
        project.EndDate.Should().Be(Start.AddDays(2));
        project.Featured.Should().BeTrue();
        project.Skills.Values.Should().Equal("Go");
    }

    [Fact]
    public void FeatureAndUnfeature_ToggleFeatured()
    {
        // Arrange
        var project = NewProject();

        // Act & Assert
        project.Feature();
        project.Featured.Should().BeTrue();
        project.Unfeature();
        project.Featured.Should().BeFalse();
    }

    [Fact]
    public void ChangeImage_WithUrl_ReplacesImage()
    {
        // Arrange
        var project = NewProject();

        // Act
        project.ChangeImage("https://example.com/p.png");

        // Assert
        project.Image.Should().Be("https://example.com/p.png");
    }
}
