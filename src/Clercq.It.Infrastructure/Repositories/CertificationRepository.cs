using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Infrastructure.Data;

namespace Clercq.It.Infrastructure.Repositories;

public class CertificationRepository : Repository<Certification>, ICertificationRepository
{
    public CertificationRepository(ApplicationDbContext context) : base(context)
    {
    }
}
