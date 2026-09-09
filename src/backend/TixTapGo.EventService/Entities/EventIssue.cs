using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Entities;

internal record EventIssue(EventIssueType Type, DateTimeOffset DetectedAt, string? Description = null);
