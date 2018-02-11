using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CityResult
    {
        public string CityName { get; }
        public IEnumerable<string> Hashtags { get; }

        public CityResult(string cityName, IEnumerable<string> hashtags)
        {
            CityName = cityName;
            Hashtags = hashtags;
        }
    }
}
