using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class ApplicationStatusChangeConfiguration : IEntityTypeConfiguration<ApplicationStatusChange>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusChange> builder)
    {
        builder.HasKey(change => change.ApplicationStatusChangeId);
        builder.Property(change => change.OldStatus)
            .HasColumnName("OldApplicationStatusId")
            .HasConversion<int>();
        builder.Property(change => change.NewStatus)
            .HasColumnName("NewApplicationStatusId")
            .HasConversion<int>();
        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(change => change.OldStatus)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(change => change.NewStatus)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(change => change.Comment).HasMaxLength(ColumnLengths.Comment);
        builder.Property(change => change.ChangedDate).IsRequired();
        builder.HasIndex(change => new { change.RentalApplicationId, change.ChangedDate });
        builder.HasOne(change => change.ChangeByUser)
            .WithMany()
            .HasForeignKey(change => change.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
