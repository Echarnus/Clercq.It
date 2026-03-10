using Clercq.It.Application.Features.Projects.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests.Features.Projects.Commands;

public class UpdateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<IObjectStorageService> _mockObjectStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockObjectStorageService = new Mock<IObjectStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new UpdateProjectCommandHandler(
            _mockProjectRepository.Object,
            _mockObjectStorageService.Object,
            _mockUnitOfWork.Object);
    }

    private static Project CreateExistingProject()
    {
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
        return existingProject;
    }

    [Fact]
    public async Task Handle_ShouldUpdateProject_WithCorrectProperties()
    {
        // Arrange
        var existingProject = CreateExistingProject();
        var projectId = existingProject.Id;
        var startDate = DateTime.UtcNow.AddMonths(-3);
        var endDate = DateTime.UtcNow;

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new UpdateProjectCommand(
            projectId,
            "Updated Project",
            "Updated short desc",
            "Updated long desc",
            null,
            null,
            null,
            startDate,
            endDate,
            true,
            new[] { "C#", "React" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Project");
        result.ShortDescription.Should().Be("Updated short desc");
        result.LongDescription.Should().Be("Updated long desc");
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
        result.Featured.Should().BeTrue();
        result.Skills.Should().BeEquivalentTo(new[] { "C#", "React" });

        _mockProjectRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Project>(p =>
                p.Title == "Updated Project" &&
                p.ShortDescription == "Updated short desc" &&
                p.LongDescription == "Updated long desc"),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenProjectNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new UpdateProjectCommand(
            projectId,
            "Title",
            "Short",
            "Long",
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _mockProjectRepository.Verify(x => x.UpdateAsync(
            MoqIt.IsAny<Project>(),
            MoqIt.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUploadNewImage_WhenImageProvided()
    {
        // Arrange
        var existingProject = CreateExistingProject();
        var projectId = existingProject.Id;
        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var imageFileName = "new-image.jpg";
        var imageContentType = "image/jpeg";
        var uploadedImageUrl = "https://s3.fr-par.scw.cloud/test-bucket/project-images/new-image.jpg";

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        _mockObjectStorageService
            .Setup(x => x.UploadFileAsync(
                imageFileName,
                MoqIt.IsAny<Stream>(),
                imageContentType,
                false,
                MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedImageUrl);

        var command = new UpdateProjectCommand(
            projectId,
            "Title",
            "Short",
            "Long",
            imageStream,
            imageFileName,
            imageContentType,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().Be(uploadedImageUrl);

        _mockObjectStorageService.Verify(x => x.UploadFileAsync(
            imageFileName,
            MoqIt.IsAny<Stream>(),
            imageContentType,
            false,
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepExistingImage_WhenNoImageProvided()
    {
        // Arrange
        var existingProject = CreateExistingProject();
        var projectId = existingProject.Id;
        var originalImage = existingProject.Image;

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new UpdateProjectCommand(
            projectId,
            "Title",
            "Short",
            "Long",
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            new[] { "Skill1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().Be(originalImage);

        _mockObjectStorageService.Verify(
            x => x.UploadFileAsync(
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<Stream>(),
                MoqIt.IsAny<string>(),
                MoqIt.IsAny<bool>(),
                MoqIt.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateFeaturedFlag()
    {
        // Arrange
        var existingProject = CreateExistingProject();
        var projectId = existingProject.Id;

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new UpdateProjectCommand(
            projectId,
            "Title",
            "Short",
            "Long",
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            true,
            new[] { "Skill1" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Featured.Should().BeTrue();

        _mockProjectRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Project>(p => p.Featured == true),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSkills()
    {
        // Arrange
        var existingProject = CreateExistingProject();
        var projectId = existingProject.Id;
        var newSkills = new[] { "C#", ".NET", "React", "TypeScript", "Docker" };

        _mockProjectRepository
            .Setup(x => x.GetByIdAsync(projectId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new UpdateProjectCommand(
            projectId,
            "Title",
            "Short",
            "Long",
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            false,
            newSkills
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Skills.Should().BeEquivalentTo(newSkills);

        _mockProjectRepository.Verify(x => x.UpdateAsync(
            MoqIt.Is<Project>(p => p.Skills.Values.SequenceEqual(newSkills)),
            MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
