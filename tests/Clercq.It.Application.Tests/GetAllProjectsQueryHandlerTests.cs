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
    public async Task Handle_ShouldReturnAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Project 1",
                ShortDescription = "Short 1",
                LongDescription = "Long 1",
                Image = "https://example.com/image1.jpg",
                StartDate = DateTime.UtcNow.AddYears(-1),
                EndDate = DateTime.UtcNow,
                Featured = true,
                Skills = new Skills(new[] { "C#", ".NET" })
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Project 2",
                ShortDescription = "Short 2",
                LongDescription = "Long 2",
                Image = "https://example.com/image2.jpg",
                StartDate = DateTime.UtcNow.AddMonths(-6),
                EndDate = DateTime.UtcNow.AddMonths(-3),
                Featured = false,
                Skills = new Skills(new[] { "Python", "Django" })
            }
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Title.Should().Be("Project 1");
        resultList[1].Title.Should().Be("Project 2");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoProjects()
    {
        // Arrange
        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapProjectPropertiesCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var skills = new[] { "Angular", "TypeScript", "Node.js" };

        var project = new Project
        {
            Id = projectId,
            Title = "Full Stack Application",
            ShortDescription = "A modern web application",
            LongDescription = "# Full Stack Application\n\nBuilt with Angular and Node.js",
            Image = "https://storage.example.com/projects/fullstack.png",
            StartDate = startDate,
            EndDate = endDate,
            Featured = true,
            Skills = new Skills(skills)
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(projectId);
        dto.Title.Should().Be("Full Stack Application");
        dto.ShortDescription.Should().Be("A modern web application");
        dto.LongDescription.Should().Be("# Full Stack Application\n\nBuilt with Angular and Node.js");
        dto.Image.Should().Be("https://storage.example.com/projects/fullstack.png");
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.Featured.Should().BeTrue();
        dto.Skills.Should().BeEquivalentTo(skills);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var query = new GetAllProjectsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockProjectRepository.Verify(
            x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync(new List<Project>());

        var query = new GetAllProjectsQuery();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockProjectRepository.Verify(
            x => x.GetAllAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnBothFeaturedAndNonFeaturedProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Featured Project",
                Featured = true,
                Skills = new Skills(Array.Empty<string>())
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Regular Project",
                Featured = false,
                Skills = new Skills(Array.Empty<string>())
            }
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList.Should().Contain(p => p.Featured == true);
        resultList.Should().Contain(p => p.Featured == false);
    }

    [Fact]
    public async Task Handle_ShouldHandleProjectWithEmptySkills()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project with no skills",
            Skills = new Skills(Array.Empty<string>())
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPreserveProjectOrder()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = Guid.NewGuid(), Title = "Alpha", Skills = new Skills(Array.Empty<string>()) },
            new() { Id = Guid.NewGuid(), Title = "Beta", Skills = new Skills(Array.Empty<string>()) },
            new() { Id = Guid.NewGuid(), Title = "Gamma", Skills = new Skills(Array.Empty<string>()) }
        };

        _mockProjectRepository
            .Setup(x => x.GetAllAsync(MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        resultList[0].Title.Should().Be("Alpha");
        resultList[1].Title.Should().Be("Beta");
        resultList[2].Title.Should().Be("Gamma");
    }
}
