using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class RentalUnitConfiguration : IEntityTypeConfiguration<RentalUnit>
{
    public void Configure(EntityTypeBuilder<RentalUnit> builder)
    {
        builder.HasKey(unit => unit.RentalUnitId);
        builder.Property(unit => unit.UnitNumber).IsRequired().HasMaxLength(50);
        builder.Property(unit => unit.MonthlyRent).HasPrecision(18, 2);
        builder.Property(unit => unit.Kind)
            .HasColumnName("UnitTypeId")
            .HasConversion<int>();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_RentalUnits_MonthlyRent", "[MonthlyRent] >= 0");
            table.HasCheckConstraint("CK_RentalUnits_Bedrooms", "[Bedrooms] >= 0");
        });
        builder.HasIndex(unit => new { unit.PropertyId, unit.UnitNumber }).IsUnique();
        builder.HasOne(unit => unit.Property)
            .WithMany(property => property.Units)
            .HasForeignKey(unit => unit.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(unit => unit.UnitType)
            .WithMany()
            .HasForeignKey(unit => unit.Kind)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
