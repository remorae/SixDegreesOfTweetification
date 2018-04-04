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
        private readonly TwitterCacheDbContext twitterCacheDb;

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

        public SearchController(IConfiguration configuration, UserManager<ApplicationUser> userManager, RateLimitDbContext rateLimitDb, TwitterCacheDbContext twitterCacheDb)
        {
            Configuration = configuration;
            this.userManager = userManager;
            this.rateLimitDb = rateLimitDb;
            this.twitterCacheDb = twitterCacheDb;
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
                return default(T);
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
        public async Task<IActionResult> HashtagConnections(string query, int numberOfDegrees = 6, int maxCalls = 60, int maxNodeConnections = 500)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1 || maxNodeConnections < 1)
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
                    if (await Hashtags(toQuery) is OkObjectResult result && result.Value is IEnumerable<string> lookup)
                    {
                        foreach (var hashtag in lookup.Distinct())
                        {
                            results[toQuery].Connections.Add(hashtag);
                            if (results[toQuery].Connections.Count >= maxNodeConnections)
                                break;
                        }
                        int nextDistance = results[toQuery].Distance + 1;
                        foreach (var hashtag in lookup.Distinct().Except(queried).Except(remaining).Take(maxNodeConnections))
                        {
                            if (nextDistance < numberOfDegrees)
                                remaining.Push(hashtag);
                            results[hashtag] = new HashtagConnectionInfo(nextDistance);
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
        public async Task<IActionResult> UserConnections(string query, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 50)
        {
            if (query == null || numberOfDegrees < 1 || maxCalls < 1 || maxNodeConnections < 1)
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
                while ((callsMade == 0 || callsMade < maxAPICalls) && remaining.Count > 0)
                {
                    int currentLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[QueryType.UserConnectionsByScreenName][AuthenticationType.User];
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    if (await GetUserConnections(toQuery) is OkObjectResult result && result.Value is IEnumerable<UserResult> lookup)
                    {
                        int newLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[QueryType.UserConnectionsByScreenName][AuthenticationType.User];
                        if (newLimit < currentLimit)
                            ++callsMade;
                        foreach (var user in lookup.Distinct())
                        {
                            results[toQuery].Connections.Add(user);
                            if (results[toQuery].Connections.Count >= maxNodeConnections)
                                break;
                        }
                        int nextDistance = results[toQuery].Distance + 1;
                        foreach (var user in lookup.Distinct().Where(user => !queried.Contains(user.ScreenName)).Where(user => !remaining.Contains(user.ScreenName)).Take(maxNodeConnections))
                        {
                            if (nextDistance < numberOfDegrees)
                                remaining.Push(user.ScreenName);
                            results[user.ScreenName] = new UserConnectionInfo(nextDistance);
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
        /// Performs a depth-first search, and will never exceed 60 API calls by default.
        /// </summary>
        /// <param name="hashtag1">The starting hashtag (minus the '#').</param>
        /// <param name="hashtag2">The ending hashtag (minus the '#').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <returns></returns>
        [HttpGet("degrees/hashtags")]
        public async Task<IActionResult> HashtagLink(string hashtag1, string hashtag2, int numberOfDegrees = 6, int maxCalls = 60, int maxNodeConnections = 500)
        {
            if (hashtag1 == null || hashtag2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid query.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.HashtagConnectionsByHashtag, rateLimitDb, userManager, User)[AuthenticationType.User]);
            
            return await FindLink(hashtag1, hashtag2, numberOfDegrees, maxAPICalls, maxNodeConnections, false,
                async query =>
                Ok((await GetResults<TweetSearchResults>(
                       query,
                       AuthenticationType.Both,
                       TwitterAPIUtils.HashtagIgnoreRepeatSearchQuery,
                       TwitterAPIEndpoint.SearchTweets))
                    .Statuses
                    .Where(status => status.Entities.Hashtags.Any(tag => tag.Text.ToLower() == query))
                    .Aggregate(new Dictionary<Status, IEnumerable<string>>(), (dict, status) => AppendHashtagsInStatus(status, dict))),
                QueryType.HashtagConnectionsByHashtag
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
        public async Task<IActionResult> UserLink(string user1, string user2, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 5000, bool lookupIDs = false)
        {

            if (user1 == null || user2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid parameters.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByID, rateLimitDb, userManager, User)[AuthenticationType.User]);
            string user1ID = ((await GetUser(user1) as OkObjectResult)?.Value as UserResult)?.ID;
            string user2ID = ((await GetUser(user2) as OkObjectResult)?.Value as UserResult)?.ID;
            if (user1ID == null || user2ID == null)
                return BadRequest("Unable to retrieve given users' ids.");

            return await FindLink(user1ID, user2ID, numberOfDegrees, maxAPICalls, maxNodeConnections, lookupIDs,
                async query =>
                Ok(((await GetUserConnectionIDs(query) as OkObjectResult)?.Value as IEnumerable<long>)?.Select(id => id.ToString())?.Distinct()),
                QueryType.UserConnectionsByScreenName);
        }

        private async Task<IActionResult> FindLink<T>(T start, T end, int numberOfDegrees, int maxAPICalls, int maxPerNode, bool lookupIDs, Func<T, Task<IActionResult>> lookupFunc, QueryType queryType)
            where T : class
        {
            if (maxAPICalls < 0)
                return BadRequest("Authentication error.");
            if (start.Equals(end))
                return BadRequest("Start and end must differ.");
            DateTime startTime = DateTime.Now;
            int callsMade = 0;

            try
            {
                var hashtagLinks = new Dictionary<Status, IEnumerable<T>>();
                var startList = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(start, 0) };
                var endList = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(end, 0) };

                ISet<T> queried = new HashSet<T>();
                ISet<T> seen = new HashSet<T>() { start, end };
                ISet<T> seenStart = new HashSet<T>() { start };
                ISet<T> seenEnd = new HashSet<T>() { end };
                IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections = new Dictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>>(new ConnectionInfo<T>.Node.EqualityComparer())
                {
                    { startList.First(), new ConnectionInfo<T>() },
                    { endList.First(), new ConnectionInfo<T>() }
                };

                bool foundLink = false;
                while ((callsMade == 0 || callsMade < maxAPICalls) && (startList.Count > 0 || endList.Count > 0) && !foundLink)
                {
                    if (startList.Count > 0)
                    {
                        int currentLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[queryType][AuthenticationType.User];
                        ConnectionInfo<T>.Node toQuery = startList.First();
                        startList.Remove(toQuery);
                        var obj = (await lookupFunc(toQuery.Value) as OkObjectResult)?.Value;
                        int newLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[queryType][AuthenticationType.User];
                        if (newLimit < currentLimit)
                            ++callsMade;
                        if (obj != null)
                            HandleSearchResults(obj, toQuery, ref foundLink, ref seenEnd, ref hashtagLinks, numberOfDegrees, maxPerNode, ref startList, ref queried, ref seen, ref seenStart, ref connections);
                    }
                    if (endList.Count > 0 && callsMade < maxAPICalls)
                    {
                        int currentLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[queryType][AuthenticationType.User];
                        ConnectionInfo<T>.Node toQuery = endList.First();
                        endList.Remove(toQuery);
                        var obj = (await lookupFunc(toQuery.Value) as OkObjectResult)?.Value;
                        int newLimit = RateLimitCache.Get.CurrentRateLimits(rateLimitDb, userManager, User)[queryType][AuthenticationType.User];
                        if (newLimit < currentLimit)
                            ++callsMade;
                        if (obj != null)
                            HandleSearchResults(obj, toQuery, ref foundLink, ref seenStart, ref hashtagLinks, numberOfDegrees, maxPerNode, ref endList, ref queried, ref seen, ref seenEnd, ref connections);
                    }
                }

                List<ConnectionInfo<T>.Node> results = (!foundLink) ? null : LookForPath(connections, start, end);

                if (hashtagLinks.Count == 0 && lookupIDs)
                {
                    var searchResults = new List<UserSearchResults>();
                    var userResults = new List<UserResult>();
                    var userIDs = (connections.Keys.Select(node => node.Value) as IEnumerable<string>).ToHashSet();
                    var cached = from user in twitterCacheDb.Users
                                 where userIDs.Contains(user.ID)
                                 select user;
                    userResults.AddRange(cached);
                    var idsToLookup = userIDs.Except(cached.Select(user => user.ID)).ToList();
                    while (idsToLookup.Count() > 0)
                    {
                        int count = Math.Min(100, idsToLookup.Count());
                        try
                        {
                            var search = await GetResultCollection<UserSearchResults>(
                               idsToLookup.Take(count),
                               AuthenticationType.Both,
                               TwitterAPIUtils.UserLookupQuery,
                               TwitterAPIEndpoint.UsersLookup);
                            searchResults.AddRange(search);
                        }
                        catch (Exception)
                        {}
                        idsToLookup.RemoveRange(0, count);
                    }
                    userResults.AddRange(searchResults.Select(user => ToUserResult(user)));

                    int numberSeen = 0;
                    foreach (var user in searchResults.Select(user => ToUserResult(user)))
                    {
                        twitterCacheDb.Users.Add(user);
                        ++numberSeen;
                        if (numberSeen % 100 == 0)
                            twitterCacheDb.SaveChanges();
                    }

                    twitterCacheDb.SaveChanges();
                    ReplaceUserIDsPairsWithScreenNames(ref connections, userResults);
                    ReplaceUserIDsWithScreenNames(ref seenStart, userResults);
                    ReplaceUserIDsWithScreenNames(ref seenEnd, userResults);
                }

                if (results == null || results.Count() == 0)
                {
                    // Failed to find link
                    return Ok(new
                    {
                        Connections = connections
                            .Where(entry => entry.Value.Connections.Count > 0)
                            .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value)),
                        Links = Enumerable.Empty<string>(),
                        Metadata = new { Time = DateTime.Now - startTime, Calls = callsMade }
                    });
                }

                var resultLinks = new List<Status>();
                if (hashtagLinks.Count > 0)
                {
                    for (int i = 0; i < results.Count() - 1; ++i)
                        resultLinks.Add(hashtagLinks.First(entry => entry.Value.Contains(results[i].Value) && entry.Value.Contains(results[i + 1].Value)).Key);
                }
                else if (lookupIDs)
                {
                    var userResults = new List<UserResult>();
                    var ids = results.Select(node => node.Value as string).ToHashSet();
                    var cached = from user in twitterCacheDb.Users
                                 where ids.Contains(user.ID)
                                 select user;
                    userResults.AddRange(cached);
                    var idsToLookup = ids.Except(cached.Select(user => user.ID)).ToList();
                    if (idsToLookup.Count > 0)
                    {
                        var searchResults = await GetResultCollection<UserSearchResults>(
                               idsToLookup,
                               AuthenticationType.Both,
                               TwitterAPIUtils.UserLookupQuery,
                               TwitterAPIEndpoint.UsersLookup);
                        userResults.AddRange(searchResults.Select(searchResult => ToUserResult(searchResult)));
                        foreach (var user in searchResults.Select(result => ToUserResult(result)))
                            twitterCacheDb.Users.Add(user);
                        twitterCacheDb.SaveChanges();
                    }
                    ReplaceUserIDNodesWithScreenNames(ref results, userResults);
                }
                var expandedStart = seenStart.Except(results.Select(node => node.Value)).Aggregate(new HashSet<T>(), AggregateSetConnections(connections));
                var expandedEnd = seenEnd.Except(results.Select(node => node.Value)).Aggregate(new HashSet<T>(), AggregateSetConnections(connections));
                return Ok(new
                {
                    Connections = connections
                            .Where(entry => entry.Value.Connections.Count > 0)
                            .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value)),
                    Shared = expandedStart.Intersect(expandedEnd),
                    Path = results.ToDictionary(node => node.Value, node => node.Distance),
                    Links = resultLinks.Select(status => status.URL),
                    Metadata = new { Time = DateTime.Now - startTime, Calls = callsMade }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static Func<HashSet<T>, T, HashSet<T>> AggregateSetConnections<T>(IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections) where T : class
        {
            return (set, next) =>
            {
                if (!set.Contains(next))
                    set.Add(next);
                set.UnionWith(connections.First(conn => conn.Key.Value.Equals(next)).Value.Connections.Select(c => c.Key.Value));
                return set;
            };
        }

        private List<ConnectionInfo<T>.Node> LookForPath<T>(IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections, T start, T end)
        {
            // Make all connections bidirectional
            foreach (var entry in connections)
                foreach (var node in entry.Value.Connections.Keys)
                    if (!connections[node].Connections.ContainsKey(entry.Key))
                        connections[node].Connections.Add(entry.Key, 1);
            var results = ConnectionInfo<T>.ShortestPath(
                    connections,
                    connections.First(node => node.Key.Value.Equals(start)).Key,
                    connections.First(node => node.Key.Value.Equals(end)).Key);
            return results;
        }

        private void ReplaceUserIDsPairsWithScreenNames<T>(
            ref IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections,
            IEnumerable<UserResult> userResults)
            where T : class
        {
            var newConnections = new Dictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>>();
            foreach (var entry in connections.Where(con => userResults.Any(user => user.ID.Equals(con.Key.Value))))
            {
                var newNode = new ConnectionInfo<T>.Node(userResults.First(user => user.ID.Equals(entry.Key.Value)).ScreenName as T, entry.Key.Distance);
                var newInfo = new ConnectionInfo<T>();
                foreach (var connection in entry.Value.Connections.Where(con => userResults.Any(user => user.ID.Equals(con.Key.Value))))
                    newInfo.Connections.Add(new ConnectionInfo<T>.Node(userResults.First(user => user.ID.Equals(connection.Key.Value)).ScreenName as T, connection.Key.Distance), connection.Value);
                newConnections.Add(newNode, newInfo);
            }

            connections = newConnections;
        }

        private void ReplaceUserIDNodesWithScreenNames<T>(
            ref List<ConnectionInfo<T>.Node> results,
            IEnumerable<UserResult> userResults)
            where T : class
        {
            var newSet = new List<ConnectionInfo<T>.Node>();
            foreach (var node in results)
            {
                if (userResults.First(user => user.ID.Equals(node.Value)).ScreenName is T found)
                    newSet.Add(new ConnectionInfo<T>.Node(found, node.Distance));
                else if (twitterCacheDb.Users.FirstOrDefault(user => user.ID.Equals(node.Value))?.ScreenName is T lookup)
                    newSet.Add(new ConnectionInfo<T>.Node(lookup, node.Distance));
            }

            results = newSet;
        }

        private void ReplaceUserIDsWithScreenNames<T>(
           ref ISet<T> results,
           IEnumerable<UserResult> userResults)
           where T : class
        {
            var newSet = new HashSet<T>();
            foreach (T val in results)
            {
                if (userResults.FirstOrDefault(user => user.ID.Equals(val))?.ScreenName is T found)
                    newSet.Add(found);
                else if (twitterCacheDb.Users.FirstOrDefault(user => user.ID.Equals(val))?.ScreenName is T lookup)
                    newSet.Add(lookup);
            }

            results = newSet;
        }

        private void HandleSearchResults<T>(
            object results,
            ConnectionInfo<T>.Node toQuery,
            ref bool foundLink,
            ref ISet<T> goals,
            ref Dictionary<Status, IEnumerable<T>> links,
            int numberOfDegrees,
            int maxPerNode,
            ref List<ConnectionInfo<T>.Node> list,
            ref ISet<T> queried,
            ref ISet<T> seen,
            ref ISet<T> seenSubset,
            ref IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections)
            where T : class
        {
            if (results is IEnumerable<T> lookup)
            {
                StoreResults(numberOfDegrees, maxPerNode, ref list, ref queried, ref seen, ref seenSubset, ref connections, ref toQuery, lookup.Where(result => !result.Equals(toQuery.Value)));
                if (lookup.Intersect(goals).Count() > 0)
                    foundLink = true;
            }
            else if (results is IDictionary<Status, IEnumerable<T>> newLinks)
            {
                foreach (var entry in newLinks)
                    links.Add(entry.Key, entry.Value);
                var tags = newLinks.Aggregate(new List<T>(), (collection, entry) => { collection.AddRange(entry.Value); return collection; });
                StoreResults(numberOfDegrees, maxPerNode, ref list, ref queried, ref seen, ref seenSubset, ref connections, ref toQuery, tags.Where(result => !result.Equals(toQuery.Value)));
                if (tags.Intersect(goals).Count() > 0)
                    foundLink = true;
            }
        }

        private void StoreResults<T>(
            int numberOfDegrees,
            int maxPerNode,
            ref List<ConnectionInfo<T>.Node> list,
            ref ISet<T> queried,
            ref ISet<T> allSeen,
            ref ISet<T> subSetSeen,
            ref IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections,
            ref ConnectionInfo<T>.Node toQuery,
            IEnumerable<T> lookup)
        {
            queried.Add(toQuery.Value);
            int nextDistance = toQuery.Distance + 1;
            foreach (T entity in lookup.Distinct())
            {
                connections[toQuery].Connections.Add(new ConnectionInfo<T>.Node(entity, nextDistance), 1);
                if (connections[toQuery].Connections.Count >= maxPerNode)
                    break;
            }
            var tempSeen = allSeen;
            foreach (T entity in lookup.Distinct().Where(entity => !tempSeen.Contains(entity)).Where(entity => !tempSeen.Contains(entity)))
            {
                ConnectionInfo<T>.Node node = new ConnectionInfo<T>.Node(entity, nextDistance);
                if (nextDistance < numberOfDegrees)
                    list.Add(node);
                connections[node] = new ConnectionInfo<T>();
                tempSeen.Add(entity);
                subSetSeen.Add(entity);
            }
            allSeen = tempSeen;
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
                    Countries = GetFormattedCountries(countries.Values),
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
        public async Task<IActionResult> GetUser(string screen_name, string id = null)
        {
            if (screen_name == null && id == null)
                return BadRequest("Invalid parameters.");
            try
            {
                UserResult found = (id == null)
                    ? twitterCacheDb.Users.FirstOrDefault(user => user.ScreenName == screen_name)
                    : twitterCacheDb.Users.Find(id);
                if (found != null)
                    return Ok(found);
                var results = (screen_name != null)
                    ? await GetResults<UserSearchResults>(
                        screen_name,
                        AuthenticationType.Both,
                        TwitterAPIUtils.UserSearchQuery,
                        TwitterAPIEndpoint.UsersShow)
                    : await GetResults<UserSearchResults>(
                        id,
                        AuthenticationType.Both,
                        TwitterAPIUtils.UserIDSearchQuery,
                        TwitterAPIEndpoint.UsersShow);
                var userResult = ToUserResult(results);
                if (twitterCacheDb.Users.Find(userResult.ID) == null)
                    twitterCacheDb.Users.Add(userResult);
                twitterCacheDb.SaveChanges();
                return Ok(userResult);
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
                if (!twitterCacheDb.Users.Any(user => user.ScreenName.ToLower() == screen_name.ToLower()))
                {
                    twitterCacheDb.Add(ToUserResult(await GetResults<UserSearchResults>(screen_name, AuthenticationType.Both, TwitterAPIUtils.UserSearchQuery, TwitterAPIEndpoint.UsersShow)));
                    twitterCacheDb.SaveChanges();
                }
                var queriedUser = twitterCacheDb.Users.First(user => user.ScreenName == screen_name);

                ISet<string> idSet = new HashSet<string>();
                if (twitterCacheDb.UserConnectionLookups.Find(queriedUser.ID) is UserConnectionLookupStatus status && status.Queried)
                {
                    idSet = twitterCacheDb.UserConnections
                        .Where(connection => connection.Start == queriedUser.ID)
                        .Select(connection => connection.End)
                        .ToHashSet();
                }
                else
                {
                    twitterCacheDb.UserConnectionLookups.Add(new UserConnectionLookupStatus() { ID = queriedUser.ID, Queried = true });
                    twitterCacheDb.SaveChanges();

                    var followerResults = await GetResults<UserIdsResults>(
                        screen_name,
                        AuthenticationType.Both,
                        TwitterAPIUtils.FollowersFriendsIDsQuery,
                        TwitterAPIEndpoint.FollowersIDs);

                    if (idSet.Count < limit)
                    {
                        var friendResults = await GetResults<UserIdsResults>(
                            screen_name,
                            AuthenticationType.Both,
                            TwitterAPIUtils.FollowersFriendsIDsQuery,
                            TwitterAPIEndpoint.FriendsIDs);
                        if (friendResults != null)
                            foreach (string id in friendResults.Ids.Select(id => id.ToString()))
                            {
                                if (idSet.Count >= limit)
                                    break;
                                if (!idSet.Contains(id))
                                    idSet.Add(id);
                            }
                    }
                }

                ICollection<UserResult> results = new List<UserResult>();
                ISet<long> uniqueIDsToLookup = new HashSet<long>();
                int maxLookupCount = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min();
                limit = Math.Min(limit, maxLookupCount * MaxUserLookupCount);

                var cachedUsers = twitterCacheDb.Users
                    .Where(user => idSet.Contains(user.ID));
                foreach (var user in cachedUsers)
                    results.Add(user);
                foreach (var id in idSet.Except(cachedUsers.Select(user => user.ID)))
                    uniqueIDsToLookup.Add(long.Parse(id));

                var ids = new List<long>(uniqueIDsToLookup);
                var usersToCache = new List<UserResult>();
                while (ids.Count > 0)
                {
                    int count = Math.Min(100, ids.Count);
                    var searchResults = await GetResultCollection<UserSearchResults>(
                        ids.Take(count).Select(id => id.ToString()),
                        AuthenticationType.Both,
                        TwitterAPIUtils.UserLookupQuery,
                        TwitterAPIEndpoint.UsersLookup);
                    ids.RemoveRange(0, count);
                    if (searchResults != null)
                    {
                        foreach (var searchResult in searchResults)
                        {
                            var user = ToUserResult(searchResult);
                            results.Add(user);
                            usersToCache.Add(user);
                        }
                    }
                }
                foreach (var user in usersToCache.Distinct())
                {
                    twitterCacheDb.Users.Add(user);
                    if (twitterCacheDb.UserConnections.Find(queriedUser.ID, user.ID) == null)
                    {
                        twitterCacheDb.UserConnections.Add(new UserConnection() { Start = queriedUser.ID, End = user.ID });
                        twitterCacheDb.UserConnections.Add(new UserConnection() { End = queriedUser.ID, Start = user.ID });
                    }
                }
                twitterCacheDb.SaveChanges();
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
                if (twitterCacheDb.UserConnectionLookups.Find(startID) is UserConnectionLookupStatus status && status.Queried)
                {
                    return Ok(twitterCacheDb.UserConnections
                        .Where(connection => connection.Start == startID)
                        .Select(connection => long.Parse(connection.End))
                        .AsEnumerable());
                }
                else
                {
                    twitterCacheDb.UserConnectionLookups.Add(new UserConnectionLookupStatus() { ID = startID, Queried = true });
                    twitterCacheDb.SaveChanges();
                }

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

                foreach (var id in uniqueIds.Select(val => val.ToString()))
                {
                    if (twitterCacheDb.UserConnections.Find(startID, id) == null)
                    {
                        twitterCacheDb.UserConnections.Add(new UserConnection() { Start = startID, End = id });
                        twitterCacheDb.UserConnections.Add(new UserConnection() { End = startID, Start = id });
                    }
                }
                twitterCacheDb.SaveChanges();
                return Ok(uniqueIds);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
