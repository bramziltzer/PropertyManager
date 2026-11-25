using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class ApplicationStatusTypeConfiguration : IEntityTypeConfiguration<ApplicationStatusType>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusType> builder)
    {
        builder.HasKey(type => type.Status);
        builder.Property(type => type.Status)
            .HasColumnName("ApplicationStatusId")
            .HasConversion<int>()
            .ValueGeneratedNever();
        builder.Property(type => type.Name).IsRequired().HasMaxLength(ColumnLengths.LookupName);
        builder.HasIndex(type => type.Name).IsUnique();
    }
}
