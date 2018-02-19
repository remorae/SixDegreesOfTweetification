using System.Collections.Generic;

namespace SixDegrees.Model
{
    class Country
    {
        internal string Name { get; set; }
        internal IDictionary<string, PlaceResult> Places { get; } = new Dictionary<string, PlaceResult>();

        internal Country(string name)
        {
            Name = name;
        }
    }
}
