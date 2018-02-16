using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CountryResult
    {
        public string Name { get; }
        public List<PlaceResult> Places { get; }

        public CountryResult(string name, List<PlaceResult> places)
        {
            Name = name;
            Places = places;
        }
    }
}
