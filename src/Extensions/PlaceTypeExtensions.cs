using SixDegrees.Model;

namespace SixDegrees.Extensions
{
    public static class PlaceTypeExtensions
    {
        public static PlaceType ToPlaceType(this string str)
        {
            switch (str)
            {
                default:
                    return PlaceType.Unknown;
                case "poi":
                    return PlaceType.POI;
                case "neighborhood":
                    return PlaceType.Neighborhood;
                case "city":
                    return PlaceType.City;
                case "admin":
                    return PlaceType.Admin;
            }
        }
    }
}
