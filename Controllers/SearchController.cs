using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SixDegrees.Extensions;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private const int MaxUserLookupCount = 100;

        private static void LogQuery(string query, TwitterAPIEndpoint endpoint, IQueryResults results)
        {
            QueryHistory.Get[endpoint].LastQuery = query;
            if (QueryInfo.UsesMaxID(endpoint))
            {
                TweetSearchResults statusResults = results as TweetSearchResults;
                // Exclude lowest ID to prevent duplicate results
                string nextMaxID = (long.TryParse(statusResults.MinStatusID, out long result)) ? (result - 1).ToString() : "";
                QueryHistory.Get[endpoint].NextMaxID = nextMaxID;
            }
            if (QueryInfo.UsesCursor(endpoint))
            {
                UserIdsResults idResults = results as UserIdsResults;
                QueryHistory.Get[endpoint].NextCursor = idResults.NextCursorStr;
            }
        }

        private static void LogQuerySet(IEnumerable<string> querySet, TwitterAPIEndpoint endpoint, IEnumerable<IQueryResults> resultSet)
        {
            QueryHistory.Get[endpoint].LastQuerySet = querySet;
        }

        private static void UpdateCountriesWithPlace(IDictionary<string, Country> countries, Status status)
        {
            string placeName = status.Place.FullName;

            string countryName = status.Place.Country;
            if (!countries.ContainsKey(countryName))
                countries[countryName] = new Country(countryName);
            if (!countries[countryName].Places.ContainsKey(placeName))
            {
                PlaceResult toAdd = new PlaceResult() { Name = placeName, Type = status.Place.PlaceType, Country = countryName };
                countries[countryName].Places[placeName] = toAdd;
            }
            countries[countryName].Places[placeName].Sources.Add(status.URL);
            foreach (Hashtag tag in status.Entities.Hashtags)
            {
                if (!countries[countryName].Places[placeName].Hashtags.Contains(tag.Text))
                    countries[countryName].Places[placeName].Hashtags.Add(tag.Text);
            }
        }

        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private async Task<T> GetResults<T>(string query, AuthenticationType authType, Func<string, TwitterAPIEndpoint, string> buildQueryString, TwitterAPIEndpoint endpoint) where T : IQueryResults
        {
            string responseBody = await TwitterAPIUtils.GetResponse(
                Configuration,
                authType,
                endpoint,
                buildQueryString(query, endpoint),
                User.GetTwitterAccessToken(),
                User.GetTwitterAccessTokenSecret());
            T results = JsonConvert.DeserializeObject<T>(responseBody);
            LogQuery(query, endpoint, results);
            return results;
        }

        private async Task<IEnumerable<T>> GetResultCollection<T>(IEnumerable<string> queries, AuthenticationType authType, Func<IEnumerable<string>, TwitterAPIEndpoint, string> buildQueryString, TwitterAPIEndpoint endpoint) where T : IQueryResults
        {
            string responseBody = await TwitterAPIUtils.GetResponse(
                Configuration,
                authType,
                endpoint,
                buildQueryString(queries, endpoint),
                User.GetTwitterAccessToken(),
                User.GetTwitterAccessTokenSecret());
            T[] results = JsonConvert.DeserializeObject<T[]>(responseBody);
            LogQuerySet(queries, endpoint, results.Cast<IQueryResults>());
            return results;
        }

        /// <summary>
        /// Returns a list of tweets containing given hashtags.
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces.</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IActionResult> Tweets(string query)
        {
            try
            {
                var results = await GetResults<TweetSearchResults>(
                    query,
                    AuthenticationType.Both,
                    TwitterAPIUtils.HashtagSearchQuery,
                    TwitterAPIEndpoint.SearchTweets);
                return Ok(results.Statuses);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of hashtags associated with the given hashtag.
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces.</param>
        /// <returns></returns>
        [HttpGet("hashtags")]
        public async Task<IActionResult> Hashtags(string query)
        {
            try
            {
                var results = await GetResults<TweetSearchResults>(
                    query,
                    AuthenticationType.Both,
                    TwitterAPIUtils.HashtagSearchQuery,
                    TwitterAPIEndpoint.SearchTweets);

                var hashtags = results.Statuses
                    .Aggregate(new HashSet<Hashtag>(), (set, status) => AppendHashtagsInStatus(status, set))
                    .Select(hashtag => hashtag.Text.ToLower());
                return Ok(hashtags);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static HashSet<Hashtag> AppendHashtagsInStatus(Status status, HashSet<Hashtag> set)
        {
            set.UnionWith(status.Entities.Hashtags.AsEnumerable());
            return set;
        }

        /// <summary>
        /// Returns a list of locations from tweets containing given hashtags.
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces.</param>
        /// <returns></returns>
        [HttpGet("locations")]
        public async Task<IActionResult> Locations(string query)
        {
            try
            {
                var results = await GetResults<TweetSearchResults>(
                    query,
                    AuthenticationType.Both,
                    TwitterAPIUtils.HashtagSearchQuery,
                    TwitterAPIEndpoint.SearchTweets);
                IDictionary<string, Country> countries = new Dictionary<string, Country>();
                foreach (Status status in results.Statuses)
                {
                    if (status.Place != null)
                        UpdateCountriesWithPlace(countries, status);
                    else if (status.Coordinates != null && status.Coordinates.Type == "Point")
                    {
                        //TODO - Look up city/country names based on longitude/latitude
                    }
                }
                return Ok(GetFormattedCountries(countries.Values));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private IEnumerable<CountryResult> GetFormattedCountries(IEnumerable<Country> countries)
        {
            return countries.Select(country => new CountryResult() { Name = country.Name, Places = country.Places.Values });
        }

        /// <summary>
        /// Returns information about a specified Twitter user.
        /// </summary>
        /// <param name="screen_name">The user screen name to search for.</param>
        /// <returns></returns>
        [HttpGet("user")]
        public async Task<IActionResult> GetUser(string screen_name)
        {
            try
            {
                var results = await GetResults<UserSearchResults>(
                    screen_name,
                    AuthenticationType.Both,
                    TwitterAPIUtils.UserSearchQuery,
                    TwitterAPIEndpoint.UsersShow);
                return Ok(ToUserResult(results));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static UserResult ToUserResult(UserSearchResults results)
        {
            return new UserResult()
            {
                CreatedAt = results.CreatedAt,
                Description = results.Description,
                FollowerCount = results.FollowersCount,
                FriendCount = results.FriendsCount,
                GeoEnabled = results.GeoEnabled,
                ID = results.IdStr,
                Lang = results.Lang,
                Location = results.Location,
                Name = results.Name,
                ProfileImage = results.ProfileImageUrlHttps,
                ScreenName = results.ScreenName,
                StatusCount = results.StatusesCount,
                TimeZone = results.TimeZone,
                Verified = results.Verified
            };
        }

        /// <summary>
        /// Returns users that are friends or followers of the specified Twitter user.
        /// </summary>
        /// <param name="screen_name">The user to get connections for.</param>
        /// <param name="limit">The maximum number of users to return.</param>
        /// <returns></returns>
        [HttpGet("user/connections")]
        public async Task<IActionResult> GetUserConnections(string screen_name, int limit = MaxUserLookupCount)
        {
            try
            {
                if (QueryHistory.Get[TwitterAPIEndpoint.FollowersIDs].LastQuery == screen_name)
                    return BadRequest("Cannot repeat user connection queries."); //TODO Cache results and return those?

                int maxLookupCount = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName).Values.Min();
                limit = Math.Min(limit, maxLookupCount * MaxUserLookupCount);
                
                var followerResults = await GetResults<UserIdsResults>(
                    screen_name,
                    AuthenticationType.Both,
                    TwitterAPIUtils.FollowersFriendsIDsQuery,
                    TwitterAPIEndpoint.FollowersIDs);
                ISet<long> uniqueIds = new HashSet<long>(followerResults?.Ids ?? Enumerable.Empty<long>());

                if (followerResults != null && followerResults.Ids.Count() < limit)
                {
                    var friendResults = await GetResults<UserIdsResults>(
                        screen_name,
                        AuthenticationType.Both,
                        TwitterAPIUtils.FollowersFriendsIDsQuery,
                        TwitterAPIEndpoint.FriendsIDs);
                    if (friendResults != null)
                        foreach (long id in friendResults.Ids)
                            if (!uniqueIds.Contains(id))
                                uniqueIds.Add(id);
                }

                Queue<long> ids = new Queue<long>(uniqueIds);
                ICollection<UserResult> results = new List<UserResult>();
                while (limit > 0 && ids.Count > 0)
                {
                    IEnumerable<long> toLookup = ids.Take(Math.Min(ids.Count, limit)).ToList();
                    int lookupCount = toLookup.Count();
                    limit -= lookupCount;
                    for (int i = 0; i < lookupCount; ++i)
                        ids.Dequeue();
                    var userResults = await GetResultCollection<UserSearchResults>(
                        toLookup.Select(id => id.ToString()),
                        AuthenticationType.Both,
                        TwitterAPIUtils.UserLookupQuery,
                        TwitterAPIEndpoint.UsersLookup);
                    foreach (var user in userResults)
                        results.Add(ToUserResult(user));
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
