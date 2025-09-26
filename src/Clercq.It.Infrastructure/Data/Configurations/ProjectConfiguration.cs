using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .ValueGeneratedNever();
            
        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(p => p.LongDescription)
            .IsRequired();
            
        builder.Property(p => p.Image)
            .IsRequired();
            
        builder.Property(p => p.StartDate)
            .IsRequired();
            
        builder.Property(p => p.EndDate)
            .IsRequired();
            
        builder.Property(p => p.Featured)
            .IsRequired();
            
        // Configure Skills as JSON column
        builder.OwnsOne(p => p.Skills, skillsBuilder =>
        {
            skillsBuilder.Property(s => s.Values)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .HasColumnName("Skills");
        });
        
        builder.ToTable("Projects");
    }
}