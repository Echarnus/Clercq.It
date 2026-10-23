namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// The one stored MyZen session (table <c>MyZenSessions</c>, always <see cref="SingletonId"/>).
/// <see cref="Payload"/> holds the cookies and form tokens, encrypted by <see cref="MyZenSessionCipher"/>.
/// </summary>
public class MyZenSessionRecord
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string State { get; set; } = string.Empty;
    public byte[]? Payload { get; set; }
    public DateTimeOffset? ConnectedAt { get; set; }
    public DateTimeOffset? LastSeenAlive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
