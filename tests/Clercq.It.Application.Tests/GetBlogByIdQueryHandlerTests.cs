using Clercq.It.Application.Features.Blogs.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Blogs.Queries;

public class GetBlogByIdQueryHandlerTests
{
    private readonly Mock<IBlogRepository> _mockBlogRepository;
    private readonly GetBlogByIdQueryHandler _handler;

    public GetBlogByIdQueryHandlerTests()
    {
        _mockBlogRepository = new Mock<IBlogRepository>();
        _handler = new GetBlogByIdQueryHandler(_mockBlogRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnBlogDto_WhenBlogExists()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var createdDate = DateTime.UtcNow.AddDays(-30);
        var publishDate = DateTime.UtcNow.AddDays(-25);
        var tags = new Tags(new[] { "Development", "Testing", "C#" });

        var blog = new Blog
        {
            Id = blogId,
            ShortDescription = "Short blog description",
            LongDescription = "Long blog description",
            CreatedDate = createdDate,
            PublishDate = publishDate,
            Image = "https://example.com/blog.jpg",
            Tags = tags
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);

        var query = new GetBlogByIdQuery(blogId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(blogId);
        result.ShortDescription.Should().Be("Short blog description");
        result.LongDescription.Should().Be("Long blog description");
        result.CreatedDate.Should().Be(createdDate);
        result.PublishDate.Should().Be(publishDate);
        result.Image.Should().Be("https://example.com/blog.jpg");
        result.Tags.Should().BeEquivalentTo(new[] { "Development", "Testing", "C#" });

        _mockBlogRepository.Verify(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenBlogNotFound()
    {
        // Arrange
        var blogId = Guid.NewGuid();

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Blog?)null);

        var query = new GetBlogByIdQuery(blogId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _mockBlogRepository.Verify(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapTagsCorrectly()
    {
        // Arrange
        var blogId = Guid.NewGuid();
        var expectedTags = new[] { "C#", "DDD", "SOLID", "Testing", "Clean Architecture" };
        var blog = new Blog
        {
            Id = blogId,
            ShortDescription = "Short description",
            LongDescription = "Long description",
            CreatedDate = DateTime.UtcNow,
            PublishDate = DateTime.UtcNow,
            Image = "https://example.com/image.jpg",
            Tags = new Tags(expectedTags)
        };

        _mockBlogRepository
            .Setup(x => x.GetByIdAsync(blogId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);

        var query = new GetBlogByIdQuery(blogId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().HaveCount(5);
        result.Tags.Should().BeEquivalentTo(expectedTags);
    }
}
