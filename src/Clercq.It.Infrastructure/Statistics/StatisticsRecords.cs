namespace Clercq.It.Infrastructure.Statistics;

/// <summary>
/// The random salt for one UTC day, as hex (table <c>StatsSalts</c>). Deleted once the day is
/// rolled up, so that day's visitor hashes can no longer be recomputed from an IP address.
/// </summary>
public class StatsSaltRecord
{
    public DateOnly Day { get; set; }
    public string Salt { get; set; } = string.Empty;
}

/// <summary>
/// Today's views of one page by one visitor (table <c>StatsPageVisits</c>). The visitor is
/// SHA-256(daily salt, IP address, user agent), as hex. Rows are rolled up into
/// <see cref="StatsDailyPageRecord"/> and <see cref="StatsDayRecord"/> and deleted after the day.
/// </summary>
public class StatsPageVisitRecord
{
    public DateOnly Day { get; set; }
    public string Path { get; set; } = string.Empty;
    public string VisitorHash { get; set; } = string.Empty;
    public int Views { get; set; }
}

/// <summary>Views of one page on one past day (table <c>StatsDailyPages</c>).</summary>
public class StatsDailyPageRecord
{
    public DateOnly Day { get; set; }
    public string Path { get; set; } = string.Empty;
    public long Views { get; set; }
    public long UniqueViews { get; set; }
}

/// <summary>Site totals for one past day (table <c>StatsDays</c>).</summary>
public class StatsDayRecord
{
    public DateOnly Day { get; set; }
    public long Views { get; set; }
    public long UniqueViews { get; set; }
    public long Visitors { get; set; }
}
