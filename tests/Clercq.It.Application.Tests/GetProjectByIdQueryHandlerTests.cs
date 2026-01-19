using Clercq.It.Application.Features.Projects.Queries;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Queries;

public class GetProjectByIdQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new GetProjectByIdQueryHandler(_mockProjectRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Test Project",
            ShortDescription = "A test project",
            LongDescription = "# Test Project\n\nThis is a detailed description.",
            Image = "https://example.com/project.jpg",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Featured = true,
            Skills = new Skills(new[] { "C#", "ASP.NET Core", "PostgreSQL" })
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(projectId);
        result.Title.Should().Be("Test Project");
        result.ShortDescription.Should().Be("A test project");
        result.LongDescription.Should().Be("# Test Project\n\nThis is a detailed description.");
        result.Image.Should().Be("https://example.com/project.jpg");
        result.Featured.Should().BeTrue();
        result.Skills.Should().BeEquivalentTo(new[] { "C#", "ASP.NET Core", "PostgreSQL" });
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(nonExistentId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetProjectByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockProjectRepository.Verify(
            x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, cancellationToken))
            .ReturnsAsync((Project?)null);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockProjectRepository.Verify(
            x => x.GetByIdAsync(projectId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapAllProjectPropertiesCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2023, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        var skills = new[] { "React", "TypeScript", "GraphQL", "Node.js" };

        var project = new Project
        {
            Id = projectId,
            Title = "E-Commerce Platform",
            ShortDescription = "Modern e-commerce solution",
            LongDescription = "# E-Commerce Platform\n\n## Features\n- Shopping cart\n- Payment integration\n- Inventory management",
            Image = "https://storage.example.com/ecommerce.png",
            StartDate = startDate,
            EndDate = endDate,
            Featured = false,
            Skills = new Skills(skills)
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(projectId);
        result.Title.Should().Be("E-Commerce Platform");
        result.ShortDescription.Should().Be("Modern e-commerce solution");
        result.LongDescription.Should().Contain("E-Commerce Platform");
        result.Image.Should().Be("https://storage.example.com/ecommerce.png");
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
        result.Featured.Should().BeFalse();
        result.Skills.Should().BeEquivalentTo(skills);
    }

    [Fact]
    public async Task Handle_ShouldHandleProjectWithEmptySkills()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Simple Project",
            Skills = new Skills(Array.Empty<string>())
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldHandleProjectWithEmptyStrings()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "",
            ShortDescription = "",
            LongDescription = "",
            Image = "",
            Skills = new Skills(Array.Empty<string>())
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().BeEmpty();
        result.ShortDescription.Should().BeEmpty();
        result.LongDescription.Should().BeEmpty();
        result.Image.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnFeaturedProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Featured Project",
            Featured = true,
            Skills = new Skills(new[] { "Skill1" })
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Featured.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnNonFeaturedProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Regular Project",
            Featured = false,
            Skills = new Skills(new[] { "Skill1" })
        };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Featured.Should().BeFalse();
    }
}
