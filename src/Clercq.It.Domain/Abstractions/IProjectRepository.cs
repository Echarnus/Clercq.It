using Clercq.It.Domain.Entities;

namespace Clercq.It.Domain.Abstractions;

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetFeaturedAsync(CancellationToken cancellationToken = default);
}