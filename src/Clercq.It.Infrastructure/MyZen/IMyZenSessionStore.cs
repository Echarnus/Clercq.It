namespace Clercq.It.Infrastructure.MyZen;

/// <summary>Loads and saves the one MyZen session.</summary>
public interface IMyZenSessionStore
{
    /// <returns>The stored session, or null when there is none.</returns>
    Task<MyZenSessionRecord?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(MyZenSessionRecord record, CancellationToken cancellationToken);

    Task DeleteAsync(CancellationToken cancellationToken);
}
