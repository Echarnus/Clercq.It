using Clercq.It.Domain.Entities;

namespace Clercq.It.Domain.Abstractions;

public interface IBlogRepository : IRepository<Blog>
{
    Task<IEnumerable<Blog>> GetPublishedAsync(CancellationToken cancellationToken = default);
}