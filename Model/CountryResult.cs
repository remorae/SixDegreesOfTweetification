using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CountryResult
    {
        public string CountryName { get; }
        public IEnumerable<CityResult> Cities { get; }

        public CountryResult(string countryName, IEnumerable<CityResult> cities)
        {
            CountryName = countryName;
            Cities = cities;
        }
    }
}
