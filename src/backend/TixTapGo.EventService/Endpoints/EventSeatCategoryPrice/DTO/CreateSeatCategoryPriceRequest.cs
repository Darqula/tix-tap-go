using System.ComponentModel.DataAnnotations;

using TixTapGo.EventService.Contracts.Enums;

namespace TixTapGo.EventService.Endpoints.EventSeatCategoryPrice.DTO;

public record CreateSeatCategoryPriceRequest
{
    public Guid VenueSeatCategoryId { get; set; }

    public bool IsEnabled { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal BasePrice { get; set; }

    public SeatAssignmentType SeatAssignmentType { get; set; }

    [Range(0, 10_000)]
    public int Capacity { get; set; }
}
