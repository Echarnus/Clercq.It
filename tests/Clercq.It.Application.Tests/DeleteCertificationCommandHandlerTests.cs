using Clercq.It.Application.Features.Certifications.Commands;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Application.Tests.Features.Certifications.Commands;

public class DeleteCertificationCommandHandlerTests
{
    private readonly Mock<ICertificationRepository> _mockCertificationRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DeleteCertificationCommandHandler _handler;

    public DeleteCertificationCommandHandlerTests()
    {
        _mockCertificationRepository = new Mock<ICertificationRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new DeleteCertificationCommandHandler(
            _mockCertificationRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenCertificationExists()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/cert.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var command = new DeleteCertificationCommand(certId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenCertificationNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(nonExistentId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync((Certification?)null);

        var command = new DeleteCertificationCommand(nonExistentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteOnRepository()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var existingCert = new Certification
        {
            Id = certId,
            Title = "Azure Expert",
            Issuer = "Microsoft",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CredentialId = "CERT-123",
            CredentialUrl = "https://example.com/cert",
            Description = "Test cert",
            Image = "https://example.com/cert.jpg"
        };

        _mockCertificationRepository
            .Setup(x => x.GetByIdAsync(certId, MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCert);

        var command = new DeleteCertificationCommand(certId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCertificationRepository.Verify(x => x.DeleteAsync(
            MoqIt.Is<Certification>(c => c.Id == certId),
            MoqIt.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(MoqIt.IsAny<CancellationToken>()), Times.Once);
    }
}
