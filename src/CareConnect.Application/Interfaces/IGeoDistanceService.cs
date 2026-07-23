namespace CareConnect.Application.Interfaces;

/// <summary>
/// Straight-line ("as the crow flies") distance between two points, and the bounding-box
/// pre-filter used to narrow candidates before running the exact calculation. No external
/// distance API is ever called, and nothing here is persisted.
/// </summary>
public interface IGeoDistanceService
{
    /// <summary>Haversine great-circle distance in kilometres, rounded to two decimal places.</summary>
    double CalculateDistanceKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2);

    /// <summary>
    /// An approximate lat/long rectangle that fully contains every point within
    /// <paramref name="radiusKm"/> of the centre - cheap enough to translate into a SQL
    /// WHERE clause, used to shrink the candidate set before the exact Haversine pass.
    /// </summary>
    GeoBoundingBox CalculateBoundingBox(decimal latitude, decimal longitude, double radiusKm);
}

public readonly record struct GeoBoundingBox(
    decimal MinLatitude,
    decimal MaxLatitude,
    decimal MinLongitude,
    decimal MaxLongitude);
