using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SixDegrees.Controllers;
using SixDegrees.Data;

namespace SixDegrees.Model
{
    /// <summary>
    /// Manages cached rate limit information for Twitter.
    /// </summary>
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

        /// <summary>
        /// Which Twitter API endpoints are used by the given SixDegrees endpoint.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
                case QueryType.UserConnectionsByID:
                    yield return TwitterAPIEndpoint.FollowersIDs;
                    yield return TwitterAPIEndpoint.FriendsIDs;
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

        internal void Reset(QueryType type)
        {
            foreach (TwitterAPIEndpoint endpoint in Endpoints(type))
                cache[endpoint].ResetIfNeeded();
        }

        /// <summary>
        /// How long until the rate limits reset for the given SixDegrees endpoint.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal TimeSpan? UntilReset(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].UntilReset as TimeSpan?) ?? null;

        /// <summary>
        /// How long it has been since the cached rate limits were updated for the given SixDegrees endpoint.
        /// </summary>
        /// <param name="type"></param>
        internal TimeSpan? SinceLastUpdate(QueryType type) => Endpoints(type).Max(endpoint => cache[endpoint.Value].SinceLastUpdate as TimeSpan?) ?? null;

        /// <summary>
        /// The maximum number of calls available for a given SixDegrees endpoint assuming all related Twitter API endpoints will be hit.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dbContext"></param>
        /// <param name="userManager"></param>
        /// <param name="user">The user accessing the SixDegrees API.</param>
        /// <returns></returns>
        internal IDictionary<AuthenticationType, int> MinimumRateLimits(QueryType type, RateLimitDbContext dbContext, UserManager<ApplicationUser> userManager, ClaimsPrincipal user) => new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, Endpoints(type).Min(endpoint => { var info = cache[endpoint.Value]; info.ResetIfNeeded(); return info.Limit as int?; }) ?? -1 },
                { AuthenticationType.User, Endpoints(type).Min(endpoint => RateLimitController.GetCurrentUserInfo(dbContext, endpoint.Value, userManager, user)?.Limit as int?) ?? -1 }
            };
    }
}
