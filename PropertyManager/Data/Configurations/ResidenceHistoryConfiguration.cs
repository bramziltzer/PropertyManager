using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class ResidenceHistoryConfiguration : IEntityTypeConfiguration<ResidenceHistory>
{
    public void Configure(EntityTypeBuilder<ResidenceHistory> builder)
    {
        builder.HasKey(residence => residence.ResidenceHistoryId);
        builder.Property(residence => residence.LandlordName).IsRequired().HasMaxLength(200);
        builder.Property(residence => residence.LandlordPhone).IsRequired().HasMaxLength(ColumnLengths.PhoneNumber);
        builder.Property(residence => residence.MoveInDate).HasColumnType("date");
        builder.Property(residence => residence.MoveOutDate).HasColumnType("date");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ResidenceHistories_MoveOutDate",
            "[MoveOutDate] IS NULL OR [MoveOutDate] >= [MoveInDate]"));
        builder.HasIndex(residence => new { residence.RentalApplicationId, residence.MoveInDate });
        builder.OwnsOne(residence => residence.Address, address => address.ConfigureAddress());
        builder.Navigation(residence => residence.Address).IsRequired();
    }
}
