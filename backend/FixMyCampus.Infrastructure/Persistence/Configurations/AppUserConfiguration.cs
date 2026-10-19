using FixMyCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixMyCampus.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.UniversityId).HasMaxLength(30);
        builder.Property(u => u.RejectionReason).HasMaxLength(500);

        builder.Property(u => u.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(u => u.RequestedRole)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.HasIndex(u => u.ApprovalStatus);
        builder.HasIndex(u => u.UniversityId).IsUnique();
    }
}