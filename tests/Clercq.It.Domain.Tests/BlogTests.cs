using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Tests.Entities;

public class BlogTests
{
    [Fact]
    public void Blog_ShouldInitializeWithDefaultValues()
    {
        // Act
        var blog = new Blog();

        // Assert
        blog.Id.Should().BeEmpty();
        blog.ShortDescription.Should().BeEmpty();
        blog.LongDescription.Should().BeEmpty();
        blog.Image.Should().BeEmpty();
        blog.Tags.Should().NotBeNull();
        blog.Tags.Values.Should().BeEmpty();
        blog.CreatedDate.Should().Be(default(DateTime));
        blog.PublishDate.Should().Be(default(DateTime));
    }

    [Fact]
    public void Blog_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdDate = DateTime.UtcNow.AddDays(-30);
        var publishDate = DateTime.UtcNow.AddDays(-25);
        var tags = new Tags(new[] { "Development", "Testing", "C#" });

        // Act
        var blog = new Blog
        {
            Id = id,
            ShortDescription = "Short blog description",
            LongDescription = "Long blog description",
            CreatedDate = createdDate,
            PublishDate = publishDate,
            Image = "https://example.com/blog.jpg",
            Tags = tags
        };

        // Assert
        blog.Id.Should().Be(id);
        blog.ShortDescription.Should().Be("Short blog description");
        blog.LongDescription.Should().Be("Long blog description");
        blog.CreatedDate.Should().Be(createdDate);
        blog.PublishDate.Should().Be(publishDate);
        blog.Image.Should().Be("https://example.com/blog.jpg");
        blog.Tags.Should().Be(tags);
        blog.Tags.Values.Should().BeEquivalentTo(new[] { "Development", "Testing", "C#" });
    }

    [Fact]
    public void Blog_ShouldHandleNullTags()
    {
        // Act
        var blog = new Blog
        {
            Tags = null!
        };

        // Assert
        blog.Tags.Should().BeNull();
    }

    [Fact]
    public void Blog_ShouldHandleEmptyTags()
    {
        // Arrange
        var emptyTags = new Tags(Array.Empty<string>());

        // Act
        var blog = new Blog
        {
            Tags = emptyTags
        };

        // Assert
        blog.Tags.Should().NotBeNull();
        blog.Tags.Values.Should().BeEmpty();
    }
}