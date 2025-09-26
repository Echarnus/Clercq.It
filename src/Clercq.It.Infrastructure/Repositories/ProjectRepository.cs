using Microsoft.EntityFrameworkCore;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Infrastructure.Data;

namespace Clercq.It.Infrastructure.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Project>> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.Featured).ToListAsync(cancellationToken);
    }
}