using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.HasKey(type => type.Kind);
        builder.Property(type => type.Kind)
            .HasColumnName("UnitTypeId")
            .HasConversion<int>()
            .ValueGeneratedNever();
        builder.Property(type => type.Name).IsRequired().HasMaxLength(ColumnLengths.LookupName);
        builder.Property(type => type.EndDate).HasColumnType("date");
        builder.HasIndex(type => type.Name).IsUnique();
    }
}
