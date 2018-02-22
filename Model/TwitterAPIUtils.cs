using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
                case TwitterAPIEndpoint.UserLookup:
                    return "https://api.twitter.com/1.1/users/lookup.json";
                case TwitterAPIEndpoint.OAuthAuthorize:
                    return "https://api.twitter.com/oauth/request_token";
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

        internal static async Task<string> GetResponse(IConfiguration config, AuthenticationType authType, TwitterAPIEndpoint endpoint, string query, string token)
        {
            RateLimitInfo endpointStatus = RateLimitCache.Get[endpoint];
            if (!endpointStatus.Available)
                return null;
            endpointStatus.ResetIfNeeded();
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, GetUri(endpoint, query)))
                    {
                        if (!TryAuthorize(request, config, authType, config["userToken"], endpointStatus, out AuthenticationType? authTypeUsed)) //TODO remove test user token
                            return null;
                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                        {
                            response.EnsureSuccessStatusCode();
                            if (response.Headers.TryGetValues("x-rate-limit-remaining", out IEnumerable<string> remaining) &&
                                response.Headers.TryGetValues("x-rate-limit-reset", out IEnumerable<string> reset))
                            {
                                if (int.TryParse(remaining.FirstOrDefault(), out int limitRemaining) &&
                                    double.TryParse(reset.FirstOrDefault(), out double secondsUntilReset))
                                    RateLimitCache.Get[endpoint].Update(authTypeUsed.Value, limitRemaining, TimeSpan.FromSeconds(secondsUntilReset));
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

        private static bool TryAuthorize(HttpRequestMessage request, IConfiguration config, AuthenticationType authType, string token, RateLimitInfo endpointStatus, out AuthenticationType? used)
        {
            if (UseApplicationAuth(authType, token, endpointStatus))
            {
                AddBearerAuth(config, request);
                used = AuthenticationType.Application;
            }
            else if (UseUserAuth(authType, token, endpointStatus))
            {
                AddUserAuth(config, request, token, config["userSecret"]); //TODO Use user token secret
                used = AuthenticationType.User;
            }
            else
            {
                used = null;
                return false;
            }
            return true;
        }

        private static bool UseApplicationAuth(AuthenticationType authType, string token, RateLimitInfo endpointStatus)
        {
            return endpointStatus.IsAvailable(AuthenticationType.Application) &&
                (authType == AuthenticationType.Application ||
                (authType == AuthenticationType.Both && (null == token || !endpointStatus.IsAvailable(AuthenticationType.User))));
        }

        private static bool UseUserAuth(AuthenticationType authType, string token, RateLimitInfo endpointStatus)
        {
            return endpointStatus.IsAvailable(AuthenticationType.User) && token != null &&
                (authType == AuthenticationType.User || authType == AuthenticationType.Both);
        }

        private static void AddBearerAuth(IConfiguration config, HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {config["bearerToken"]}");
        }

        private static void AddUserAuth(IConfiguration config, HttpRequestMessage request, string oauthToken, string oauthTokenSecret)
        {
            IDictionary<string, string> pairs
                = new SortedDictionary<string, string>(
                    request.RequestUri.Query?.Substring(1).Split("&")
                    .ToDictionary(param => param.Split("=")[0], param => Uri.UnescapeDataString(param.Split("=")[1]))
                    ?? new Dictionary<string, string>())
            {
                { "oauth_consumer_key", config["consumerKey"] },
                { "oauth_nonce", Guid.NewGuid().ToString("N") },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "oauth_token", oauthToken },
                { "oauth_version", "1.0" },
            };

            Debug.WriteLine(request.RequestUri.OriginalString);
            string parameters = string.Join("&", pairs.Select((pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")));
            string signatureBase = $"{request.Method.ToString().ToUpper()}&{Uri.EscapeDataString(request.RequestUri.GetLeftPart(UriPartial.Path))}&{Uri.EscapeDataString(parameters)}";
            string signingKey = $"{Uri.EscapeDataString(config["consumerSecret"])}&{((oauthTokenSecret != null) ? Uri.EscapeDataString(oauthTokenSecret) : "")}";
            using (HMAC sha = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
                pairs.Add("oauth_signature", Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))));

            request.Headers.Add("Authorization", OAuthHeader(pairs));
        }

        private static string OAuthHeader(IDictionary<string, string> oauthInfo)
        {
            return string.Join(", ",
                "OAuth " + HeaderKVP(oauthInfo, "oauth_consumer_key"),
                HeaderKVP(oauthInfo, "oauth_nonce"),
                HeaderKVP(oauthInfo, "oauth_signature"),
                HeaderKVP(oauthInfo, "oauth_signature_method"),
                HeaderKVP(oauthInfo, "oauth_timestamp"),
                HeaderKVP(oauthInfo, "oauth_token"),
                HeaderKVP(oauthInfo, "oauth_version"));
        }

        private static string HeaderKVP(IDictionary<string, string> pairs, string key)
        {
            return $"{key}=\"{Uri.EscapeDataString(pairs[key])}\"";
        }
    }
}
