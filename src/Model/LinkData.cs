using System;
using System.Collections.Generic;

namespace SixDegrees.Controllers
{
    /// <summary>
    /// The commputed results of a search for connections between entities.
    /// </summary>
    /// <typeparam name="TPath">The type of entity that forms the path.</typeparam>
    /// <typeparam name="TConnection">The stored information about connections from each entity.</typeparam>
    internal class LinkData<TPath, TConnection>
    {
        public Dictionary<string, ICollection<TConnection>> Connections { get; set; }
        public IEnumerable<LinkPath<TPath>> Paths { get; set; }
        public LinkMetaData Metadata { get; set; }
    }

    /// <summary>
    /// Metadata collected during a search.
    /// </summary>
    public class LinkMetaData
    {
        /// <summary>
        /// How long it took to perform a search.
        /// </summary>
        public TimeSpan Time { get; set; }
        /// <summary>
        /// How many Twitter API calls it took to perform a search.
        /// </summary>
        public int Calls { get; set; }
    }

    /// <summary>
    /// Computed paths between entities of a given type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkPath<T>
    {
        /// <summary>
        /// The entities that form a path, by distance.
        /// </summary>
        public Dictionary<int, T> Path { get; set; }
        /// <summary>
        /// URLs of Tweets used to form the path, if applicable.
        /// </summary>
        public IEnumerable<string> Links { get; set; }
    }
}