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
        public async Task<IEnumerable<int>> GetRateLimitStatus(string endpoint, string forceUpdate)
        {
            if (Enum.TryParse(endpoint, out QueryType type))
            {
                if (forceUpdate.ToLower() == "true" || QueryHistory.Get[type].RateLimitInfo.SinceLastUpdate > MaxRateLimitAge)
                {
                    (int appRemaining, int userRemaining) limits = await GetUpdatedLimits(type);
                    QueryHistory.Get[type].RateLimitInfo.Update(limits.appRemaining, limits.userRemaining);
                }
                return new int[] { QueryHistory.Get[type].RateLimitInfo.AppAuthRemaining, QueryHistory.Get[type].RateLimitInfo.UserAuthRemaining };
            }
            else
                return new int[] { -1, -1 };
        }

        private async Task<(int appRemaining, int userRemaining)> GetUpdatedLimits(QueryType type)
        {
            string responseBody = await TwitterAPIUtils.GetResponse(Configuration, AuthenticationType.Application, TwitterAPIUtils.RateLimitAPIUri(TwitterAPIUtils.RateLimitStatusQuery(new string[] {"users", "search"})));
            if (responseBody == null)
                return (-1, -1);
            var appResults = RateLimitResults.FromJson(responseBody);
            switch (type)
            {
                case QueryType.TweetsByHashtag:
                case QueryType.LocationsByHashtag:
                    return ((int)appResults.Resources.Search.SearchTweets.Remaining, 0);
                case QueryType.UserByScreenName:
                    return ((int)appResults.Resources.Users["/users/show/:id"].Remaining, 0);
                case QueryType.UserConnectionsByScreenName:
                    return ((int)appResults.Resources.Users["/users/lookup"].Remaining, 0);
                default:
                    return (-1, -1);
            }
        }
    }
}
