using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Infrastructure.Data.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .ValueGeneratedNever();
            
        builder.Property(b => b.ShortDescription)
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(b => b.LongDescription)
            .IsRequired();
            
        builder.Property(b => b.Image)
            .IsRequired();
            
        builder.Property(b => b.CreatedDate)
            .IsRequired();
            
        builder.Property(b => b.PublishDate)
            .IsRequired();
            
        // Configure Tags as JSON column
        builder.OwnsOne(b => b.Tags, tagsBuilder =>
        {
            tagsBuilder.Property(t => t.Values)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .HasColumnName("Tags");
        });
        
        builder.ToTable("Blogs");
    }
}