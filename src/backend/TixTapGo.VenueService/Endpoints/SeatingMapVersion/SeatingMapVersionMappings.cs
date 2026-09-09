using Riok.Mapperly.Abstractions;

using TixTapGo.VenueService.Endpoints.SeatingMapVersion.DTO;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion;

[Mapper]
internal static partial class SeatingMapVersionMappings
{
    public static partial IQueryable<GetSeatingMapVersionResponse> ProjectToSeatingMapVersionResponse(
        this IQueryable<VenueSeatingMapVersion> query);

    [MapperIgnoreSource(nameof(VenueSeatingMapVersion.IsDeleted))]
    [MapperIgnoreSource(nameof(VenueSeatingMapVersion.CreatedAt))]
    [MapperIgnoreSource(nameof(VenueSeatingMapVersion.UpdatedAt))]
    [MapperIgnoreSource(nameof(VenueSeatingMapVersion.DomainEvents))]
    [MapperIgnoreSource(nameof(VenueSeatingMapVersion.Venue))]
    public static partial GetSeatingMapVersionResponse ToGetSeatingMapVersionResponse(
        this VenueSeatingMapVersion entity);
    
    [MapperIgnoreTarget(nameof(VenueSeatingMapVersion.Id))]
    [MapperIgnoreTarget(nameof(VenueSeatingMapVersion.Venue))]
    [MapperIgnoreTarget(nameof(VenueSeatingMapVersion.ValidFrom))]
    [MapperIgnoreTarget(nameof(VenueSeatingMapVersion.ValidToExclusive))]
    [MapValue(nameof(VenueSeatingMapVersion.IsDraft), true)]
    public static partial VenueSeatingMapVersion ToEntity(this CreateSeatingMapVersionDraftRequest dto, Guid venueId);
}
