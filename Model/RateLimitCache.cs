using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SixDegrees.Controllers;
using SixDegrees.Data;

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

        internal AppRateLimitInfo this[TwitterAPIEndpoint type] => cache[type];

        private IDictionary<TwitterAPIEndpoint, AppRateLimitInfo> cache;

        private RateLimitCache()
        {
            cache = new Dictionary<TwitterAPIEndpoint, AppRateLimitInfo>();
            foreach (TwitterAPIEndpoint endpoint in Enum.GetValues(typeof(TwitterAPIEndpoint)))
                cache.Add(endpoint, new AppRateLimitInfo() { Type = endpoint });
        }

        internal IDictionary<QueryType, IDictionary<AuthenticationType, int>> CurrentRateLimits(RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, ClaimsPrincipal user)
            => Enum.GetValues(typeof(QueryType)).Cast<QueryType>()
            .ToDictionary(type => type, type => MinimumRateLimits(type, rateLimitDb, userManager, user));

        internal static bool HasEndpoint(QueryType type) => Endpoints(type).Count() > 0;

        internal void Reset(QueryType type)
        {
            foreach (TwitterAPIEndpoint endpoint in Endpoints(type))
                cache[endpoint].ResetIfNeeded();
        }

        internal TimeSpan? UntilReset(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].UntilReset as TimeSpan?) ?? null;

        internal TimeSpan? SinceLastUpdate(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].SinceLastUpdate as TimeSpan?) ?? null;

        internal IDictionary<AuthenticationType, int> MinimumRateLimits(QueryType type, RateLimitDbContext dbContext, UserManager<ApplicationUser> userManager, ClaimsPrincipal user) => new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, Endpoints(type).Min(endpoint => { var info = cache[endpoint.Value]; info.ResetIfNeeded(); return info.Limit as int?; }) ?? -1 },
                { AuthenticationType.User, Endpoints(type).Min(endpoint => RateLimitController.GetCurrentUserInfo(dbContext, endpoint.Value, userManager, user)?.Limit as int?) ?? -1 }
            };
    }
}
