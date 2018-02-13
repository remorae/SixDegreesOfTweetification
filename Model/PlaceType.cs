namespace SixDegrees.Model
{
    public enum PlaceType
    {
        POI,
        Neighborhood,
        City,
        Admin,
        Unknown
    }

    static class PlaceTypeMethods
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