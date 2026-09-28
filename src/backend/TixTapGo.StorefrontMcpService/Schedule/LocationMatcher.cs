using System.Globalization;

namespace TixTapGo.StorefrontMcpService.Schedule;

internal static class LocationMatcher
{
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Case- and diacritic-insensitive substring match against the venue title and address.
    /// Relies on ICU, so globalization invariant mode must stay disabled
    /// </summary>
    public static bool Matches(string venueTitle, string venueAddress, string location)
    {
        var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        return compareInfo.IndexOf(venueTitle, location, Options) >= 0
               || compareInfo.IndexOf(venueAddress, location, Options) >= 0;
    }
}
