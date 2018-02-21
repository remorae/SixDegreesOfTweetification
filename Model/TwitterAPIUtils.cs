using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SixDegrees.Model
{
    static class TwitterAPIUtils
    {
        private const int TweetCount = 100;
        private const string TweetMode = "extended";
        private const bool IncludeEntities = true;
        private const string ContentType = "application/x-www-form-urlencoded";

        internal static Uri GetUri(TwitterAPIEndpoint endpoint, string query) => new UriBuilder(GetUriString(endpoint)) { Query = query }.Uri;

        private static string GetUriString(TwitterAPIEndpoint endpoint)
        {
            switch (endpoint)
            {
                case TwitterAPIEndpoint.SearchTweets:
                    return "https://api.twitter.com/1.1/search/tweets.json";
                case TwitterAPIEndpoint.UserShow:
                    return "https://api.twitter.com/1.1/users/show.json";
                case TwitterAPIEndpoint.RateLimitStatus:
                    return "https://api.twitter.com/1.1/application/rate_limit_status.json";
                default:
                    return "";
            }
        }

        internal static string UserSearchQuery(string screenName, QueryType type)
        {
            return $"screen_name={screenName}&include_entities={IncludeEntities}";
        }

        internal static string HashtagSearchQuery(string hashtag, QueryType type)
        {
            string result = $"q=%23{hashtag}&count={TweetCount}&tweet_mode={TweetMode}&include_entities={IncludeEntities}";
            if (hashtag == QueryHistory.Get[type].LastQuery && QueryHistory.Get[type].LastMaxID != "")
                result += $"&max_id={QueryHistory.Get[type].LastMaxID}";
            return result;
        }

        internal static string RateLimitStatusQuery(IEnumerable<string> resources)
        {
            return $"resources={string.Join(',', resources)}";
        }

        internal static void AddBearerAuth(IConfiguration config, HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {config["bearerToken"]}");
        }

        internal static async Task<string> GetResponse(IConfiguration config, AuthenticationType authType, TwitterAPIEndpoint endpoint, string query)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, GetUri(endpoint, query)))
                    {
                        if (authType == AuthenticationType.Application)
                            AddBearerAuth(config, request);
                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                        {
                            response.EnsureSuccessStatusCode();
                            if (endpoint != TwitterAPIEndpoint.RateLimitStatus &&
                                response.Headers.TryGetValues("x-rate-limit-remaining", out IEnumerable<string> remaining) &&
                                response.Headers.TryGetValues("x-rate-limit-reset", out IEnumerable<string> reset))
                            {
                                if (int.TryParse(remaining.First(), out int limitRemaining) &&
                                    double.TryParse(reset.First(), out double secondsUntilReset))
                                    RateLimitCache.Get[endpoint].Update(authType, limitRemaining, TimeSpan.FromSeconds(secondsUntilReset));
                            }
                            return await response.Content.ReadAsStringAsync();
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
