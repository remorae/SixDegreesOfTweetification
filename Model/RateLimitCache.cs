using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Model
{
    class RateLimitCache
    {
        private static RateLimitCache instance;

        internal static RateLimitCache Get
        {
            get
            {
                if (instance == null)
                    instance = new RateLimitCache();
                return instance;
            }
        }

        internal static readonly IDictionary<AuthenticationType, int> BadRateLimit
            = new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, -1 },
                { AuthenticationType.User, -1 }
            };

        private static IEnumerable<TwitterAPIEndpoint?> Endpoints(QueryType type)
        {
            switch (type)
            {
                case QueryType.TweetsByHashtag:
                case QueryType.LocationsByHashtag:
                case QueryType.HashtagsFromHashtag:
                case QueryType.HashtagConnectionsByHashtag:
                    yield return TwitterAPIEndpoint.SearchTweets;
                    yield break;
                case QueryType.UserByScreenName:
                    yield return TwitterAPIEndpoint.UsersShow;
                    yield break;
                case QueryType.UserConnectionsByScreenName:
                    yield return TwitterAPIEndpoint.FollowersIDs;
                    yield return TwitterAPIEndpoint.FriendsIDs;
                    yield return TwitterAPIEndpoint.UsersLookup;
                    yield break;
                default:
                    yield break;
            }
        }

        internal RateLimitInfo this[TwitterAPIEndpoint type] => cache[type];

        private IDictionary<TwitterAPIEndpoint, RateLimitInfo> cache;

        private RateLimitCache()
        {
            cache = new Dictionary<TwitterAPIEndpoint, RateLimitInfo>();
            foreach (TwitterAPIEndpoint endpoint in Enum.GetValues(typeof(TwitterAPIEndpoint)))
                cache.Add(endpoint, new RateLimitInfo(endpoint));
        }

        internal IDictionary<QueryType, IDictionary<AuthenticationType, int>> CurrentRateLimits
            => Enum.GetValues(typeof(QueryType)).Cast<QueryType>()
            .ToDictionary(type => type, type => MinimumRateLimits(type));

        internal static bool HasEndpoint(QueryType type) => Endpoints(type).Count() > 0;

        internal void Reset(QueryType type)
        {
            foreach (TwitterAPIEndpoint endpoint in Endpoints(type))
                cache[endpoint].ResetIfNeeded();
        }

        internal TimeSpan? UntilReset(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].UntilReset as TimeSpan?) ?? null;

        internal TimeSpan? SinceLastUpdate(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].SinceLastUpdate as TimeSpan?) ?? null;

        internal IDictionary<AuthenticationType, int> MinimumRateLimits(QueryType type)
            => RateLimitInfo.UseableAuthTypes.ToDictionary(authType => authType, authType => Endpoints(type).Min(endpoint => cache[endpoint.Value][authType] as int?) ?? -1);
    }
}
