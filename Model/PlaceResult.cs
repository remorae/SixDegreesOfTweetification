using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class PlaceResult
    {
        public string Name { get; }
        public string Type { get; }
        public string Country { get; }
        public HashSet<string> Hashtags { get; } = new HashSet<string>();
        public List<string> Sources { get; } = new List<string>();

        public PlaceResult(string name, PlaceType type, string country)
        {
            Name = name;
            Type = type.ToString();
            Country = country;
        }
    }
}
