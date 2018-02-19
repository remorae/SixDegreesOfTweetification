using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;
using SixDegrees.Model.JSON;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        [HttpGet("status")]
        public IEnumerable<int> GetRateLimitStatus(string endpoint, string forceUpdate)
        {
            if (Enum.TryParse(endpoint, out QueryType type))
            {
                if (QueryHistory.Get[type].RateLimitInfo.SinceLastUpdate > QueryHistory.Get[type].RateLimitInfo.UntilReset)
                    QueryHistory.Get[type].RateLimitInfo.Reset();
                else if (forceUpdate?.ToLower() == "true" || QueryHistory.Get[type].RateLimitInfo.SinceLastUpdate > MaxRateLimitAge)
                    GetUpdatedLimits();
                return new int[] { QueryHistory.Get[type].RateLimitInfo.AppAuthRemaining, QueryHistory.Get[type].RateLimitInfo.UserAuthRemaining };
            }
            else
                return new int[] { -1, -1 };
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
    }
}
