using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class ApplicantInfoConfiguration
{
    public void Configure(OwnedNavigationBuilder<RentalApplication, ApplicantInfo> builder)
    {
        builder.ToTable("ApplicantInfos");
        builder.Property(info => info.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(info => info.LastName).IsRequired().HasMaxLength(100);
        builder.Property(info => info.PhoneNumber).IsRequired().HasMaxLength(ColumnLengths.PhoneNumber);
        builder.Property(info => info.Email).IsRequired().HasMaxLength(256);
        builder.OwnsOne(info => info.CurrentAddress, address => address.ConfigureAddress());
        builder.Navigation(info => info.CurrentAddress).IsRequired();
    }
}
