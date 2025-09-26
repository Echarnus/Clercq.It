using Clercq.It.Application.Features.Projects.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Queries;

public class GetFeaturedProjectsQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly GetFeaturedProjectsQueryHandler _handler;

    public GetFeaturedProjectsQueryHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new GetFeaturedProjectsQueryHandler(_mockProjectRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyFeaturedProjects_WhenFeaturedProjectsExist()
    {
        // Arrange
        var featuredProjects = new List<Project>
        {
            CreateTestProject("Featured Project 1", featured: true),
            CreateTestProject("Featured Project 2", featured: true)
        };

        _mockProjectRepository
            .Setup(x => x.GetFeaturedAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(featuredProjects);

        var query = new GetFeaturedProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Featured);

        var resultList = result.ToList();
        resultList[0].Title.Should().Be("Featured Project 1");
        resultList[1].Title.Should().Be("Featured Project 2");

        _mockProjectRepository.Verify(x => x.GetFeaturedAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoFeaturedProjectsExist()
    {
        // Arrange
        _mockProjectRepository
            .Setup(x => x.GetFeaturedAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var query = new GetFeaturedProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockProjectRepository.Verify(x => x.GetFeaturedAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapFeaturedProjectPropertiesCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-4);
        var endDate = DateTime.UtcNow.AddMonths(-1);
        var skills = new Skills(new[] { "TypeScript", "Next.js", "PostgreSQL" });
        
        var featuredProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Featured Test Project",
            ShortDescription = "Featured short description",
            LongDescription = "Featured long description",
            StartDate = startDate,
            EndDate = endDate,
            Image = "https://example.com/featured.jpg",
            Featured = true,
            Skills = skills
        };

        _mockProjectRepository
            .Setup(x => x.GetFeaturedAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { featuredProject });

        var query = new GetFeaturedProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(featuredProject.Id);
        dto.Title.Should().Be("Featured Test Project");
        dto.ShortDescription.Should().Be("Featured short description");
        dto.LongDescription.Should().Be("Featured long description");
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.Image.Should().Be("https://example.com/featured.jpg");
        dto.Featured.Should().BeTrue();
        dto.Skills.Should().BeEquivalentTo(new[] { "TypeScript", "Next.js", "PostgreSQL" });
    }

    private static Project CreateTestProject(string title, bool featured = false)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortDescription = $"Short description for {title}",
            LongDescription = $"Long description for {title}",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(-3),
            Image = $"https://example.com/{title.ToLower().Replace(" ", "-")}.jpg",
            Featured = featured,
            Skills = new Skills(new[] { "C#", "React" })
        };
    }
}