using System.Globalization;
using System.Text.RegularExpressions;

using ModelContextProtocol;

namespace TixTapGo.StorefrontMcpService.Schedule.Model;

/// <summary>
/// Half-open range [From, To) plus the timezone
/// </summary>
internal sealed partial class ScheduleRange
{
    private static readonly string[] LocalDateTimeFormats = ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm"];
    private static readonly string[] OffsetDateTimeFormats =
        ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mmK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"];

    private const string DateOnlyFormat = "yyyy-MM-dd";
    private const int MaxInputLength = 64;
    private const int MinSupportedYear = 1900;
    private const int MaxSupportedYear = 2999;

    private readonly TimeZoneInfo? _timeZone;
    private readonly TimeSpan _fixedOffset;

    private ScheduleRange(DateTimeOffset from, DateTimeOffset to, TimeZoneInfo? timeZone, string timeZoneLabel)
    {
        From = from;
        To = to;
        _timeZone = timeZone;
        _fixedOffset = from.Offset;
        TimeZoneLabel = timeZoneLabel;
    }

    public DateTimeOffset From { get; }
    public DateTimeOffset To { get; }

    /// <summary>
    /// The IANA id as given by the caller, or the offset of dateFrom
    /// </summary>
    public string TimeZoneLabel { get; }

    public static ScheduleRange Parse(string dateFrom, string dateTo, string? timeZone)
    {
        // Values are echoed back in error messages, so oversized input is rejected before anything else
        if (dateFrom?.Length > MaxInputLength || dateTo?.Length > MaxInputLength || timeZone?.Length > MaxInputLength)
        {
            throw new McpException($"dateFrom, dateTo and timeZone must not be longer than {MaxInputLength} characters.");
        }

        dateFrom ??= string.Empty;
        dateTo ??= string.Empty;

        string? timeZoneId = string.IsNullOrWhiteSpace(timeZone) ? null : timeZone.Trim();
        TimeZoneInfo? zone = timeZoneId == null ? null : FindTimeZone(timeZoneId);

        DateTimeOffset from = ParseBoundary(dateFrom, zone, timeZoneId, isUpperBound: false);
        DateTimeOffset to = ParseBoundary(dateTo, zone, timeZoneId, isUpperBound: true);

        if (from >= to)
        {
            throw new McpException("dateFrom must be earlier than dateTo.");
        }

        return new ScheduleRange(from, to, zone, timeZoneId ?? FormatOffset(from.Offset));
    }
    
    public bool Overlaps(DateTimeOffset start, DateTimeOffset end)
    {
        return start < To && end > From;
    }
    
    public DateTimeOffset AdjustTimeZone(DateTimeOffset instant)
    {
        return _timeZone != null ? TimeZoneInfo.ConvertTime(instant, _timeZone) : instant.ToOffset(_fixedOffset);
    }

    public string Format(DateTimeOffset instant)
    {
        return AdjustTimeZone(instant).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ParseBoundary(string value, TimeZoneInfo? zone, string? timeZoneId,
        bool isUpperBound)
    {
        string trimmed = value.Trim();

        if (OffsetSuffixRegex().IsMatch(trimmed))
        {
            // Exact parsing needs an uppercase 'Z'
            string normalized = trimmed.ToUpperInvariant();

            if (!DateTimeOffset.TryParseExact(normalized, OffsetDateTimeFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var instant))
            {
                throw InvalidDate(value);
            }

            EnsureSupportedYear(instant.Year, value);
            return instant;
        }

        bool isDateOnly = DateTime.TryParseExact(trimmed, DateOnlyFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime local);

        if (!isDateOnly && !DateTime.TryParseExact(trimmed, LocalDateTimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out local))
        {
            throw InvalidDate(value);
        }

        EnsureSupportedYear(local.Year, value);

        if (zone == null)
        {
            throw new McpException(
                "dateFrom/dateTo need a UTC offset (e.g. 2026-11-07T00:00:00+01:00) or a timeZone argument.");
        }

        // A date-only upper bound means "through the end of that day"
        if (isDateOnly && isUpperBound)
        {
            local = local.AddDays(1);
        }

        if (zone.IsInvalidTime(local))
        {
            throw new McpException($"'{value}' does not exist in {timeZoneId} (daylight-saving gap).");
        }

        // Ambiguous local times resolve to standard time
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // The id lookup is case-sensitive on some platforms
            var match = TimeZoneInfo.GetSystemTimeZones()
                .FirstOrDefault(zone => string.Equals(zone.Id, timeZoneId, StringComparison.OrdinalIgnoreCase));

            return match ?? throw new McpException(
                $"Unknown timeZone '{timeZoneId}'. Use an IANA id such as Europe/Berlin.");
        }
    }

    private static string FormatOffset(TimeSpan offset)
    {
        return (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }
    
    private static void EnsureSupportedYear(int year, string value)
    {
        if (year is < MinSupportedYear or > MaxSupportedYear)
        {
            throw new McpException(
                $"'{value}' is outside the supported range (years {MinSupportedYear}-{MaxSupportedYear}).");
        }
    }

    private static McpException InvalidDate(string value)
    {
        return new McpException($"'{value}' is not a valid ISO 8601 date or date-time.");
    }

    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetSuffixRegex();
}
