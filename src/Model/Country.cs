using System.Collections.Generic;

namespace SixDegrees.Model
{
    /// <summary>
    /// Represents a country from Earth that contains Twitter places.
    /// </summary>
    class Country
    {
        internal string Name { get; }
        internal IDictionary<string, Place> Places { get; } = new Dictionary<string, Place>();

        internal Country(string name)
        {
            Name = name;
        }
    }
}
