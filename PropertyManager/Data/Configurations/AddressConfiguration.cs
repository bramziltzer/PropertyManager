using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public static class AddressConfiguration
{
    public static OwnedNavigationBuilder<TOwner, Address> ConfigureAddress<TOwner>(
        this OwnedNavigationBuilder<TOwner, Address> builder)
        where TOwner : class
    {
        builder.Property(address => address.Line1).IsRequired().HasMaxLength(200);
        builder.Property(address => address.Line2).HasMaxLength(200);
        builder.Property(address => address.City).IsRequired().HasMaxLength(100);
        builder.Property(address => address.State).IsRequired().HasMaxLength(50);
        builder.Property(address => address.ZipCode).IsRequired().HasMaxLength(10);
        return builder;
    }
}
