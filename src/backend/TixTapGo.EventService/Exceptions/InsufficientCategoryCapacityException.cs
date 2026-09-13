using TixTapGo.Shared.Exceptions;

namespace TixTapGo.EventService.Exceptions;

internal sealed class InsufficientCategoryCapacityException(string categoryIdentifier, int required, int available)
    : DomainException($"Impossible to enable pricing for category {categoryIdentifier} due to insufficient " +
                      $"capacity (only {available} out of {required} attendees are available)")
{
    public int Required { get; } = required;
    public int Available { get; } = available;
}
