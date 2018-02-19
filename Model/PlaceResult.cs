using System.Collections.Generic;

namespace SixDegrees.Model
{
    class PlaceResult
    {
        internal string Name { get; }
        internal string Type { get; }
        internal string Country { get; }
        internal ISet<string> Hashtags { get; } = new HashSet<string>();
        internal ICollection<string> Sources { get; } = new List<string>();

        internal PlaceResult(string name, PlaceType type, string country)
        {
            Name = name;
            Type = type.ToString();
            Country = country;
        }
    }
}
