using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class CountryResult
    {
        public string Name { get; set; }
        public IEnumerable<PlaceResult> Places { get; set; }
    }
}
