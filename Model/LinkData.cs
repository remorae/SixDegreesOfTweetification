using System;
using System.Collections.Generic;

namespace SixDegrees.Controllers
{
    internal class LinkData<T>
    {
        public Dictionary<string, ICollection<T>> Connections { get; set; }
        public IEnumerable<LinkPath<T>> Paths { get; set; }
        public LinkMetaData Metadata { get; set; }
    }

    public class LinkMetaData
    {
        public TimeSpan Time { get; set; }
        public int Calls { get; set; }
    }

    public class LinkPath<T>
    {
        public Dictionary<int, T> Path { get; set; }
        public IEnumerable<string> Links { get; set; }
    }
}