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
    [Route("api/search")]
    public class SearchController : Controller
    {
        private const int MaxUserLookupCount = 100;
        private const int MaxUserFriendsCount = 5000;
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
        /// Returns tweet search results for the given hashtag.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IActionResult> Tweets(string query)
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
        /// Returns a list of hashtags (will contain duplicates) associated directly (contained within the same tweet) with the given hashtag.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("hashtags")]
        public async Task<IActionResult> Hashtags(string query)
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
        /// Returns a list of hashtags within the given number of "degrees" of the given hashtag (each tweet represents one degree).
        /// Performs a depth-first search, and will never exceed 60 API calls by default.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <returns></returns>
        [HttpGet("degrees/hashtags/single")]
        public async Task<IActionResult> HashtagConnections(string query, int numberOfDegrees = 6, int maxCalls = 60)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User]);
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

                return Ok(results.ToDictionary(entry => entry.Key, entry => entry.Value.Connections));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of users within the given number of "degrees" of the user with the given screen name
        /// (a link to a user's followers/friends represents one degree).
        /// Performs a depth-first search, and will never exceed 5 API calls by default.
        /// </summary>
        /// <param name="query">The screen name to search for (minus the '@').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <returns></returns>
        [HttpGet("degrees/users/single")]
        public async Task<IActionResult> UserConnections(string query, int numberOfDegrees = 6, int maxCalls = 5)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User)[AuthenticationType.User]);
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
                    foreach (var user in lookup.Distinct())
                    {
                        results[toQuery].Connections.Add(user);
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
        /// Attempts to find a link between the two hashtags (each tweet represents one degree).
        /// Performs a depth-first search, and will never exceed 60 API calls by default.
        /// </summary>
        /// <param name="hashtag1">The starting hashtag (minus the '#').</param>
        /// <param name="hashtag2">The ending hashtag (minus the '#').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <returns></returns>
        [HttpGet("degrees/hashtags")]
        public async Task<IActionResult> HashtagLink(string hashtag1, string hashtag2, int numberOfDegrees = 6, int maxCalls = 60)
        {
            if (hashtag1 == null || hashtag2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User]);
            return await FindLink(hashtag1, hashtag2, numberOfDegrees, maxAPICalls,
                async query =>
                Ok((await GetResults<TweetSearchResults>(
                       query,
                       AuthenticationType.Both,
                       TwitterAPIUtils.HashtagIgnoreRepeatSearchQuery,
                       TwitterAPIEndpoint.SearchTweets))
                    .Statuses
                    .Aggregate(new Dictionary<Status, IEnumerable<string>>(), (dict, status) => AppendHashtagsInStatus(status, dict)))
            );
        }

        private Dictionary<Status, IEnumerable<string>> AppendHashtagsInStatus(Status status, Dictionary<Status, IEnumerable<string>> dict)
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
        /// <returns></returns>
        [HttpGet("degrees/users")]
        public async Task<IActionResult> UserLink(string user1, string user2, int numberOfDegrees = 6, int maxCalls = 5)
        {

            if (user1 == null || user2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("InvalValue query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByID, rateLimitDb, userManager, User)[AuthenticationType.User]);
            string user1ID = ((await GetUser(user1) as OkObjectResult).Value as UserResult).ID;
            string user2ID = ((await GetUser(user2) as OkObjectResult).Value as UserResult).ID;

            return await FindLink(user1ID, user2ID, numberOfDegrees, maxAPICalls,
                async query =>
                Ok(((await GetUserConnectionIDs(query) as OkObjectResult).Value as IEnumerable<long>).Select(id => id.ToString()).Distinct()));
        }

        private async Task<IActionResult> FindLink<T>(T start, T end, int numberOfDegrees, int maxAPICalls, Func<T, Task<IActionResult>> lookupFunc) where T : class
        {
            if (maxAPICalls < 1)
                return BadRequest("Rate limit exceeded.");
            DateTime startTime = DateTime.Now;
            int callsMade = 0;
            try
            {
                var links = new Dictionary<Status, IEnumerable<T>>();
                var startList = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(start, 0) };
                var endList = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(end, 0) };

                ISet<T> queried = new HashSet<T>();
                ISet<T> seen = new HashSet<T>() { start, end };
                IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections = new Dictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>>()
                {
                    { startList.First(), new ConnectionInfo<T>() },
                    { endList.First(), new ConnectionInfo<T>() }
                };

                bool foundOther = false;
                while (callsMade < maxAPICalls && (startList.Count > 0 || endList.Count > 0))
                {
                    if (startList.Count > 0)
                    {
                        ++callsMade;
                        ConnectionInfo<T>.Node toQuery = startList.First();
                        startList.Remove(toQuery);
                        var obj = (await lookupFunc(toQuery.Value) as OkObjectResult).Value;
                        if (obj is IEnumerable<T> lookup)
                        {
                            StoreResults(numberOfDegrees, ref startList, ref queried, ref seen, ref connections, ref toQuery, lookup.Where(result => !result.Equals(toQuery.Value)));
                            if (lookup.Contains(end))
                            {
                                foundOther = true;
                                break;
                            }
                        }
                        else if (obj is IDictionary<Status, IEnumerable<T>> newLinks)
                        {
                            foreach (var entry in newLinks)
                                links.Add(entry.Key, entry.Value);
                            var tags = newLinks.Aggregate(new List<T>(), (list, entry) => { list.AddRange(entry.Value); return list; });
                            StoreResults(numberOfDegrees, ref startList, ref queried, ref seen, ref connections, ref toQuery, tags.Where(result => !result.Equals(toQuery.Value)));
                            if (tags.Contains(end))
                            {
                                foundOther = true;
                                break;
                            }
                        }
                    }
                    if (endList.Count > 0 && callsMade < maxAPICalls)
                    {
                        ++callsMade;
                        ConnectionInfo<T>.Node toQuery = endList.First();
                        endList.Remove(toQuery);
                        var obj = (await lookupFunc(toQuery.Value) as OkObjectResult).Value;
                        if (obj is IEnumerable<T> lookup)
                        {
                            StoreResults(numberOfDegrees, ref endList, ref queried, ref seen, ref connections, ref toQuery, lookup.Where(result => !result.Equals(toQuery.Value)));
                            if (lookup.Contains(start))
                            {
                                foundOther = true;
                                break;
                            }
                        }
                        else if (obj is IDictionary<Status, IEnumerable<T>> newLinks)
                        {
                            foreach (var entry in newLinks)
                                links.Add(entry.Key, entry.Value);
                            var tags = newLinks.Aggregate(new List<T>(), (list, entry) => { list.AddRange(entry.Value); return list; });
                            StoreResults(numberOfDegrees, ref endList, ref queried, ref seen, ref connections, ref toQuery, tags.Where(result => !result.Equals(toQuery.Value)));
                            if (tags.Contains(start))
                            {
                                foundOther = true;
                                break;
                            }
                        }
                    }
                }

                var results = (!foundOther)
                    ? null
                    : ConnectionInfo<T>.ShortestPath(
                        connections,
                        connections.First(node => node.Key.Value.Equals(start)).Key,
                        connections.First(node => node.Key.Value.Equals(end)).Key);
                if (results == null || results.Count() == 0)
                {
                    results = ConnectionInfo<T>.ShortestPath(
                        connections,
                        connections.First(node => node.Key.Value.Equals(end)).Key,
                        connections.First(node => node.Key.Value.Equals(start)).Key);
                }
                if (results == null || results.Count() == 0)
                    return Ok(new {
                        Connections = connections //TODO Get screen names
                            .Where(entry => entry.Value.Connections.Count > 0)
                            .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value)),
                        Links = Enumerable.Empty<string>(),
                        Metadata = new { Time = DateTime.Now - startTime, Calls = callsMade } });
                var resultLinks = new List<Status>();
                if (links.Count > 0)
                {
                    for (int i = 0; i < results.Count() - 1; ++i)
                        resultLinks.Add(links.First(entry => entry.Value.Contains(results[i].Value) && entry.Value.Contains(results[i + 1].Value)).Key);
                }
                else
                {
                    var userResults = await GetResultCollection<UserSearchResults>(
                           results.Select(node => node.Value) as IEnumerable<string>,
                           AuthenticationType.Both,
                           TwitterAPIUtils.UserLookupQuery,
                           TwitterAPIEndpoint.UsersLookup);
                    var newPath = new List<ConnectionInfo<T>.Node>();
                    foreach (var node in results)
                        newPath.Add(new ConnectionInfo<T>.Node(userResults.First(user => user.IdStr.Equals(node.Value)).ScreenName as T, node.Distance));
                    results = newPath;
                }
                return Ok(new {
                    Path = results.ToDictionary(node => node.Value, node => node.Distance),
                    Links = resultLinks.Select(status => status.URL),
                    Metadata = new { Time = DateTime.Now - startTime, Calls = callsMade } });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private void StoreResults<T>(int numberOfDegrees, ref List<ConnectionInfo<T>.Node> list, ref ISet<T> queried, ref ISet<T> seen, ref IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections, ref ConnectionInfo<T>.Node toQuery, IEnumerable<T> lookup)
        {
            queried.Add(toQuery.Value);
            int nextDistance = toQuery.Distance + 1;
            foreach (var entity in lookup.Distinct())
            {
                connections[toQuery].Connections.Add(new ConnectionInfo<T>.Node(entity, nextDistance), 1);
            }
            var tempSeen = seen;
            foreach (var entity in lookup.Distinct().Where(entity => !tempSeen.Contains(entity)).Where(entity => !tempSeen.Contains(entity)))
            {
                ConnectionInfo<T>.Node node = new ConnectionInfo<T>.Node(entity, nextDistance);
                if (nextDistance < numberOfDegrees)
                    list.Add(node);
                connections[node] = new ConnectionInfo<T>();
                tempSeen.Add(entity);
            }
            seen = tempSeen;
            list.Sort((lhs, rhs) => lhs.Heuristic(nextDistance).CompareTo(rhs.Heuristic(nextDistance)));
        }

        /// <summary>
        /// Returns search results for the given hashtag with any associated hashtags and the source tweet urls organized by location.
        /// </summary>
        /// <param name="query">The hashtag to search for (minus the '#').</param>
        /// <returns></returns>
        [HttpGet("locations")]
        public async Task<IActionResult> Locations(string query)
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
        /// <param name="screen_name">Returns information about a specified user.</param>
        /// <returns></returns>
        [HttpGet("user")]
        public async Task<IActionResult> GetUser(string screen_name)
        {
            if (screen_name == null)
                return BadRequest("Invalid parameters.");
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
        /// Returns information about the friends and followers of the specified user.
        /// </summary>
        /// <param name="screen_name">The screen name of the Twitter user to search for (minus the '@').</param>
        /// <param name="limit">The maximum number of users to return (will be rounded up to the nearest 100).
        /// The maximum number of friends/followers to lookup in one query is 1500 (for each) due to rate limiting.</param>
        /// <returns></returns>
        [HttpGet("user/connections")]
        public async Task<IActionResult> GetUserConnections(string screen_name, int limit = MaxUserLookupCount)
        {
            if (screen_name == null || limit < 1)
                return BadRequest("Invalid parameters.");
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

                if (uniqueIds.Count < limit)
                {
                    var friendResults = await GetResults<UserIdsResults>(
                        screen_name,
                        AuthenticationType.Both,
                        TwitterAPIUtils.FollowersFriendsIDsQuery,
                        TwitterAPIEndpoint.FriendsIDs);
                    if (friendResults != null)
                        foreach (long id in friendResults.Ids)
                        {
                            if (uniqueIds.Count >= limit)
                                break;
                            if (!uniqueIds.Contains(id))
                                uniqueIds.Add(id);
                        }
                }

                Queue<long> ids = new Queue<long>(uniqueIds);
                ICollection<UserResult> results = new List<UserResult>();
                while (ids.Count > 0)
                {
                    for (int i = 0; i < ids.Count(); ++i)
                        ids.Dequeue();
                    var userResults = await GetResultCollection<UserSearchResults>(
                        ids.Select(id => id.ToString()),
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

        /// <summary>
        /// Returns IDs of the friends and followers of the specified user.
        /// </summary>
        /// <param name="screen_name">The id of the Twitter user to search for.</param>
        /// <param name="limit">The maximum number of users to return (capped at 5000 followers and 5000 friends).</param>
        /// <returns></returns>
        [HttpGet("user/connectionids")]
        public async Task<IActionResult> GetUserConnectionIDs(string startID)
        {
            if (startID == null)
                return BadRequest("Invalid parameters.");
            try
            {
                var followerResults = await GetResults<UserIdsResults>(
                    startID,
                    AuthenticationType.Both,
                    TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                    TwitterAPIEndpoint.FollowersIDs);
                ISet<long> uniqueIds = new HashSet<long>(followerResults?.Ids ?? Enumerable.Empty<long>());
                
                var friendResults = await GetResults<UserIdsResults>(
                    startID,
                    AuthenticationType.Both,
                    TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                    TwitterAPIEndpoint.FriendsIDs);
                if (friendResults != null)
                    foreach (long id in friendResults.Ids)
                    {
                        if (!uniqueIds.Contains(id))
                            uniqueIds.Add(id);
                    }

                return Ok(uniqueIds);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
