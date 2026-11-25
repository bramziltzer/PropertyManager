using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data.Configurations;
using PropertyManager.Domain;

namespace PropertyManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RentalUnit> RentalUnits => Set<RentalUnit>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<ApplicationStatusType> ApplicationStatusTypes => Set<ApplicationStatusType>();
    public DbSet<ResidenceHistory> ResidenceHistories => Set<ResidenceHistory>();
    public DbSet<ApplicationStatusChange> ApplicationStatusChanges => Set<ApplicationStatusChange>();
    public DbSet<RentalLease> RentalLeases => Set<RentalLease>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PropertyConfiguration());
        modelBuilder.ApplyConfiguration(new RentalUnitConfiguration());
        modelBuilder.ApplyConfiguration(new UnitTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicationStatusTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RentalApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new ResidenceHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicationStatusChangeConfiguration());
        modelBuilder.ApplyConfiguration(new RentalLeaseConfiguration());
    }
}
