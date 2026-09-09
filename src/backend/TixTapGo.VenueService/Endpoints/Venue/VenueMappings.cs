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
    [MapperIgnoreSource(nameof(Entities.Venue.DomainEvents))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatCategories))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatingMapVersions))]
    public static partial GetVenueResponse ToGetVenueResponse(this Entities.Venue entity);

    [MapperIgnoreSource(nameof(Entities.Venue.CreatedAt))]
    [MapperIgnoreSource(nameof(Entities.Venue.UpdatedAt))]
    [MapperIgnoreSource(nameof(Entities.Venue.IsDeleted))]
    [MapperIgnoreSource(nameof(Entities.Venue.DomainEvents))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatCategories))]
    [MapperIgnoreSource(nameof(Entities.Venue.SeatingMapVersions))]
    public static partial GetVenueDetailedResponse ToGetVenueDetailedResponse(this Entities.Venue entity,
        Guid? seatingMapVersionId);

    [MapperIgnoreTarget(nameof(Entities.Venue.Id))]
    public static partial Entities.Venue ToEntity(this CreateVenueRequest dto);
}
