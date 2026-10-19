using FixMyCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixMyCampus.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.Property(t => t.Room).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(2000);

        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Urgency).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(t => t.Building)
            .WithMany(b => b.Tickets)
            .HasForeignKey(t => t.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Reporter)
            .WithMany(u => u.ReportedTickets)
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedTechnician)
            .WithMany(x => x.AssignedTickets)
            .HasForeignKey(t => t.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.ReporterId);
        builder.HasIndex(t => new { t.BuildingId, t.Status });
        builder.HasIndex(t => new { t.BuildingId, t.Room, t.Category });

        builder.Property<uint>("xmin").IsRowVersion();
    }
}