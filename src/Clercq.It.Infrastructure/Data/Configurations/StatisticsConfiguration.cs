using Clercq.It.Application.Features.Statistics;
using Clercq.It.Infrastructure.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clercq.It.Infrastructure.Data.Configurations;

public class StatsSaltConfiguration : IEntityTypeConfiguration<StatsSaltRecord>
{
    public void Configure(EntityTypeBuilder<StatsSaltRecord> builder)
    {
        builder.HasKey(s => s.Day);
        builder.Property(s => s.Salt).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.ToTable("StatsSalts");
    }
}

public class StatsPageVisitConfiguration : IEntityTypeConfiguration<StatsPageVisitRecord>
{
    public void Configure(EntityTypeBuilder<StatsPageVisitRecord> builder)
    {
        builder.HasKey(v => new { v.Day, v.Path, v.VisitorHash });
        builder.Property(v => v.Path).HasMaxLength(RecordPageViewCommandValidator.MaxPathLength).IsRequired();
        builder.Property(v => v.VisitorHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.ToTable("StatsPageVisits");
    }
}

public class StatsDailyPageConfiguration : IEntityTypeConfiguration<StatsDailyPageRecord>
{
    public void Configure(EntityTypeBuilder<StatsDailyPageRecord> builder)
    {
        builder.HasKey(p => new { p.Day, p.Path });
        builder.Property(p => p.Path).HasMaxLength(RecordPageViewCommandValidator.MaxPathLength).IsRequired();
        builder.ToTable("StatsDailyPages");
    }
}

public class StatsDayConfiguration : IEntityTypeConfiguration<StatsDayRecord>
{
    public void Configure(EntityTypeBuilder<StatsDayRecord> builder)
    {
        builder.HasKey(d => d.Day);
        builder.ToTable("StatsDays");
    }
}
