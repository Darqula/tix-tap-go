using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Entities;

internal sealed class EventIssue
{
    public EventIssue(EventIssueType type, string? key = null, DateTimeOffset? detectedAt = null)
    {
        Type = type;
        Key = key;
        DetectedAt = detectedAt ?? DateTimeOffset.UtcNow;
    }

    private EventIssue() { }

    public EventIssueType Type { get; init; }
    public string? Key { get; init; }
    public DateTimeOffset DetectedAt { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is EventIssue issue && Equals(issue);
    }

    public bool Equals(EventIssue? other)
    {
        return other != null &&
               Type == other.Type &&
               Key == other.Key;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Key);
    }
}
