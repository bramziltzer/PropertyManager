using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Domain;

namespace PropertyManager.Tests.Domain;

public sealed class OrphanDeletionTests
{
    [Fact]
    public void RemovingAResidenceFromTheApplication_MarksItDeleted()
    {
        using var db = CreateContext();
        var application = TestData.Completed();
        var residence = application.ResidenceHistories.Single();
        TrackAsLoaded(db, application);

        application.RemoveResidence(residence.ResidenceHistoryId);
        db.ChangeTracker.DetectChanges();

        db.Entry(residence).State.Should().Be(EntityState.Deleted);
    }

    [Fact]
    public void RemovingAUnitFromTheProperty_MarksItDeleted()
    {
        using var db = CreateContext();
        var property = new Property("Maple Court", TestData.Address());
        var unit = new RentalUnit("101", 2, 1500m, TestData.ActiveType());
        property.AddUnit(unit);
        TrackAsLoaded(db, property);

        property.RemoveUnit(unit);
        db.ChangeTracker.DetectChanges();

        db.Entry(unit).State.Should().Be(EntityState.Deleted);
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("DataSource=:memory:").Options);

    // Simulates entities that were loaded from the database, without needing one.
    private static void TrackAsLoaded(ApplicationDbContext db, object root)
    {
        db.Add(root);
        var nextId = 1;
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            foreach (var property in entry.Properties.Where(property => property.IsTemporary))
            {
                property.CurrentValue = nextId++;
                property.IsTemporary = false;
            }
        }

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Unchanged;
        }
    }
}
