using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class RentalLeaseConfiguration : IEntityTypeConfiguration<RentalLease>
{
    public void Configure(EntityTypeBuilder<RentalLease> builder)
    {
        builder.HasKey(lease => lease.LeaseId);
        builder.HasOne(lease => lease.RentalApplication)
            .WithOne(application => application.Lease)
            .HasForeignKey<RentalLease>(lease => new { lease.RentalApplicationId, lease.RentalUnitId })
            .HasPrincipalKey<RentalApplication>(application => new { application.RentalApplicationId, application.RentalUnitId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(lease => lease.RentalUnit)
            .WithMany(unit => unit.Leases)
            .HasForeignKey(lease => lease.RentalUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(lease => lease.Lessee)
            .WithMany()
            .HasForeignKey(lease => lease.LesseeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(lease => lease.BeginDate).HasColumnType("date");
        builder.Property(lease => lease.EndDate).HasColumnType("date");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RentalLeases_EndDate",
            "[EndDate] > [BeginDate]"));
        builder.HasIndex(lease => new { lease.RentalUnitId, lease.BeginDate, lease.EndDate });
    }
}
