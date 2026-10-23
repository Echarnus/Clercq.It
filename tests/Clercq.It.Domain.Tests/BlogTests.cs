using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.Entities;

public class BlogTests
{
    private static readonly DateTime Created = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Blog NewBlog() =>
        new(Guid.NewGuid(), Created, Created.AddDays(1), "Title", "Short", "Long", "https://example.com/a.png", new Tags(["a"]));

    [Fact]
    public void Constructor_WithValidValues_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var blog = new Blog(id, Created, Created.AddDays(5), "Title", "Short", "Long", "https://example.com/a.png",
            new Tags(["Development", "C#"]));

        // Assert
        blog.Id.Should().Be(id);
        blog.CreatedDate.Should().Be(Created);
        blog.PublishDate.Should().Be(Created.AddDays(5));
        blog.Title.Should().Be("Title");
        blog.ShortDescription.Should().Be("Short");
        blog.LongDescription.Should().Be("Long");
        blog.Image.Should().Be("https://example.com/a.png");
        blog.Tags.Values.Should().Equal("Development", "C#");
    }

    [Fact]
    public void Create_WithNow_PublishesImmediatelyWithNewId()
    {
        // Act
        var blog = Blog.Create("Title", "Short", "Long", "img", new Tags([]), Created);

        // Assert
        blog.Id.Should().NotBeEmpty();
        blog.CreatedDate.Should().Be(Created);
        blog.PublishDate.Should().Be(Created);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankShortDescription_Throws(string shortDescription)
    {
        // Act
        var act = () => new Blog(Guid.NewGuid(), Created, Created, "Title", shortDescription, "Long", "img", new Tags([]));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_Throws(string title)
    {
        // Act
        var act = () => new Blog(Guid.NewGuid(), Created, Created, title, "Short", "Long", "img", new Tags([]));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidValues_ReplacesContent()
    {
        // Arrange
        var blog = NewBlog();

        // Act
        blog.Update("New title", "New short", "New long", new Tags(["b", "c"]));

        // Assert
        blog.Title.Should().Be("New title");
        blog.ShortDescription.Should().Be("New short");
        blog.LongDescription.Should().Be("New long");
        blog.Tags.Values.Should().Equal("b", "c");
    }

    [Fact]
    public void Update_WithBlankShortDescription_ThrowsAndKeepsState()
    {
        // Arrange
        var blog = NewBlog();

        // Act
        var act = () => blog.Update("New title", " ", "New long", new Tags([]));

        // Assert
        act.Should().Throw<ArgumentException>();
        blog.ShortDescription.Should().Be("Short");
    }

    [Fact]
    public void Reschedule_WithDates_SetsBothDates()
    {
        // Arrange
        var blog = NewBlog();

        // Act
        blog.Reschedule(Created.AddDays(-1), Created.AddDays(10));

        // Assert
        blog.CreatedDate.Should().Be(Created.AddDays(-1));
        blog.PublishDate.Should().Be(Created.AddDays(10));
    }

    [Fact]
    public void ChangeImage_WithUrl_ReplacesImage()
    {
        // Arrange
        var blog = NewBlog();

        // Act
        blog.ChangeImage("https://example.com/b.png");

        // Assert
        blog.Image.Should().Be("https://example.com/b.png");
    }
}
