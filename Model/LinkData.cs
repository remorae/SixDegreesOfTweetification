using System;
using System.Collections.Generic;

namespace SixDegrees.Controllers
{
    internal class LinkData<T>
    {
        public Dictionary<string, IEnumerable<T>> Connections { get; set; }
        public IEnumerable<Dictionary<int, T>> Paths { get; set; }
        public IEnumerable<string> Links { get; set; }
        public LinkMetaData Metadata { get; set; }
    }

    public class LinkMetaData
    {
        public TimeSpan Time { get; set; }
        public int Calls { get; set; }
    }
}