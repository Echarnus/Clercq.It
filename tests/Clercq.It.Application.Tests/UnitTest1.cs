using Clercq.It.Application.Features.Projects.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Queries;

public class GetAllProjectsQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly GetAllProjectsQueryHandler _handler;

    public GetAllProjectsQueryHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new GetAllProjectsQueryHandler(_mockProjectRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllProjects_WhenProjectsExist()
    {
        // Arrange
        var projects = new List<Project>
        {
            CreateTestProject("Project 1", featured: true),
            CreateTestProject("Project 2", featured: false),
            CreateTestProject("Project 3", featured: true)
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        var resultList = result.ToList();
        resultList[0].Title.Should().Be("Project 1");
        resultList[0].Featured.Should().BeTrue();
        resultList[1].Title.Should().Be("Project 2");
        resultList[1].Featured.Should().BeFalse();
        resultList[2].Title.Should().Be("Project 3");
        resultList[2].Featured.Should().BeTrue();

        // Verify repository was called
        _mockProjectRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoProjectsExist()
    {
        // Arrange
        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockProjectRepository.Verify(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapProjectPropertiesCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow.AddMonths(-3);
        var skills = new Skills(new[] { "C#", "React", "Docker" });
        
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            ShortDescription = "Short description",
            LongDescription = "Long description",
            StartDate = startDate,
            EndDate = endDate,
            Image = "https://example.com/image.jpg",
            Featured = true,
            Skills = skills
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(project.Id);
        dto.Title.Should().Be("Test Project");
        dto.ShortDescription.Should().Be("Short description");
        dto.LongDescription.Should().Be("Long description");
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.Image.Should().Be("https://example.com/image.jpg");
        dto.Featured.Should().BeTrue();
        dto.Skills.Should().BeEquivalentTo(new[] { "C#", "React", "Docker" });
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
