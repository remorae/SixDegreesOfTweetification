using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;
using SixDegrees.Model.JSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Controllers
{
    [Route("api/[controller]")]
    public class RateLimitController : Controller
    {
        private static readonly IEnumerable<string> RateLimitResources = new string[] { "account", "followers", "friends", "search", "oauth", "users" };
        private static readonly TimeSpan MaxRateLimitAge = new TimeSpan(0, 5, 0);

        private IConfiguration Configuration { get; }

        public RateLimitController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpGet("all")]
        public IDictionary<QueryType, IDictionary<AuthenticationType, int>> GetAllRateLimits() => RateLimitCache.Get.CurrentRateLimits;

        [HttpGet("status")]
        public IDictionary<AuthenticationType, int> GetRateLimitStatus(string endpoint, string forceUpdate)
        {
            if (Enum.TryParse(endpoint, out QueryType type))
            {
                TimeSpan? timeSinceLastUpdate = RateLimitCache.Get.SinceLastUpdate(type);
                if (timeSinceLastUpdate > RateLimitCache.Get.UntilReset(type))
                    RateLimitCache.Get.Reset(type);
                else if ((forceUpdate?.ToLower() == "true" || timeSinceLastUpdate > MaxRateLimitAge) && RateLimitCache.Get[TwitterAPIEndpoint.RateLimitStatus].Available)
                    GetUpdatedLimits(null); //TODO Use user token
                return RateLimitCache.Get.MinimumRateLimits(type);
            }
            else
                return RateLimitCache.BadRateLimit;
        }

        private async void GetUpdatedLimits(string token)
        {
            RateLimitCache.Get[TwitterAPIEndpoint.RateLimitStatus].ResetIfNeeded();

            string appResponseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.Application, TwitterAPIEndpoint.RateLimitStatus, TwitterAPIUtils.RateLimitStatusQuery(RateLimitResources), null);
            string userResponseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.User, TwitterAPIEndpoint.RateLimitStatus, TwitterAPIUtils.RateLimitStatusQuery(RateLimitResources), token);
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
                RateLimitCache.Get[endpoint].Update(GetLimit(appResults, key), GetLimit(userResults, key));
            }
        }

        private static int GetLimit(RateLimitResults results, string key)
        {
            string resourceName = key.Substring(1, key.IndexOf('/', 1) - 1);
            return (int)(results?.Resources.GetValueOrDefault(resourceName)?.GetValueOrDefault(key)?.Remaining ?? 0);
        }
    }
}
