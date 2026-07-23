using System.Globalization;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Builds a destination-only external directions link. No API key, no request signing, and
/// the patient's own location is never included - the link only ever encodes where the
/// hospital is.
/// </summary>
internal static class DirectionsUrlBuilder
{
    internal static string? Build(decimal? latitude, decimal? longitude)
    {
        if (latitude is null || longitude is null)
        {
            return null;
        }

        var lat = latitude.Value.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.Value.ToString(CultureInfo.InvariantCulture);
        var destination = Uri.EscapeDataString($"{lat},{lon}");

        return $"https://www.google.com/maps/dir/?api=1&destination={destination}";
    }
}
