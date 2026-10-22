namespace KandyTourism.Api.Application.Common;

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371;

    /// <summary>Great-circle ("as the crow flies") distance. Not a road distance.</summary>
    public static double StraightLineKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRad(double deg) => deg * Math.PI / 180;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(h));
    }
}
