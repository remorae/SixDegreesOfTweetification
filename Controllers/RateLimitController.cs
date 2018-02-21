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
                else if (forceUpdate?.ToLower() == "true" || timeSinceLastUpdate > MaxRateLimitAge)
                    GetUpdatedLimits();
                return RateLimitCache.Get.MinimumRateLimits(type);
            }
            else
                return RateLimitCache.BadRateLimit();
        }

        private async void GetUpdatedLimits()
        {
            string responseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.Application, TwitterAPIEndpoint.RateLimitStatus, TwitterAPIUtils.RateLimitStatusQuery(new string[] {"users", "search"}));
            if (responseBody == null)
                return;
            var appResults = RateLimitResults.FromJson(responseBody);

            //TODO User authentication
            RateLimitCache.Get[TwitterAPIEndpoint.SearchTweets].Update((int)appResults.Resources.Search.SearchTweets.Remaining, 0);
            RateLimitCache.Get[TwitterAPIEndpoint.UserShow].Update((int)appResults.Resources.Users["/users/show/:id"].Remaining, 0);
            RateLimitCache.Get[TwitterAPIEndpoint.UserLookup].Update((int)appResults.Resources.Users["/users/lookup"].Remaining, 0);
        }
    }
}
