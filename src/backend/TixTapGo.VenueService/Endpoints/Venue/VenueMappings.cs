using Riok.Mapperly.Abstractions;

using TixTapGo.VenueService.Endpoints.Venue.DTO;

namespace TixTapGo.VenueService.Endpoints.Venue;

[Mapper]
internal static partial class VenueMappings
{
    public static partial IQueryable<GetVenueResponse> ProjectToGetVenueResponses(this IQueryable<Entities.Venue> query);

    [MapperIgnoreSource(nameof(Entities.Venue.CreatedAt))]
    [MapperIgnoreSource(nameof(Entities.Venue.UpdatedAt))]
    [MapperIgnoreSource(nameof(Entities.Venue.IsDeleted))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatCategories))]
    [MapperIgnoreSource(nameof(Entities.Venue.CurrentSeatingMap))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatingMapVersions))]
    [MapProperty(nameof(Entities.Venue.CurrentSeatingMapId), nameof(GetVenueResponse.SeatingMapVersionId))]
    public static partial GetVenueResponse ToGetVenueResponse(this Entities.Venue entity);

    [MapperIgnoreTarget(nameof(Entities.Venue.Id))]
    [MapperIgnoreTarget(nameof(Entities.Venue.CurrentSeatingMap))]
    [MapperIgnoreTarget(nameof(Entities.Venue.CurrentSeatingMapId))]
    public static partial Entities.Venue ToEntity(this CreateVenueRequest dto);
}
