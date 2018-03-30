using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Data;
using SixDegrees.Extensions;
using SixDegrees.Model;
using SixDegrees.Model.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SixDegrees.Controllers
{
    [Route("api/[controller]")]
    public class RateLimitController : Controller
    {
        private static readonly IEnumerable<string> RateLimitResources = new string[] { "account", "followers", "friends", "search", "oauth", "users" };
        private static readonly TimeSpan MaxRateLimitAge = new TimeSpan(0, 5, 0);

        private readonly IConfiguration configuration;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RateLimitDbContext rateLimitDb;

        public RateLimitController(IConfiguration configuration, UserManager<ApplicationUser> userManager, RateLimitDbContext rateLimitDb)
        {
            this.configuration = configuration;
            this.userManager = userManager;
            this.rateLimitDb = rateLimitDb;
        }

        /// <summary>
        /// Returns current rate limiting information for all endpoints.
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        public async Task<IDictionary<QueryType, IDictionary<AuthenticationType, int>>> GetAllRateLimits(string forceUpdate)
        {
            if (forceUpdate?.ToLower() == "true")
                await GetUpdatedLimits(User.GetTwitterAccessToken(), User.GetTwitterAccessTokenSecret());
            return RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User);
        }

        /// <summary>
        /// Returns current rate limiting information for the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The Six Degrees endpoint to retrieve rate limiting info for.</param>
        /// <param name="forceUpdate">Set to "true" to request the most up-to-date info from the Twitter API itself rather than the cache.</param>
        /// <returns></returns>
        [HttpGet("status")]
        public async Task<IDictionary<AuthenticationType, int>> GetRateLimitStatus(string endpoint, string forceUpdate)
        {
            if (endpoint == null)
                return RateLimitCache.BadRateLimit;
            if (Enum.TryParse(endpoint, out QueryType type))
            {
                TimeSpan? timeSinceLastUpdate = RateLimitCache.Get.SinceLastUpdate(type);
                if (timeSinceLastUpdate > RateLimitCache.Get.UntilReset(type))
                    RateLimitCache.Get.Reset(type);
                else if ((forceUpdate?.ToLower() == "true" || timeSinceLastUpdate > MaxRateLimitAge) && RateLimitCache.Get[TwitterAPIEndpoint.RateLimitStatus].Available)
                    await GetUpdatedLimits(User.GetTwitterAccessToken(), User.GetTwitterAccessTokenSecret());
                return RateLimitCache.Get.MinimumRateLimits(type, rateLimitDb, userManager, User);
            }
            else if (Enum.TryParse(endpoint, out TwitterAPIEndpoint apiEndpoint))
            {
                RateLimitCache.Get[apiEndpoint].ResetIfNeeded();
                if (forceUpdate?.ToLower() == "true")
                    await GetUpdatedLimits(User.GetTwitterAccessToken(), User.GetTwitterAccessTokenSecret());
                return new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, RateLimitCache.Get[apiEndpoint].Limit },
                    { AuthenticationType.User, GetCurrentUserInfo(rateLimitDb, apiEndpoint, userManager, User).Limit }
                };
            }
            else
                return RateLimitCache.BadRateLimit;
        }

        private async Task GetUpdatedLimits(string token, string tokenSecret)
        {
            RateLimitCache.Get[TwitterAPIEndpoint.RateLimitStatus].ResetIfNeeded();

            UserRateLimitInfo userInfo = GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.RateLimitStatus, userManager, User);
            string appResponseBody = await TwitterAPIUtils.GetResponse(
                configuration,
                AuthenticationType.Application,
                TwitterAPIEndpoint.RateLimitStatus,
                TwitterAPIUtils.RateLimitStatusQuery(RateLimitResources),
                null,
                null,
                null);
            string userResponseBody = (token == null || tokenSecret == null)
                ? null
                : await TwitterAPIUtils.GetResponse(
                    configuration,
                    AuthenticationType.User,
                    TwitterAPIEndpoint.RateLimitStatus,
                    TwitterAPIUtils.RateLimitStatusQuery(RateLimitResources),
                    token,
                    tokenSecret,
                    userInfo);
            var appResults = (appResponseBody != null) ? RateLimitResults.FromJson(appResponseBody) : null;
            var userResults = (userResponseBody != null) ? RateLimitResults.FromJson(userResponseBody) : null;

            UpdateEndpointLimits(appResults, userResults);
        }

        private void UpdateEndpointLimits(RateLimitResults appResults, RateLimitResults userResults)
        {
            foreach (var endpoint in Enum.GetValues(typeof(TwitterAPIEndpoint)).Cast<TwitterAPIEndpoint>())
            {
                string key = "";
                switch (endpoint)
                {
                    case TwitterAPIEndpoint.RateLimitStatus:
                        continue;
                    case TwitterAPIEndpoint.SearchTweets:
                        key = "/search/tweets";
                        break;
                    case TwitterAPIEndpoint.UsersShow:
                        key = "/users/show/:id";
                        break;
                    case TwitterAPIEndpoint.UsersLookup:
                        key = "/users/lookup";
                        break;
                    case TwitterAPIEndpoint.OAuthAuthorize:
                        key = "/oauth/authorize";
                        break;
                    case TwitterAPIEndpoint.FriendsIDs:
                        key = "/friends/following/ids";
                        break;
                    case TwitterAPIEndpoint.FollowersIDs:
                        key = "/followers/ids";
                        break;
                    default:
                        throw new Exception("Unimplemented TwitterAPIEndpoint");
                }
                RateLimitCache.Get[endpoint].Update(GetLimit(appResults, key));
                var userInfo = GetCurrentUserInfo(rateLimitDb, endpoint, userManager, User);
                if (userInfo != null)
                {
                    userInfo.Update(GetLimit(userResults, key));
                    rateLimitDb.Update(userInfo);
                    rateLimitDb.SaveChanges();
                }
                else
                {
                    var user = new UserRateLimitInfo() { Type = endpoint };
                    user.Update(GetLimit(userResults, key));
                    rateLimitDb.Add(user);
                    rateLimitDb.SaveChanges();
                }
            }
        }

        internal static UserRateLimitInfo GetCurrentUserInfo(RateLimitDbContext rateLimitDb, TwitterAPIEndpoint endpoint, UserManager<ApplicationUser> userManager, ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated || user.GetTwitterAccessToken() == null)
                return null;
            var info = rateLimitDb.Find(typeof(UserRateLimitInfo), new object[] { userManager.GetUserId(user), endpoint }) as UserRateLimitInfo;
            if (info == null)
            {
                info = new UserRateLimitInfo() { UserID = userManager.GetUserId(user), Type = endpoint };
                rateLimitDb.Add(info);
                rateLimitDb.SaveChanges();
            }
            else
            {
                info.ResetIfNeeded();
                rateLimitDb.Update(info);
                rateLimitDb.SaveChanges();
            }

            return info;
        }

        private static int GetLimit(RateLimitResults results, string key)
        {
            string resourceName = key.Substring(1, key.IndexOf('/', 1) - 1);
            return (int)(results?.Resources.GetValueOrDefault(resourceName)?.GetValueOrDefault(key)?.Remaining ?? 0);
        }
    }
}
