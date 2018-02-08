using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private const int TWEET_COUNT = 100;
        private const string TWEET_MODE = "extended";
        private const bool INCLUDE_ENTITIES = true;
        private const string CONTENT_TYPE = "application/x-www-form-urlencoded";

        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/search/tweets.json")
            {
                Query = $"q={query}&count={TWEET_COUNT}&tweet_mode={TWEET_MODE}&include_entities={INCLUDE_ENTITIES}"
            };
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, bob.Uri))
                    {
                        request.Headers.Add("Authorization", $"Bearer {Configuration["bearerToken"]}");
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                        {
                            response.EnsureSuccessStatusCode();
                            string body = await response.Content.ReadAsStringAsync();
                            TweetSearch results = JsonConvert.DeserializeObject<TweetSearch>(body);
                            return results.Statuses;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
