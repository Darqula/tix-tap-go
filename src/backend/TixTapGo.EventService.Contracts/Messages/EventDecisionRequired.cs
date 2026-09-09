using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages;

public record EventDecisionRequired(string Message, Guid EventId, Dictionary<string, string>? Details = null)
    : IDomainEvent;
