using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class Country
    {
        public string Name { get; set; }
        public IDictionary<string, PlaceResult> Places { get; } = new Dictionary<string, PlaceResult>();

        public Country(string name)
        {
            Name = name;
        }
    }
}
