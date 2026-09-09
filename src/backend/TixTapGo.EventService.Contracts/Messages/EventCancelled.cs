using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages;

public record EventCancelled(Guid EventId, EventCancellationReason Reason): IDomainEvent;
