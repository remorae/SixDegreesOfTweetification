using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class PlaceResult
    {
        public string Name { get; }
        public string Type { get; }
        public HashSet<string> Hashtags { get; } = new HashSet<string>();
        public List<string> Sources { get; } = new List<string>();

        public PlaceResult(string name, PlaceType type)
        {
            Name = name;
            Type = type.ToString();
        }
    }
}
