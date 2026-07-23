using CareConnect.Application.Interfaces;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Straight-line distance only - no external API, nothing persisted. See
/// <see cref="IGeoDistanceService"/> for the contract this fulfils.
/// </summary>
public class GeoDistanceService : IGeoDistanceService
{
    private const double EarthRadiusKm = 6371.0088;

    public double CalculateDistanceKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        var lat1 = ToRadians((double)latitude1);
        var lon1 = ToRadians((double)longitude1);
        var lat2 = ToRadians((double)latitude2);
        var lon2 = ToRadians((double)longitude2);

        var deltaLat = lat2 - lat1;
        var deltaLon = lon2 - lon1;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(EarthRadiusKm * c, 2);
    }

    public GeoBoundingBox CalculateBoundingBox(decimal latitude, decimal longitude, double radiusKm)
    {
        var latitudeDegrees = (decimal)(radiusKm / (EarthRadiusKm * Math.PI / 180));

        // Longitude degrees per km shrink toward the poles - clamp the cosine so a search
        // centred at a very high latitude cannot divide by (near) zero.
        var latitudeRadians = ToRadians((double)latitude);
        var cosLatitude = Math.Max(Math.Cos(latitudeRadians), 0.01);
        var longitudeDegrees = (decimal)(radiusKm / (EarthRadiusKm * Math.PI / 180) / cosLatitude);

        var minLatitude = Math.Max(-90m, latitude - latitudeDegrees);
        var maxLatitude = Math.Min(90m, latitude + latitudeDegrees);
        var minLongitude = Math.Max(-180m, longitude - longitudeDegrees);
        var maxLongitude = Math.Min(180m, longitude + longitudeDegrees);

        return new GeoBoundingBox(minLatitude, maxLatitude, minLongitude, maxLongitude);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
