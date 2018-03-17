﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SixDegrees.Data;
using SixDegrees.Extensions;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private const int MaxUserLookupCount = 100;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RateLimitDbContext rateLimitDb;

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

        public SearchController(IConfiguration configuration, UserManager<ApplicationUser> userManager, RateLimitDbContext rateLimitDb)
        {
            Configuration = configuration;
            this.userManager = userManager;
            this.rateLimitDb = rateLimitDb;
        }

        private async Task<T> GetResults<T>(string query, AuthenticationType authType, Func<string, TwitterAPIEndpoint, string> buildQueryString, TwitterAPIEndpoint endpoint) where T : IQueryResults
        {
            UserRateLimitInfo userInfo = RateLimitController.GetCurrentUserInfo(rateLimitDb, endpoint, userManager, User);
            string responseBody = await TwitterAPIUtils.GetResponse(
                Configuration,
                authType,
                endpoint,
                buildQueryString(query, endpoint),
                User.GetTwitterAccessToken(),
                User.GetTwitterAccessTokenSecret(),
                userInfo);
            if (userInfo != null)
            {
                rateLimitDb.Update(userInfo);
                rateLimitDb.SaveChanges();
            }
            T results = JsonConvert.DeserializeObject<T>(responseBody);
            LogQuery(query, endpoint, results);
            return results;
        }

        private async Task<IEnumerable<T>> GetResultCollection<T>(IEnumerable<string> queries, AuthenticationType authType, Func<IEnumerable<string>, TwitterAPIEndpoint, string> buildQueryString, TwitterAPIEndpoint endpoint) where T : IQueryResults
        {
            UserRateLimitInfo userInfo = RateLimitController.GetCurrentUserInfo(rateLimitDb, endpoint, userManager, User);
            string responseBody = await TwitterAPIUtils.GetResponse(
                Configuration,
                authType,
                endpoint,
                buildQueryString(queries, endpoint),
                User.GetTwitterAccessToken(),
                User.GetTwitterAccessTokenSecret(),
                userInfo);
            if (userInfo != null)
            {
                rateLimitDb.Update(userInfo);
                rateLimitDb.SaveChanges();
            }
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
         /// Returns a list of hashtags within the given number of "degrees" of the given hashtag.
         /// </summary>
         /// <param name="query">The hashtag to search for.</param>
         /// <param name="numberOfDegrees">The maximum distance between a hashtag and the initial term.</param>
         /// <returns></returns>
        [HttpGet("degrees/hashtags")]
        public async Task<IActionResult> HashtagConnections(string query, int numberOfDegrees = 6)
        {
            if (query == null)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User], 60);
            int callsMade = 0;
            try
            {
                ISet<string> queried = new HashSet<string>();
                Stack<string> remaining = new Stack<string>();
                IDictionary<string, HashtagConnectionInfo> results = new Dictionary<string, HashtagConnectionInfo>
                {
                    { query, new HashtagConnectionInfo(0) }
                };
                remaining.Push(query);
                while (callsMade < maxAPICalls && remaining.Count > 0)
                {
                    ++callsMade;
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    var lookup = (await Hashtags(toQuery) as OkObjectResult).Value as IEnumerable<string>;
                    foreach (var hashtag in lookup.Distinct())
                    {
                        results[toQuery].Connections.Add(hashtag);
                    }
                    int nextDistance = results[toQuery].Distance + 1;
                    foreach (var hashtag in lookup.Distinct().Except(queried).Except(remaining))
                    {
                        if (nextDistance < numberOfDegrees)
                            remaining.Push(hashtag);
                        results[hashtag] = new HashtagConnectionInfo(nextDistance);
                    
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of users within the given number of "degrees" of the given user.
        /// </summary>
        /// <param name="query">The user to search for.</param>
        /// <param name="numberOfDegrees">The maximum distance between a user and the initial username.</param>
        /// <returns></returns>
        [HttpGet("degrees/users")]
        public async Task<IActionResult> UserConnections(string query, int numberOfDegrees = 6)
        {
            if (query == null)
                return BadRequest("Invalid query.");
            int maxAPICalls = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User)[AuthenticationType.User];
            int callsMade = 0;
            try
            {
                ISet<string> queried = new HashSet<string>();
                Stack<string> remaining = new Stack<string>();
                IDictionary<string, UserConnectionInfo> results = new Dictionary<string, UserConnectionInfo>
                {
                    { query, new UserConnectionInfo(0) }
                };
                remaining.Push(query);
                while (callsMade < maxAPICalls && remaining.Count > 0)
                {
                    ++callsMade;
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    var lookup = (await GetUserConnections(toQuery) as OkObjectResult).Value as IEnumerable<UserResult>;
                    foreach (var hashtag in lookup.Distinct())
                    {
                        results[toQuery].Connections.Add(hashtag);
                    }
                    int nextDistance = results[toQuery].Distance + 1;
                    foreach (var user in lookup.Distinct().Where(user => !queried.Contains(user.ScreenName)).Where(user => !remaining.Contains(user.ScreenName)))
                    {
                        if (nextDistance < numberOfDegrees)
                            remaining.Push(user.ScreenName);
                        results[user.ScreenName] = new UserConnectionInfo(nextDistance);

                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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

                int maxLookupCount = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min();
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
