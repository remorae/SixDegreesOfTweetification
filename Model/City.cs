using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class City
    {
        public string CityName { get; set; }
        public HashSet<string> Hashtags { get; } = new HashSet<string>();

        public City(string cityName)
        {
            CityName = cityName;
        }
    }
}
