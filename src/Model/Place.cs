using System.Collections.Generic;

namespace SixDegrees.Model
{
    /// <summary>
    /// Represents a Twitter Place and any hashtags tweeted from its location.
    /// </summary>
    public class Place
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public ISet<string> Hashtags { get; } = new HashSet<string>();
        public ICollection<string> Sources { get; } = new List<string>();
    }
}
