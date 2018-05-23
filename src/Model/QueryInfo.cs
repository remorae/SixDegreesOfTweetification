using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Model
{
    /// <summary>
    /// Cached query information to support traversing through multiple "pages" of Twitter data upon repeat queries.
    /// </summary>
    class QueryInfo
    {
        internal static bool UsesMaxID(TwitterAPIEndpoint endpoint)
        {
            switch (endpoint)
            {
                case TwitterAPIEndpoint.SearchTweets:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool UsesCursor(TwitterAPIEndpoint endpoint)
        {
            switch (endpoint)
            {
                case TwitterAPIEndpoint.SearchTweets:
                case TwitterAPIEndpoint.UsersShow:
                case TwitterAPIEndpoint.UsersLookup:
                case TwitterAPIEndpoint.RateLimitStatus:
                case TwitterAPIEndpoint.OAuthAuthorize:
                    return false;
                case TwitterAPIEndpoint.FriendsIDs:
                case TwitterAPIEndpoint.FollowersIDs:
                    return true;
                default:
                    throw new Exception("Unimplemented TwitterAPIEndpoint");
            }
        }

        internal IEnumerable<string> LastQuerySet { get; set; } = Enumerable.Empty<string>();
        internal string LastQuery { get; set; } = "";
        internal string NextMaxID { get; set; } = "";
        internal string NextCursor { get; set; } = "";
    }
}
