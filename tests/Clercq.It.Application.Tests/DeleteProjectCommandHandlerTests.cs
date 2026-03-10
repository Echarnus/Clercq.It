using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new DeleteProjectCommandHandler(
            _mockProjectRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenProjectExists()
    {
        // Arrange
        var existingProject = new Project
        {
            Title = "Old Project",
            ShortDescription = "Old desc",
            LongDescription = "Old long desc",
            Image = "https://example.com/old.jpg",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow,
            Featured = false
        };
        typeof(Project).GetProperty("Skills")!.SetValue(existingProject, (Skills)new[] { "OldSkill" });
        typeof(Project).GetProperty("Id")!.SetValue(existingProject, Guid.NewGuid());

        var projectId = existingProject.Id;

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new DeleteProjectCommand(projectId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenProjectNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new DeleteProjectCommand(projectId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockProjectRepository.Verify(x => x.DeleteAsync(
            MoqIt.IsAny<Project>(),
            MoqIt.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteOnRepository()
    {
        // Arrange
        var existingProject = new Project
        {
            Title = "Old Project",
            ShortDescription = "Old desc",
            LongDescription = "Old long desc",
            Image = "https://example.com/old.jpg",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow,
            Featured = false
        };
        typeof(Project).GetProperty("Skills")!.SetValue(existingProject, (Skills)new[] { "OldSkill" });
        typeof(Project).GetProperty("Id")!.SetValue(existingProject, Guid.NewGuid());

        var projectId = existingProject.Id;

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new DeleteProjectCommand(projectId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockProjectRepository.Verify(x => x.DeleteAsync(
            MoqIt.Is<Project>(p => p.Id == projectId),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
