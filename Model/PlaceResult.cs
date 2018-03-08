using SixDegrees.Model;
using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class PlaceResult
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public ISet<string> Hashtags { get; } = new HashSet<string>();
        public ICollection<string> Sources { get; } = new List<string>();
    }
}
