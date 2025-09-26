using Clercq.It.Application.Features.Blogs.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Blogs.Queries;

public class GetAllBlogsQueryHandlerTests
{
    private readonly Mock<IBlogRepository> _mockBlogRepository;
    private readonly GetAllBlogsQueryHandler _handler;

    public GetAllBlogsQueryHandlerTests()
    {
        _mockBlogRepository = new Mock<IBlogRepository>();
        _handler = new GetAllBlogsQueryHandler(_mockBlogRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllBlogs_WhenBlogsExist()
    {
        // Arrange
        var blogs = new List<Blog>
        {
            CreateTestBlog("Blog 1"),
            CreateTestBlog("Blog 2"),
            CreateTestBlog("Blog 3")
        };

        _mockBlogRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(blogs);

        var query = new GetAllBlogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        var resultList = result.ToList();
        resultList[0].ShortDescription.Should().Contain("Blog 1");
        resultList[1].ShortDescription.Should().Contain("Blog 2");
        resultList[2].ShortDescription.Should().Contain("Blog 3");

        _mockBlogRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoBlogsExist()
    {
        // Arrange
        _mockBlogRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Blog>());

        var query = new GetAllBlogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockBlogRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapBlogPropertiesCorrectly()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddDays(-30);
        var publishDate = DateTime.UtcNow.AddDays(-25);
        var tags = new Tags(new[] { "Development", "Testing", "C#" });
        
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            ShortDescription = "Short blog description",
            LongDescription = "Long blog description",
            CreatedDate = createdDate,
            PublishDate = publishDate,
            Image = "https://example.com/blog.jpg",
            Tags = tags
        };

        _mockBlogRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Blog> { blog });

        var query = new GetAllBlogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(blog.Id);
        dto.ShortDescription.Should().Be("Short blog description");
        dto.LongDescription.Should().Be("Long blog description");
        dto.CreatedDate.Should().Be(createdDate);
        dto.PublishDate.Should().Be(publishDate);
        dto.Image.Should().Be("https://example.com/blog.jpg");
        dto.Tags.Should().BeEquivalentTo(new[] { "Development", "Testing", "C#" });
    }

    [Fact]
    public async Task Handle_ShouldHandleBlogsWithMultipleTags()
    {
        // Arrange
        var blog1 = CreateTestBlog("Blog 1", new[] { "C#", "Testing" });
        var blog2 = CreateTestBlog("Blog 2", new[] { "React", "JavaScript", "Frontend", "TypeScript" });

        _mockBlogRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Blog> { blog1, blog2 });

        var query = new GetAllBlogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList[0].Tags.Should().BeEquivalentTo(new[] { "C#", "Testing" });
        resultList[1].Tags.Should().BeEquivalentTo(new[] { "React", "JavaScript", "Frontend", "TypeScript" });
    }

    private static Blog CreateTestBlog(string identifier, string[]? tags = null)
    {
        return new Blog
        {
            Id = Guid.NewGuid(),
            ShortDescription = $"Short description for {identifier}",
            LongDescription = $"Long description for {identifier}",
            CreatedDate = DateTime.UtcNow.AddDays(-30),
            PublishDate = DateTime.UtcNow.AddDays(-25),
            Image = $"https://example.com/{identifier.ToLower().Replace(" ", "-")}.jpg",
            Tags = new Tags(tags ?? new[] { "Development", "Testing" })
        };
    }
}