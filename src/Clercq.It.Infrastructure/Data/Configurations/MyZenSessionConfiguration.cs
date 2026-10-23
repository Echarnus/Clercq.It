using Clercq.It.Infrastructure.MyZen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clercq.It.Infrastructure.Data.Configurations;

public class MyZenSessionConfiguration : IEntityTypeConfiguration<MyZenSessionRecord>
{
    public void Configure(EntityTypeBuilder<MyZenSessionRecord> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.State)
            .HasMaxLength(20)
            .IsRequired();

        builder.ToTable("MyZenSessions");
    }
}
