using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class HashtagConnectionInfo
    {
        public int Distance { get; }
        public ISet<string> Connections { get; }

        public HashtagConnectionInfo(int distance)
        {
            Distance = distance;
            Connections = new HashSet<string>();
        }
    }
}
