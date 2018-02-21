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

        internal RateLimitInfo this[TwitterAPIEndpoint type]
        {
            get
            {
                if (!cache.ContainsKey(type))
                    cache.Add(type, new RateLimitInfo(type));
                return cache[type];
            }
        }

        private IDictionary<TwitterAPIEndpoint, RateLimitInfo> cache;

        private RateLimitCache()
        {
            cache = new Dictionary<TwitterAPIEndpoint, RateLimitInfo>();
            foreach (var endpoint in Enum.GetValues(typeof(TwitterAPIEndpoint)).Cast<TwitterAPIEndpoint>()
                    .Where(endpoint => endpoint != TwitterAPIEndpoint.RateLimitStatus))
                cache.Add(endpoint, new RateLimitInfo(endpoint));
        }

        internal IDictionary<QueryType, IDictionary<AuthenticationType, int>> CurrentRateLimits
            => Enum.GetValues(typeof(QueryType)).Cast<QueryType>().ToDictionary(type => type, type => MinimumRateLimits(type));

        internal void Reset(QueryType type)
        {
            foreach (TwitterAPIEndpoint endpoint in Endpoints(type))
                cache[endpoint].Reset();
        }

        internal TimeSpan? UntilReset(QueryType type) => Endpoints(type)?.Max(endpoint => cache[endpoint].UntilReset) ?? null;

        internal TimeSpan? SinceLastUpdate(QueryType type) => Endpoints(type)?.Max(endpoint => cache[endpoint].SinceLastUpdate) ?? null;

        internal IDictionary<AuthenticationType, int> MinimumRateLimits(QueryType type)
            => new Dictionary<AuthenticationType, int>(
                Enum.GetValues(typeof(AuthenticationType)).Cast<AuthenticationType>()
                .ToDictionary(authType => authType, authType => Endpoints(type)?.Min(endpoint => cache[endpoint][authType]) ?? -1)
            );

        internal static IDictionary<AuthenticationType, int> BadRateLimit()
            => new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, -1 },
                { AuthenticationType.User, -1 }
            };

        private IEnumerable<TwitterAPIEndpoint> Endpoints(QueryType type)
        {
            switch (type)
            {
                case QueryType.TweetsByHashtag:
                case QueryType.LocationsByHashtag:
                    yield return TwitterAPIEndpoint.SearchTweets;
                    break;
                case QueryType.UserByScreenName:
                    yield return TwitterAPIEndpoint.UserShow;
                    break;
                case QueryType.UserConnectionsByScreenName:
                    yield return TwitterAPIEndpoint.UserLookup;
                    break;
                default:
                    break;
            }
        }
    }
}
