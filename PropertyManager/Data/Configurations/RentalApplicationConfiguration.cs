using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain;

namespace PropertyManager.Data.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.HasKey(application => application.RentalApplicationId);
        builder.HasAlternateKey(application => new { application.RentalApplicationId, application.RentalUnitId });
        builder.Property(application => application.Status)
            .HasColumnName("ApplicationStatusId")
            .HasConversion<int>();
        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(application => application.Status)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(application => application.CurrentComment);
        builder.HasOne(application => application.Unit)
            .WithMany(unit => unit.Applications)
            .HasForeignKey(application => application.RentalUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(application => application.ApplicantInfo, applicantInfo =>
            new ApplicantInfoConfiguration().Configure(applicantInfo));
        builder.Navigation(application => application.ApplicantInfo).IsRequired(false);
        builder.HasMany(application => application.ResidenceHistories)
            .WithOne(residence => residence.RentalApplication)
            .HasForeignKey(residence => residence.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(application => application.History)
            .WithOne(change => change.RentalApplication)
            .HasForeignKey(change => change.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
