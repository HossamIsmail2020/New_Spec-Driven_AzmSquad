using CustomerSupportCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupportCRM.Infrastructure.Persistence.Configurations;

public class TicketHistoryEntryConfiguration : IEntityTypeConfiguration<TicketHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TicketHistoryEntry> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Description).IsRequired().HasMaxLength(500);
        builder.Property(h => h.ActorUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(h => h.TicketId);
    }
}
