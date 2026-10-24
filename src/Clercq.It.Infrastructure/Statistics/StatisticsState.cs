namespace Clercq.It.Infrastructure.Statistics;

/// <summary>
/// Per-process memory for <see cref="EfSiteStatistics"/>: today's salt, so a page view does not
/// read it every time, and the last day rolled up, so the rollup runs once a day per instance.
/// </summary>
public sealed class StatisticsState
{
    private readonly Lock _gate = new();
    private (DateOnly Day, string Salt)? _salt;
    private DateOnly? _rolledUpBefore;

    public string? SaltFor(DateOnly day)
    {
        lock (_gate)
        {
            return _salt is { } s && s.Day == day ? s.Salt : null;
        }
    }

    public void RememberSalt(DateOnly day, string salt)
    {
        lock (_gate)
        {
            _salt = (day, salt);
        }
    }

    public bool IsRolledUpBefore(DateOnly day)
    {
        lock (_gate)
        {
            return _rolledUpBefore >= day;
        }
    }

    public void RememberRollup(DateOnly day)
    {
        lock (_gate)
        {
            _rolledUpBefore = day;
        }
    }
}
