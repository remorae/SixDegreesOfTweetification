using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SixDegrees.Model
{
    public static class TwitterAPIUtils
    {
        private const int TweetCount = 100;
        private const string TweetMode = "extended";
        private const bool IncludeEntities = true;
        private const string ContentType = "application/x-www-form-urlencoded";

        public static Uri UserSearchAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/users/show.json")
            {
                Query = query
            };
            return bob.Uri;
        }

        public static Uri TweetSearchAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/search/tweets.json")
            {
                Query = query
            };
            return bob.Uri;
        }

        public static Uri RateLimitAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/application/rate_limit_status.json")
            {
                Query = query
            };
            return bob.Uri;
        }

        public static string UserSearchQuery(string screenName, QueryType type)
        {
            return $"screen_name={screenName}&include_entities={IncludeEntities}";
        }

        public static string HashtagSearchQuery(string hashtag, QueryType type)
        {
            string result = $"q=%23{hashtag}&count={TweetCount}&tweet_mode={TweetMode}&include_entities={IncludeEntities}";
            if (hashtag == QueryHistory.Get[type].LastQuery && QueryHistory.Get[type].LastMaxID != "")
                result += $"&max_id={QueryHistory.Get[type].LastMaxID}";
            return result;
        }

        public static string RateLimitStatusQuery(IEnumerable<string> resources)
        {
            return $"resources={string.Join(',', resources)}";
        }

        public static void AddBearerAuth(IConfiguration config, HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {config["bearerToken"]}");
        }
    }
}
