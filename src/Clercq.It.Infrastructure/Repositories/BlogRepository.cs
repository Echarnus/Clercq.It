using Microsoft.EntityFrameworkCore;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Infrastructure.Data;

namespace Clercq.It.Infrastructure.Repositories;

public class BlogRepository : Repository<Blog>, IBlogRepository
{
    public BlogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Blog>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.PublishDate <= DateTime.UtcNow).ToListAsync(cancellationToken);
    }
}