using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Infrastructure.Data.Configurations;

public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
    public void Configure(EntityTypeBuilder<Certification> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id)
            .ValueGeneratedNever();
            
        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(c => c.Issuer)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(c => c.IssueDate)
            .IsRequired();
            
        builder.Property(c => c.ExpiryDate)
            .IsRequired(false);
            
        builder.Property(c => c.CredentialId)
            .HasMaxLength(200);
            
        builder.Property(c => c.CredentialUrl)
            .HasMaxLength(500);
            
        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .IsRequired();
            
        builder.Property(c => c.Image)
            .IsRequired();
        
        builder.ToTable("Certifications");
    }
}
