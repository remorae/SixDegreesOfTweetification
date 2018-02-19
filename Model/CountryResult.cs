using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CountryResult
    {
        public string Name { get; }
        public IEnumerable<PlaceResult> Places { get; }

        internal CountryResult(string name, IEnumerable<PlaceResult> places)
        {
            Name = name;
            Places = places;
        }
    }
}
