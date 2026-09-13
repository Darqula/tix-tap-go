using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages.EventSeatCategory;

public record EventSeatCategoryPriceDeleted(Guid EventId, Guid EventSeatCategoryPricingId) : IDomainEvent;
