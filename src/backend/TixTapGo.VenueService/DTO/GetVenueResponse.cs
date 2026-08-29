namespace TixTapGo.VenueService.DTO;

public record GetVenueResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Address { get; set; }
    public string? Description { get; set; }
    public Guid? SeatingMapVersionId { get; set; }
}
