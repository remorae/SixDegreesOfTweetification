using System;
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
    /// <summary>
    /// Handles searches for Twitter data from the front-end.
    /// </summary>
    [Route("api/search")]
    public class SearchController : Controller
    {
        internal const int MaxSingleQueryUserLookupCount = 100;
        private const string BlacklistFile = "blacklist.txt";
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
                Model.Place toAdd = new Model.Place() { Name = placeName, Type = status.Place.PlaceType, Country = countryName };
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
                userInfo.ResetIfNeeded();
                rateLimitDb.Update(userInfo);
                rateLimitDb.SaveChanges();
            }
            if (responseBody == null)
                return default;
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
                userInfo.ResetIfNeeded();
                rateLimitDb.Update(userInfo);
                rateLimitDb.SaveChanges();
            }
            T[] results = JsonConvert.DeserializeObject<T[]>(responseBody);
            LogQuerySet(queries, endpoint, results.Cast<IQueryResults>());
            return results;
        }

        /// <summary>
        /// Returns tweet search results for the given hashtag.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IActionResult> GetTweets(string query)
        {
            if (query == null)
                return BadRequest("Invalid parameters.");
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
        /// Returns search results for the given hashtag with any associated hashtags and the source tweet urls organized by location.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations(string query)
        {
            if (query == null)
                return BadRequest("Invalid parameters.");
            try
            {
                var results = await GetResults<TweetSearchResults>(
                    query,
                    AuthenticationType.Both,
                    TwitterAPIUtils.HashtagSearchQuery,
                    TwitterAPIEndpoint.SearchTweets);
                IDictionary<string, Country> countries = new Dictionary<string, Country>();
                var coords = new Dictionary<Coordinates, (ISet<string> hashtags, ICollection<string> sources)>();
                foreach (Status status in results.Statuses)
                {
                    if (status.Place != null)
                        UpdateCountriesWithPlace(countries, status);
                    else if (status.Coordinates != null)
                        UpdateCoordinatesWithStatus(coords, status);
                }
                return Ok(new
                {
                    Countries = countries.Values.Select(country => new { country.Name, Places = country.Places.Values }),
                    CoordinateInfo = coords.Select(
                        entry => new
                        {
                            Coordinates = new { CoordType = entry.Key.Type, X = entry.Key.Value[0], Y = entry.Key.Value[1] },
                            Hashtags = entry.Value.hashtags.ToArray(),
                            Sources = entry.Value.sources.ToArray()
                        })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private void UpdateCoordinatesWithStatus(Dictionary<Coordinates, (ISet<string> sources, ICollection<string> hashtags)> coords, Status status)
        {
            Coordinates toUpdate = status.Coordinates;
            if (!coords.ContainsKey(toUpdate))
                coords[toUpdate] = (new HashSet<string>(), new List<string>());
            coords[toUpdate].sources.Add(status.URL);
            foreach (Hashtag tag in status.Entities.Hashtags)
            {
                if (!coords[toUpdate].hashtags.Contains(tag.Text))
                    coords[toUpdate].hashtags.Add(tag.Text);
            }
        }

        /// <summary>
        /// Returns a list of hashtags (will contain duplicates) associated directly (contained within the same tweet) with the given hashtag.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("hashtags")]
        public async Task<IActionResult> GetHashtags(string query)
        {
            if (query == null)
                return BadRequest("Invalid parameters.");
            try
            {
                var results = await GetResults<TweetSearchResults>(
                    query,
                    AuthenticationType.Both,
                    TwitterAPIUtils.HashtagSearchQuery,
                    TwitterAPIEndpoint.SearchTweets);

                var hashtags = results.Statuses
                    .Aggregate(new HashSet<Hashtag>(), AppendHashtagsInStatus)
                    .Select(hashtag => hashtag.Text.ToLower());
                return Ok(FilterHashtags(hashtags));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private IEnumerable<string> FilterHashtags(IEnumerable<string> hashtags)
        {
            var filtered = hashtags.ToList();
            try
            {
                foreach (string blacklist in System.IO.File.ReadLines(BlacklistFile))
                    filtered.RemoveAll(tag => (tag.Length - tag.Replace(blacklist, string.Empty).Length) / blacklist.Length > 0);
            }
            catch (System.IO.IOException)
            {
                return filtered;
            }
            return filtered;
        }

        private static HashSet<Hashtag> AppendHashtagsInStatus(HashSet<Hashtag> set, Status status)
        {
            set.UnionWith(status.Entities.Hashtags.AsEnumerable());
            return set;
        }

        /// <summary>
        /// Returns information about a specified Twitter user.
        /// </summary>
        /// <seealso cref="GetUserByID(string)"/>
        /// <param name="screen_name">The screen name of the Twitter user to search for.</param>
        /// <returns></returns>
        [HttpGet("user")]
        public async Task<IActionResult> GetUser(string screen_name)
        {
            if (screen_name == null)
                return BadRequest("Invalid parameters.");
            try
            {
                // Look for a cached user, but ensure they have been looked up (and don't just have a cached ID)
                if (TwitterCache.LookupUserByName(Configuration, screen_name) is TwitterUser cached && cached.ScreenName != null)
                    return Ok(cached);
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

        /// <summary>
        /// Returns information about a specified Twitter user.
        /// </summary>
        /// <seealso cref="GetUser(string)"/>
        /// <param name="user_id">The ID of the Twitter user to search for.</param>
        /// <returns></returns>
        [HttpGet("userID")]
        public async Task<IActionResult> GetUserByID(string user_id)
        {
            if (user_id == null)
            {
                return BadRequest("Invalid parameters");
            }
            try
            {
                var result = await GetResults<UserSearchResults>(
                    user_id,
                    AuthenticationType.Both,
                    TwitterAPIUtils.UserIDSearchQuery,
                    TwitterAPIEndpoint.UsersShow);
                return Ok(ToUserResult(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private static TwitterUser ToUserResult(UserSearchResults results)
        {
            if (results == null)
                return null;
            return new TwitterUser()
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
        /// Returns information about the friends and followers of the specified user.
        /// </summary>
        /// <param name="screen_name">The screen name of the Twitter user to search for (minus the '@').</param>
        /// <param name="limit">The maximum number of users to return.
        /// The maximum number of friends/followers to lookup in one query is 1500 (for each) due to rate limiting.</param>
        /// <param name="allowAPICalls">Whether or not Twitter API calls are allowed.</param>
        /// <returns></returns>
        [HttpGet("user/connections")]
        public async Task<IActionResult> GetUserConnections(string screen_name, int limit = MaxSingleQueryUserLookupCount, bool allowAPICalls = true)
        {
            if (screen_name == null || limit < 1)
                return BadRequest("Invalid parameters.");
            try
            {
                int maxLookupCount = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min();
                int lookupLimit = Math.Min(limit, maxLookupCount * MaxSingleQueryUserLookupCount);

                TwitterUser queried = (await GetUser(screen_name) as OkObjectResult)?.Value as TwitterUser;
                string userID = queried?.ID;
                if (userID == null)
                    return BadRequest("Invalid user screen name.");

                if (TwitterCache.UserConnectionsQueried(Configuration, queried) || !allowAPICalls)
                {
                    IEnumerable<TwitterUser> cachedResults = TwitterCache.FindUserConnections(Configuration, queried).Take(limit);
                    TwitterCache.UpdateUsers(Configuration, await LookupIDs(maxLookupCount * MaxSingleQueryUserLookupCount, cachedResults.Where(user => user.ScreenName == null).Select(user => user.ID).ToList()));
                    // Now that all cached users have been looked up, return the updated cached results.
                    return Ok(TwitterCache.FindUserConnections(Configuration, queried).Take(limit));
                }

                var remainingIDsToLookup = ((await GetUserConnectionIDs(userID) as OkObjectResult)?.Value as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList();
                ICollection<TwitterUser> results = await LookupIDs(lookupLimit, remainingIDsToLookup);
                TwitterCache.UpdateUserConnections(Configuration, queried, results);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        internal async Task<ICollection<TwitterUser>> LookupIDs(int limit, List<string> remainingIDsToLookup)
        {
            ICollection<TwitterUser> results = new List<TwitterUser>();
            while (remainingIDsToLookup.Count > 0 && results.Count < limit)
            {
                int count = Math.Min(100, remainingIDsToLookup.Count);
                var searchResults = await GetResultCollection<UserSearchResults>(
                    remainingIDsToLookup.Take(count),
                    AuthenticationType.Both,
                    TwitterAPIUtils.UserLookupQuery,
                    TwitterAPIEndpoint.UsersLookup) ?? Enumerable.Empty<UserSearchResults>();
                remainingIDsToLookup.RemoveRange(0, count);
                foreach (var searchResult in searchResults.Take(limit - results.Count))
                    results.Add(ToUserResult(searchResult));
            }

            return results;
        }

        /// <summary>
        /// Returns IDs of the friends and followers of the specified user.
        /// </summary>
        /// <param name="user_id">The ID of the Twitter user to search for.</param>
        /// <param name="allowAPICalls">Whether or not Twitter API calls are allowed.</param>
        /// <returns></returns>
        [HttpGet("user/connectionids")]
        public async Task<IActionResult> GetUserConnectionIDs(string user_id, bool allowAPICalls = true)
        {
            if (user_id == null)
                return BadRequest("Invalid parameters.");
            try
            {
                if (TwitterCache.UserConnectionsQueried(Configuration, user_id) || !allowAPICalls)
                    return Ok(TwitterCache.FindUserConnectionIDs(Configuration, user_id));

                ISet<long> uniqueIds = new HashSet<long>();
                uniqueIds.UnionWith((await GetFollowerIDs(user_id))?.Ids ?? Enumerable.Empty<long>());
                uniqueIds.UnionWith((await GetFriendIDs(user_id))?.Ids ?? Enumerable.Empty<long>());

                TwitterCache.UpdateUserConnections(Configuration, user_id, uniqueIds.Select(id => id.ToString()));
                return Ok(uniqueIds.Select(id => id.ToString()));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<UserIdsResults> GetFriendIDs(string userID)
        {
            return await GetResults<UserIdsResults>(
                userID,
                AuthenticationType.User,
                TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                TwitterAPIEndpoint.FriendsIDs);
        }

        private async Task<UserIdsResults> GetFollowerIDs(string userID)
        {
            return await GetResults<UserIdsResults>(
                userID,
                AuthenticationType.User,
                TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                TwitterAPIEndpoint.FollowersIDs);
        }

        /// <summary>
        /// Returns a list of hashtags within the given number of "degrees" of the given hashtag (each tweet represents one degree).
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <param name="maxNodeConnections">The maximum integer number of per-node connections to handle.</param>
        /// <returns></returns>
        [HttpGet("degrees/hashtags/single")]
        public async Task<IActionResult> GetSingleHashtagConnections(string query, int numberOfDegrees = 6, int maxCalls = 60, int maxNodeConnections = 500)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1 || maxNodeConnections < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User]);
            int callsMade = 0;
            try
            {
                ISet<string> queried = new HashSet<string>();
                Stack<string> remaining = new Stack<string>();
                IDictionary<string, SingleConnection<string>> results = new Dictionary<string, SingleConnection<string>>
                {
                    { query, new SingleConnection<string>(0) }
                };
                remaining.Push(query);
                while (callsMade < maxAPICalls && remaining.Count > 0)
                {
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    int previousRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.SearchTweets, userManager, User).Limit;
                    if (await GetUniqueHashtags(toQuery, maxNodeConnections) is IDictionary<Status, IEnumerable<string>> lookup)
                    {
                        int newRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.SearchTweets, userManager, User).Limit;
                        if (newRateLimit < previousRateLimit)
                            ++callsMade;
                        var lookupHashtags = lookup.Aggregate(new HashSet<string>(), AppendValuesInStatus);
                        foreach (var hashtag in lookupHashtags)
                        {
                            results[toQuery].Connections.Add(hashtag);
                            if (results[toQuery].Connections.Count >= maxNodeConnections)
                                break;
                        }
                        int nextDistance = results[toQuery].DistanceFromStart + 1;
                        foreach (var hashtag in lookupHashtags.Except(queried).Except(remaining).Take(maxNodeConnections))
                        {
                            if (nextDistance < numberOfDegrees)
                                remaining.Push(hashtag);
                            results[hashtag] = new SingleConnection<string>(nextDistance);
                        }
                    }
                }

                return Ok(results.ToDictionary(entry => entry.Key, entry => entry.Value.Connections));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        internal static HashSet<T> AppendValuesInStatus<T>(HashSet<T> set, KeyValuePair<Status, IEnumerable<T>> statusConnections)
        {
            return set.Union(statusConnections.Value).ToHashSet();
        }

        /// <summary>
        /// Returns a list of users within the given number of "degrees" of the user with the given screen name
        /// (a link to a user's followers/friends represents one degree).
        /// </summary>
        /// <param name="query">The screen name to search for (minus the '@').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <param name="maxNodeConnections">The maximum integer number of per-node connections to handle.</param>
        /// <returns></returns>
        [HttpGet("degrees/users/single")]
        public async Task<IActionResult> GetSingleUserConnections(string query, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 50)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1 || maxNodeConnections < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User)[AuthenticationType.User]);
            int callsMade = 0;
            try
            {
                ISet<string> queried = new HashSet<string>();
                Stack<string> remaining = new Stack<string>();
                IDictionary<string, SingleConnection<TwitterUser>> results = new Dictionary<string, SingleConnection<TwitterUser>>
                {
                    { query, new SingleConnection<TwitterUser>(0) }
                };
                remaining.Push(query);
                while ((callsMade == 0 || callsMade < maxAPICalls) && remaining.Count > 0)
                {
                    int previousRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.FollowersIDs, userManager, User).Limit;
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    if (await GetUserConnections(toQuery) is OkObjectResult result && result.Value is IEnumerable<TwitterUser> lookup)
                    {
                        lookup = lookup.Distinct().Where(user => user.ScreenName != null);
                        int newRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.FollowersIDs, userManager, User).Limit;
                        if (newRateLimit < previousRateLimit)
                            ++callsMade;
                        foreach (var user in lookup)
                        {
                            results[toQuery].Connections.Add(user);
                            if (results[toQuery].Connections.Count >= maxNodeConnections)
                                break;
                        }
                        int nextDistance = results[toQuery].DistanceFromStart + 1;
                        foreach (var user in lookup.Where(user => !queried.Contains(user.ScreenName)).Where(user => !remaining.Contains(user.ScreenName)).Take(maxNodeConnections))
                        {
                            if (nextDistance < numberOfDegrees)
                                remaining.Push(user.ScreenName);
                            results[user.ScreenName] = new SingleConnection<TwitterUser>(nextDistance);
                        }
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
        /// Attempts to find a link between the two hashtags (each tweet represents one degree).
        /// </summary>
        /// <param name="hashtag1">The starting hashtag (minus the '#').</param>
        /// <param name="hashtag2">The ending hashtag (minus the '#').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <param name="maxNodeConnections">The maximum integer number of per-node connections to handle.</param>
        /// <returns></returns>
        [HttpGet("degrees/hashtags")]
        public async Task<IActionResult> GetHashtagLink(string hashtag1, string hashtag2, int numberOfDegrees = 6, int maxCalls = 60, int maxNodeConnections = 500)
        {
            if (hashtag1 == null || hashtag2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid query.");

            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User]);

            return await new HashtagLinkFinder(Configuration, this, rateLimitDb, userManager, maxNodeConnections)
                .Execute(hashtag1.ToLower(), hashtag2.ToLower(), numberOfDegrees, maxAPICalls);
        }

        internal async Task<IDictionary<Status, IEnumerable<string>>> GetUniqueHashtags(string query, int max, bool allowAPICalls = true)
        {
            if (TwitterCache.HashtagConnectionsQueried(Configuration, query.ToLower()) || !allowAPICalls)
                return FilterHashtags(TwitterCache.FindHashtagConnections(Configuration, query.ToLower(), max));

            IDictionary<Status, IEnumerable<string>> results = (await GetResults<TweetSearchResults>(
                query,
                AuthenticationType.Both,
                TwitterAPIUtils.HashtagIgnoreRepeatSearchQuery,
                TwitterAPIEndpoint.SearchTweets))
                .Statuses
                .Where(status => status.Entities.Hashtags.Any(tag => tag.Text.ToLower() == query))
                .Aggregate(new Dictionary<Status, IEnumerable<string>>(), StoreHashtagsInStatus);
            foreach (var statusTags in results)
                TwitterCache.UpdateHashtagConnections(Configuration, query.ToLower(), statusTags.Key, statusTags.Value);
            return FilterHashtags(results);
        }

        private IDictionary<Status, IEnumerable<string>> FilterHashtags(IDictionary<Status, IEnumerable<string>> dictionary)
        {
            var filtered = dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var status in dictionary.Keys)
                filtered[status] = FilterHashtags(filtered[status]);
            return filtered;
        }

        private Dictionary<Status, IEnumerable<string>> StoreHashtagsInStatus(Dictionary<Status, IEnumerable<string>> dict, Status status)
        {
            dict.Add(status, status.Entities.Hashtags.Select(hashtag => hashtag.Text.ToLower()).Distinct());
            return dict;
        }

        /// <summary>
        /// Attempts to find a link between two given users.
        /// Performs a bidirectional, probabilistic, breadth-first search, and will never exceed 5 API calls by default.
        /// </summary>
        /// <param name="user1">The starting user's screen name (minus the '@').</param>
        /// <param name="user2">The ending user's screen name (minus the '@').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <param name="maxNodeConnections">The maximum integer number of per-node connections to handle.</param>
        /// <param name="lookupIDs">Whether or not to return users by name after performing a lookup.</param>
        /// <returns></returns>
        [HttpGet("degrees/users")]
        public async Task<IActionResult> GetUserLink(string user1, string user2, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 50, bool lookupIDs = false)
        {

            if (user1 == null || user2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid parameters.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByID, rateLimitDb, userManager, User)[AuthenticationType.User]);
            TwitterUser user1obj = (await GetUser(user1) as OkObjectResult)?.Value as TwitterUser;
            TwitterUser user2obj = (await GetUser(user2) as OkObjectResult)?.Value as TwitterUser;
            if (user1obj == null || user2obj == null)
                return BadRequest("Unable to find given users.");

            if (lookupIDs)
            {
                var pathResults = (await new UserLinkFinder(Configuration, this, rateLimitDb, userManager, maxNodeConnections)
                    .Execute(user1obj, user2obj, numberOfDegrees, maxAPICalls) as OkObjectResult)?.Value;

                if (pathResults is LinkData<TwitterUser, TwitterUser> originalLinkData)
                {
                    var idsToLookup = originalLinkData.Paths.Aggregate(new List<string>(), (set, path) =>
                        {
                            foreach (var id in path.Path.Where(link => link.Value.ScreenName == null).Select(link => link.Value.ID))
                                set.Add(id);
                            return set;
                        })
                        .Distinct();
                    if (idsToLookup.Count() > 0)
                    {
                        int maxIDsToLookup = RateLimitCache.Get.MinimumRateLimits(
                            QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min() * MaxSingleQueryUserLookupCount;
                        TwitterCache.UpdateUsers(Configuration, (await LookupIDs(maxIDsToLookup, idsToLookup.ToList())).AsEnumerable());
                        var updatedResults = (await new UserLinkFinder(Configuration, this, rateLimitDb, userManager, maxNodeConnections)
                            .Execute(user1obj, user2obj, numberOfDegrees, maxAPICalls) as OkObjectResult)?.Value as LinkData<TwitterUser, TwitterUser>;
                        if (updatedResults == null)
                            return BadRequest("Error during user search.");
                        updatedResults.Metadata.Time = updatedResults.Metadata.Time + originalLinkData.Metadata.Time;
                        updatedResults.Metadata.Calls = originalLinkData.Metadata.Calls;
                        return Ok(updatedResults);
                    }
                }
                return BadRequest("Error during user search.");
            }
            else
                return await new UserIDLinkFinder(Configuration, this, rateLimitDb, userManager, maxNodeConnections)
                    .Execute(user1obj.ID, user2obj.ID, numberOfDegrees, maxAPICalls);
        }
    }
}