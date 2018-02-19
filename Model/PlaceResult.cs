using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class PlaceResult
    {
        public string Name { get; }
        public string Type { get; }
        public string Country { get; }
        public ISet<string> Hashtags { get; } = new HashSet<string>();
        public ICollection<string> Sources { get; } = new List<string>();

        internal PlaceResult(string name, PlaceType type, string country)
        {
            Name = name;
            Type = type.ToString();
            Country = country;
        }
    }
}
