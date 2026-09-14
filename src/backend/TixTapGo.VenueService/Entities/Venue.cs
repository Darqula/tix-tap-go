using TixTapGo.Shared.Persistence.Entities;
using TixTapGo.VenueService.Contracts.Messages.Local;
using TixTapGo.VenueService.Endpoints.SeatingMapVersion.Exceptions;

namespace TixTapGo.VenueService.Entities;

internal sealed class Venue : EntityBase
{
    public required string Title { get; set; }
    public required string Address { get; set; }
    public string? Description { get; set; }

    public IEnumerable<VenueSeatingMapVersion>? SeatingMapVersions { get; }
    public IEnumerable<SeatCategory> SeatCategories { get; } = new List<SeatCategory>();

    public VenueSeatingMapVersion? GetCurrentActiveMapVersion()
    {
        if (SeatingMapVersions == null)
            throw new InvalidOperationException(
                $"All seating map versions of the venue must be loaded to publish a draft");
        
        return SeatingMapVersions
            .Where(VenueSeatingMapVersion.IsCurrentActiveCompiled)
            .SingleOrDefault();
    }
    
    public void PublishVersionMap(VenueSeatingMapVersion versionDraft)
    {
        if (!versionDraft.IsDraft)
            throw new SeatingMapVersioningException($"Map version {versionDraft.Id} already published");

        var currentActiveVersion = GetCurrentActiveMapVersion();

        DateTimeOffset publicationDateTime = DateTimeOffset.UtcNow;

        if (currentActiveVersion != null)
        {
            currentActiveVersion.ValidToExclusive = publicationDateTime;
        }
        
        versionDraft.IsDraft = false;
        versionDraft.ValidFrom = publicationDateTime;
        versionDraft.ValidToExclusive = null;
        AddDomainEvent(new SeatingMapVersionPublished(Id, versionDraft.Id));
    }

    public VenueSeatingMapVersion UnpublishActiveMapVersion()
    {
        var currentActiveVersion = GetCurrentActiveMapVersion();

        if (currentActiveVersion == null)
        {
            throw new SeatingMapVersioningException($"There is no active map version to unpublish");
        }
        currentActiveVersion.ValidToExclusive = DateTimeOffset.UtcNow;

        return currentActiveVersion;
    }
}
