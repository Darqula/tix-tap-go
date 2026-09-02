using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.Persistence.DAL;

namespace TixTapGo.Shared.Persistence.Tests;

/// <summary>
/// Hosts every test entity cluster in one model, isolated per test via a dedicated
/// Postgres schema (<see cref="_schema"/>) so tests can run in parallel against the
/// same container without a fresh container per test.
/// </summary>
internal sealed class TestDbContext(DbContextOptions<TestDbContext> options, string schema)
    : SharedDbContext(options)
{
    /// <summary>
    /// Exposed for <see cref="SchemaModelCacheKeyFactory"/>
    /// </summary>
    internal string Schema => schema;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AuditNote> AuditNotes => Set<AuditNote>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StoreLocation> StoreLocations => Set<StoreLocation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Locker> Lockers => Set<Locker>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Engine> Engines => Set<Engine>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkstreamA> WorkstreamsA => Set<WorkstreamA>();
    public DbSet<WorkstreamB> WorkstreamsB => Set<WorkstreamB>();
    public DbSet<SharedNote> SharedNotes => Set<SharedNote>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Meter> Meters => Set<Meter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<Branch>()
            .HasOne(b => b.Company)
            .WithMany(c => c.Branches)
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Branch)
            .WithMany(b => b.Employees)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditNote>()
            .HasOne(a => a.Branch)
            .WithMany(b => b.AuditNotes)
            .HasForeignKey(a => a.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StoreLocation>()
            .HasOne(s => s.PreferredWarehouse)
            .WithMany(w => w.PreferredByStores)
            .HasForeignKey(s => s.PreferredWarehouseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deliberately WithMany() (no back-collection) - Playlist has no navigation to Track.
        modelBuilder.Entity<Track>()
            .HasOne(t => t.Playlist)
            .WithMany()
            .HasForeignKey(t => t.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Company)
            .WithMany(co => co.Contracts)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Locker>()
            .HasOne(l => l.AssignedEmployee)
            .WithMany(e => e.AssignedLockers)
            .HasForeignKey(l => l.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Genuine 1:1 - principal (Vehicle) exposes a single Reference nav, not a collection.
        modelBuilder.Entity<Engine>()
            .HasOne(e => e.Vehicle)
            .WithOne(v => v.Engine)
            .HasForeignKey<Engine>(e => e.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkstreamA>()
            .HasOne(w => w.Project)
            .WithMany(p => p.WorkstreamsA)
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkstreamB>()
            .HasOne(w => w.Project)
            .WithMany(p => p.WorkstreamsB)
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharedNote>()
            .HasOne(n => n.WorkstreamA)
            .WithMany(w => w.Notes)
            .HasForeignKey(n => n.WorkstreamAId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharedNote>()
            .HasOne(n => n.WorkstreamB)
            .WithMany(w => w.Notes)
            .HasForeignKey(n => n.WorkstreamBId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite-key clusters (see comments in TestEntities.cs).
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Facility)
            .WithMany(f => f.Assets)
            .HasForeignKey(a => new
            {
                a.RegionCode,
                a.SiteCode
            })
            .HasPrincipalKey(f => new
            {
                f.RegionCode,
                f.SiteCode
            })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Depot)
            .WithMany() // no back-collection - forces the reflection fallback.
            .HasForeignKey(s => new
            {
                s.RegionCode,
                s.SiteCode
            })
            .HasPrincipalKey(d => new
            {
                d.RegionCode,
                d.SiteCode
            })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Outlet>()
            .HasOne(o => o.District)
            .WithMany(d => d.Outlets)
            .HasForeignKey(o => new
            {
                o.RegionCode,
                o.DistrictCode
            })
            .HasPrincipalKey(d => new
            {
                d.RegionCode,
                d.DistrictCode
            })
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Meter>()
            .HasOne(m => m.Zone)
            .WithMany(z => z.Meters)
            .HasForeignKey(m => new
            {
                m.RegionCode,
                m.ZoneCode
            })
            .HasPrincipalKey(z => new
            {
                z.RegionCode,
                z.ZoneCode
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
