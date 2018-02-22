using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;
using SixDegrees.Model.JSON;
using System;
using System.Collections.Generic;

namespace SixDegrees.Controllers
{
    [Route("api/[controller]")]
    public class RateLimitController : Controller
    {
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

            string appResponseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.Application, TwitterAPIEndpoint.RateLimitStatus, TwitterAPIUtils.RateLimitStatusQuery(new string[] {"users", "search"}), null);
            string userResponseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.User, TwitterAPIEndpoint.RateLimitStatus, TwitterAPIUtils.RateLimitStatusQuery(new string[] { "users", "search" }), token);
            var appResults = (appResponseBody != null) ? RateLimitResults.FromJson(appResponseBody) : null;
            var userResults = (userResponseBody != null) ? RateLimitResults.FromJson(userResponseBody) : null;

            RateLimitCache.Get[TwitterAPIEndpoint.SearchTweets].Update(
                (int)(appResults?.Resources.Search.SearchTweets.Remaining ?? 0),
                (int)(userResults?.Resources.Search.SearchTweets.Remaining ?? 0));
            RateLimitCache.Get[TwitterAPIEndpoint.UserShow].Update(
                GetLimit(appResults, "/users/show/:id"),
                GetLimit(userResults, "/users/show/:id"));
            RateLimitCache.Get[TwitterAPIEndpoint.UserLookup].Update(
                GetLimit(appResults, "/users/lookup"),
                GetLimit(userResults, "/users/lookup"));
        }

        private static int GetLimit(RateLimitResults results, string key)
        {
            return (int)(results?.Resources.Users.GetValueOrDefault(key)?.Remaining ?? 0);
        }
    }
}
