using System.Collections.Generic;

namespace SixDegrees.Model
{
    class UserConnectionInfo
    {
        public int Distance { get; }
        public ISet<UserResult> Connections { get; }

        public UserConnectionInfo(int distance)
        {
            Distance = distance;
            Connections = new HashSet<UserResult>();
        }
    }
}