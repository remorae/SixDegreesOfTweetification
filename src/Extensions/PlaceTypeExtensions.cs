using SixDegrees.Model;

namespace SixDegrees.Extensions
{
    /// <summary>
    /// Helper class for PlaceType.
    /// </summary>
    public static class PlaceTypeExtensions
    {
        /// <summary>
        /// Parses a PlaceType from the given string.
        /// </summary>
        /// <param name="str">One of the returned place type strings according to Twitter's API.</param>
        /// <returns>The parsed result.</returns>
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
