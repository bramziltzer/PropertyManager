using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(property => property.PropertyId);
        builder.Property(property => property.Name).IsRequired().HasMaxLength(200);
        builder.OwnsOne(property => property.Address, address => address.ConfigureAddress());
        builder.Navigation(property => property.Address).IsRequired();
    }
}
