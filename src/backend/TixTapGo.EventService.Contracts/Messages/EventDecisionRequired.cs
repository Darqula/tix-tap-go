using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages;

public record EventDecisionRequired(string Message, Guid EventId, Dictionary<string, string>? Details = null)
    : IDomainEvent
{
    public static class Keys
    {
        public const string VenueId = "VenueId";
        public const string VenueCategoryId = "VenueCategoryId";
        public const string VenueCategoryPricingId = "VenueCategoryPricingId";
    }
}
