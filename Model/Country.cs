using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class Country
    {
        public string CountryName { get; set; }
        public Dictionary<string, PlaceResult> Places { get; } = new Dictionary<string, PlaceResult>();
        public List<string> Sources { get; } = new List<string>();

        public Country(string countryName)
        {
            CountryName = countryName;
        }
    }
}
