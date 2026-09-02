using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.Tests;

// --- Cascade cluster: Company -> Branch -> Employee (2 levels), plus a non-EntityBase leaf (AuditNote). ---

internal sealed class Company : EntityBase
{
    public required string Name { get; set; }
    public ICollection<Branch> Branches { get; } = new List<Branch>();
    public ICollection<Contract> Contracts { get; } = new List<Contract>();
}

internal sealed class Branch : EntityBase
{
    public required Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public required string Name { get; set; }
    public ICollection<Employee> Employees { get; } = new List<Employee>();
    public ICollection<AuditNote> AuditNotes { get; } = new List<AuditNote>();
}

internal sealed class Employee : EntityBase
{
    public required Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public required string Name { get; set; }
    public ICollection<Locker> AssignedLockers { get; } = new List<Locker>();
}

internal sealed class Contract : EntityBase
{
    public required Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public required string Reference { get; set; }
}

internal sealed class Locker : EntityBase
{
    public required string Code { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
}

/// <summary>
/// Deliberately not an <see cref="EntityBase"/>, must be hard-deleted
/// instead of soft-deleting.
/// </summary>
internal sealed class AuditNote
{
    public Guid Id { get; set; }
    public required Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public required string Text { get; set; }
}

// --- SetNull cluster: Warehouse <- StoreLocation.PreferredWarehouseId (optional FK). ---

internal sealed class Warehouse : EntityBase
{
    public required string Name { get; set; }
    public ICollection<StoreLocation> PreferredByStores { get; } = new List<StoreLocation>();
}

internal sealed class StoreLocation : EntityBase
{
    public required string Name { get; set; }
    public Guid? PreferredWarehouseId { get; set; }
    public Warehouse? PreferredWarehouse { get; set; }
}

// --- Restrict cluster: Category <- Product.CategoryId, explicitly configured Restrict. ---

internal sealed class Category : EntityBase
{
    public required string Name { get; set; }
    public ICollection<Product> Products { get; } = new List<Product>();
}

internal sealed class Product : EntityBase
{
    public required Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public required string Name { get; set; }
}

// --- Missing-back-navigation cluster: Playlist has no collection nav to Track, exercising the reflection-based fallback. ---

internal sealed class Playlist : EntityBase
{
    public required string Name { get; set; }

    // Deliberately no `ICollection<Track> Tracks` here.
}

internal sealed class Track : EntityBase
{
    public required Guid PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;
    public required string Title { get; set; }
}

// --- Single-reference cluster: Vehicle <-> Engine, a 1:1 where the principal side
//     (Vehicle) exposes a single Reference navigation, not a collection ---

internal sealed class Vehicle : EntityBase
{
    public required string Vin { get; set; }
    public Engine? Engine { get; set; }
}

internal sealed class Engine : EntityBase
{
    public required Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public required string SerialNumber { get; set; }
}

// --- Diamond cluster: Project -> WorkstreamA -> SharedNote and Project -> WorkstreamB -> SharedNote
//     converge on the same SharedNote instance, reached via two independent Cascade paths. ---

internal sealed class Project : EntityBase
{
    public required string Name { get; set; }
    public ICollection<WorkstreamA> WorkstreamsA { get; } = new List<WorkstreamA>();
    public ICollection<WorkstreamB> WorkstreamsB { get; } = new List<WorkstreamB>();
}

internal sealed class WorkstreamA : EntityBase
{
    public required Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<SharedNote> Notes { get; } = new List<SharedNote>();
}

internal sealed class WorkstreamB : EntityBase
{
    public required Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<SharedNote> Notes { get; } = new List<SharedNote>();
}

internal sealed class SharedNote : EntityBase
{
    public required Guid WorkstreamAId { get; set; }
    public WorkstreamA WorkstreamA { get; set; } = null!;
    public required Guid WorkstreamBId { get; set; }
    public WorkstreamB WorkstreamB { get; set; } = null!;
    public required string Text { get; set; }
}

// --- Composite-key clusters. Each principal's Id stays a plain Guid (untouched, single-column
//     PK, consistent with the rest of the model); the composite key is a separate alternate key
//     (RegionCode + SiteCode/DistrictCode) that the dependent's composite FK targets via
//     HasPrincipalKey ---

// Facility -> Asset: composite key, Cascade, with a principal-side navigation
internal sealed class Facility : EntityBase
{
    public required string RegionCode { get; set; }
    public required string SiteCode { get; set; }
    public required string Name { get; set; }
    public ICollection<Asset> Assets { get; } = new List<Asset>();
}

internal sealed class Asset : EntityBase
{
    public required string RegionCode { get; set; }
    public required string SiteCode { get; set; }
    public Facility Facility { get; set; } = null!;
    public required string Tag { get; set; }
}

// Depot -> Shipment: same composite key shape, Cascade, but Depot has no collection navigation to Shipment
internal sealed class Depot : EntityBase
{
    public required string RegionCode { get; set; }
    public required string SiteCode { get; set; }
    public required string Name { get; set; }

    // Deliberately no `ICollection<Shipment> Shipments` here.
}

internal sealed class Shipment : EntityBase
{
    public required string RegionCode { get; set; }
    public required string SiteCode { get; set; }
    public Depot Depot { get; set; } = null!;
    public required string TrackingNumber { get; set; }
}

// District -> Outlet: composite key, SetNull, reached via the principal's collection
// navigation - exercises SetNull's handling of every column in a composite foreign key.
internal sealed class District : EntityBase
{
    public required string RegionCode { get; set; }
    public required string DistrictCode { get; set; }
    public required string Name { get; set; }
    public ICollection<Outlet> Outlets { get; } = new List<Outlet>();
}

internal sealed class Outlet : EntityBase
{
    public string? RegionCode { get; set; }
    public string? DistrictCode { get; set; }
    public District? District { get; set; }
    public required string Name { get; set; }
}

// Zone -> Meter: same composite-key shape, but Restrict
internal sealed class Zone : EntityBase
{
    public required string RegionCode { get; set; }
    public required string ZoneCode { get; set; }
    public required string Name { get; set; }
    public ICollection<Meter> Meters { get; } = new List<Meter>();
}

internal sealed class Meter : EntityBase
{
    public required string RegionCode { get; set; }
    public required string ZoneCode { get; set; }
    public Zone Zone { get; set; } = null!;
    public required string SerialNumber { get; set; }
}
