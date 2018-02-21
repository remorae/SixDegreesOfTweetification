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
        public IDictionary<QueryType, IDictionary<AuthenticationType, int>> GetAllRateLimits()
        {
            return QueryHistory.Get.RateLimits;
        }

        [HttpGet("status")]
        public IDictionary<AuthenticationType, int> GetRateLimitStatus(string endpoint, string forceUpdate)
        {
            if (Enum.TryParse(endpoint, out QueryType type))
            {
                if (QueryHistory.Get[type].RateLimitInfo.SinceLastUpdate > QueryHistory.Get[type].RateLimitInfo.UntilReset)
                    QueryHistory.Get[type].RateLimitInfo.Reset();
                else if (forceUpdate?.ToLower() == "true" || QueryHistory.Get[type].RateLimitInfo.SinceLastUpdate > MaxRateLimitAge)
                    GetUpdatedLimits();
                return QueryHistory.Get[type].RateLimitInfo.ToDictionary();
            }
            else
                return BadRateLimit();
        }

        private async void GetUpdatedLimits()
        {
            string responseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.Application, TwitterAPIUtils.RateLimitAPIUri(TwitterAPIUtils.RateLimitStatusQuery(new string[] {"users", "search"})));
            if (responseBody == null)
                return;
            var appResults = RateLimitResults.FromJson(responseBody);

            //TODO User authentication
            QueryHistory.Get[QueryType.TweetsByHashtag].RateLimitInfo.Update((int)appResults.Resources.Search.SearchTweets.Remaining, 0);
            QueryHistory.Get[QueryType.LocationsByHashtag].RateLimitInfo.Update((int)appResults.Resources.Search.SearchTweets.Remaining, 0);
            QueryHistory.Get[QueryType.UserByScreenName].RateLimitInfo.Update((int)appResults.Resources.Users["/users/show/:id"].Remaining, 0);
            QueryHistory.Get[QueryType.UserConnectionsByScreenName].RateLimitInfo.Update((int)appResults.Resources.Users["/users/lookup"].Remaining, 0);
        }

        private IDictionary<AuthenticationType, int> BadRateLimit()
        {
            return new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, -1 },
                { AuthenticationType.User, -1 }
            };
        }
    }
}
