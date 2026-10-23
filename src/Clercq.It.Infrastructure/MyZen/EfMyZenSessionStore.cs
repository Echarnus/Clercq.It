using Clercq.It.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clercq.It.Infrastructure.MyZen;

public sealed class EfMyZenSessionStore(ApplicationDbContext db) : IMyZenSessionStore
{
    public Task<MyZenSessionRecord?> LoadAsync(CancellationToken cancellationToken) =>
        db.MyZenSessions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == MyZenSessionRecord.SingletonId, cancellationToken);

    public async Task SaveAsync(MyZenSessionRecord record, CancellationToken cancellationToken)
    {
        record.Id = MyZenSessionRecord.SingletonId;
        var existing = await db.MyZenSessions
            .SingleOrDefaultAsync(s => s.Id == MyZenSessionRecord.SingletonId, cancellationToken);
        if (existing is null)
        {
            db.MyZenSessions.Add(record);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(record);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(CancellationToken cancellationToken) =>
        db.MyZenSessions.Where(s => s.Id == MyZenSessionRecord.SingletonId).ExecuteDeleteAsync(cancellationToken);
}
