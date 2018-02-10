using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class Country
    {
        public string CountryName { get; set; }
        public Dictionary<string, City> Cities { get; } = new Dictionary<string, City>();

        public Country(string countryName)
        {
            CountryName = countryName;
        }
    }
}
