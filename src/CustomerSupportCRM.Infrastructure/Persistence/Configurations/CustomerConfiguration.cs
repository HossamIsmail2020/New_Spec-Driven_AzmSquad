using CustomerSupportCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupportCRM.Infrastructure.Persistence.Configurations;

/// <summary>FR-003: unique email index is what makes the duplicate-email rejection a database guarantee, not just an application-level check with a race condition.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Company).HasMaxLength(200);
        builder.HasIndex(c => c.Email).IsUnique();

        builder.HasMany(c => c.Tickets)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
