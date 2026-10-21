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
            Aggregates.Project(
                id: Guid.NewGuid(),
                title: "Project 1",
                shortDescription: "Short 1",
                longDescription: "Long 1",
                image: "https://example.com/image1.jpg",
                startDate: DateTime.UtcNow.AddYears(-1),
                endDate: DateTime.UtcNow,
                featured: true,
                skills: new Skills(new[] { "C#", ".NET" })),
            Aggregates.Project(
                id: Guid.NewGuid(),
                title: "Project 2",
                shortDescription: "Short 2",
                longDescription: "Long 2",
                image: "https://example.com/image2.jpg",
                startDate: DateTime.UtcNow.AddMonths(-6),
                endDate: DateTime.UtcNow.AddMonths(-3),
                featured: false,
                skills: new Skills(new[] { "Python", "Django" }))
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

        var project = Aggregates.Project(
            id: projectId,
            title: "Full Stack Application",
            shortDescription: "A modern web application",
            longDescription: "# Full Stack Application\n\nBuilt with Angular and Node.js",
            image: "https://storage.example.com/projects/fullstack.png",
            startDate: startDate,
            endDate: endDate,
            featured: true,
            skills: new Skills(skills));

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
            Aggregates.Project(
                id: Guid.NewGuid(),
                title: "Featured Project",
                featured: true,
                skills: new Skills(Array.Empty<string>())),
            Aggregates.Project(
                id: Guid.NewGuid(),
                title: "Regular Project",
                featured: false,
                skills: new Skills(Array.Empty<string>()))
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
        var project = Aggregates.Project(
            id: Guid.NewGuid(),
            title: "Project with no skills",
            skills: new Skills(Array.Empty<string>()));

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
            Aggregates.Project(id: Guid.NewGuid(), title: "Alpha", skills: new Skills(Array.Empty<string>())),
            Aggregates.Project(id: Guid.NewGuid(), title: "Beta", skills: new Skills(Array.Empty<string>())),
            Aggregates.Project(id: Guid.NewGuid(), title: "Gamma", skills: new Skills(Array.Empty<string>()))
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
