using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CountryResult
    {
        public string Name { get; }
        public Dictionary<string, List<PlaceResult>> Places { get; }

        public CountryResult(string name, Dictionary<string, List<PlaceResult>> places)
        {
            Name = name;
            Places = places;
        }
    }
}
