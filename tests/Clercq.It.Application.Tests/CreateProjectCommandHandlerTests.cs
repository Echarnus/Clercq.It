using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        _handler = new CreateProjectCommandHandler(
            _mockProjectRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateProject_WithCorrectProperties()
    {
        // Arrange
        var imageUrl = "https://s3.fr-par.scw.cloud/test-bucket/project-images/test-image.jpg";
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow.AddMonths(-3);

        var command = new CreateProjectCommand(
            "Test Project",
            "Short description",
            "# Long Description\n\nThis is markdown content.",
            imageUrl,
            startDate,
            endDate,
            true,
            new[] { "C#", "React" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Project");
        result.ShortDescription.Should().Be("Short description");
        result.LongDescription.Should().Be("# Long Description\n\nThis is markdown content.");
        result.Image.Should().Be(imageUrl);
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
        result.Featured.Should().BeTrue();
        result.Skills.Should().BeEquivalentTo(new[] { "C#", "React" });
        result.Id.Should().NotBeEmpty();

        _mockProjectRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Project>(p => 
                p.Title == "Test Project" &&
                p.ShortDescription == "Short description" &&
                p.LongDescription == "# Long Description\n\nThis is markdown content." &&
                p.Image == imageUrl &&
                p.Featured == true),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveProjectToRepository()
    {
        // Arrange
        var callSequence = new List<string>();
        
        _mockProjectRepository
            .Setup(x => x.AddAsync(MoqIt.IsAny<Project>(), MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("AddProject"))
            .ReturnsAsync(new Project());

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()))
            .Callback(() => callSequence.Add("SaveChanges"))
            .ReturnsAsync(1);

        var command = new CreateProjectCommand(
            "Title",
            "Short",
            "Long",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callSequence.Should().Equal("AddProject", "SaveChanges");
    }

    [Fact]
    public async Task Handle_ShouldHandleMultipleSkills()
    {
        // Arrange
        var skills = new[] { "C#", ".NET", "React", "TypeScript", "Docker" };
        var command = new CreateProjectCommand(
            "Title",
            "Short",
            "Long",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            skills
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Skills.Should().BeEquivalentTo(skills);
        
        _mockProjectRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Project>(p => p.Skills.Values.SequenceEqual(skills)),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetFeaturedFlagCorrectly()
    {
        // Arrange
        var command = new CreateProjectCommand(
            "Title",
            "Short",
            "Long",
            "https://example.com/image.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            true, // Featured
            new[] { "Skill1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Featured.Should().BeTrue();
        
        _mockProjectRepository.Verify(x => x.AddAsync(
            MoqIt.Is<Project>(p => p.Featured == true),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
