using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DTO.Mapping;

internal static class VenueMappings
{
    public static GetVenueResponse ToGetVenueResponse(this Venue entity)
    {
        return new GetVenueResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Address = entity.Address,
            Description = entity.Description
        };
    }

    public static Venue ToEntity(this CreateVenueRequest dto)
    {
        return new Venue()
        {
            Title = dto.Title,
            Address = dto.Address,
            Description = dto.Description
        };
    }
}
