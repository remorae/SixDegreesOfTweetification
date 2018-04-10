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
        private const int MaxSingleQueryUserLookupCount = 100;
        private const int MaxSingleQueryUserConnectionsCount = 5000;
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
                    .Aggregate(new HashSet<Hashtag>(), AppendHashtagsInStatus)
                    .Select(hashtag => hashtag.Text.ToLower());
                return Ok(hashtags);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static HashSet<Hashtag> AppendHashtagsInStatus(HashSet<Hashtag> set, Status status)
        {
            set.UnionWith(status.Entities.Hashtags.AsEnumerable());
            return set;
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
        public async Task<IActionResult> SingleHashtagConnections(string query, int numberOfDegrees = 6, int maxCalls = 60, int maxNodeConnections = 500)
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
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    int previousRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.SearchTweets, userManager, User).Limit;
                    if (await GetUniqueHashtags(toQuery) is IDictionary<Status, IEnumerable<string>> lookup)
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
                        int nextDistance = results[toQuery].Distance + 1;
                        foreach (var hashtag in lookupHashtags.Except(queried).Except(remaining).Take(maxNodeConnections))
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
        /// </summary>
        /// <param name="query">The screen name to search for (minus the '@').</param>
        /// <param name="numberOfDegrees">The maximum integer number of degrees to search with.</param>
        /// <param name="maxCalls">The maximum integer number of Twitter API calls to make.</param>
        /// <param name="maxNodeConnections">The maximum integer number of per-node connections to handle.</param>
        /// <returns></returns>
        [HttpGet("degrees/users/single")]
        public async Task<IActionResult> SingleUserConnections(string query, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 50)
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
                    int previousRateLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, TwitterAPIEndpoint.FollowersIDs, userManager, User).Limit;
                    string toQuery = remaining.Pop();
                    queried.Add(toQuery);
                    if (await GetUserConnections(toQuery) is OkObjectResult result && result.Value is IEnumerable<UserResult> lookup)
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
                        int nextDistance = results[toQuery].Distance + 1;
                        foreach (var user in lookup.Where(user => !queried.Contains(user.ScreenName)).Where(user => !remaining.Contains(user.ScreenName)).Take(maxNodeConnections))
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

            return await FindLink(hashtag1, hashtag2, numberOfDegrees, maxAPICalls, maxNodeConnections, "Hashtag",
                async query => Ok(await GetUniqueHashtags(query)),
                TwitterAPIEndpoint.SearchTweets
            );
        }

        private async Task<IDictionary<Status, IEnumerable<string>>> GetUniqueHashtags(string query)
        {
            if (TwitterCache.HashtagConnectionsQueried(Configuration, query.ToLower()))
                return TwitterCache.FindHashtagConnections(Configuration, query.ToLower());

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
            return results;
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
        public async Task<IActionResult> UserLink(string user1, string user2, int numberOfDegrees = 6, int maxCalls = 5, int maxNodeConnections = 50, bool lookupIDs = false)
        {

            if (user1 == null || user2 == null || numberOfDegrees < 1 || maxCalls < 1)
                return BadRequest("Invalid parameters.");
            int maxAPICalls = Math.Min(maxCalls, RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByID, rateLimitDb, userManager, User)[AuthenticationType.User]);
            UserResult user1obj = (await GetUser(user1) as OkObjectResult)?.Value as UserResult;
            UserResult user2obj = (await GetUser(user2) as OkObjectResult)?.Value as UserResult;
            if (user1 == null || user2 == null)
                return BadRequest("Unable to find given users.");

            if (lookupIDs)
            {
                async Task<IActionResult> lookupFunc(UserResult query) =>
                    Ok(((await GetUserConnections(query.ScreenName) as OkObjectResult)?.Value as IEnumerable<UserResult>));

                async Task<object> findPath() =>
                    (await FindLink(user1obj, user2obj, numberOfDegrees, maxAPICalls, maxNodeConnections, "User", lookupFunc, TwitterAPIEndpoint.FollowersIDs) as OkObjectResult)?.Value;

                var pathResults = await findPath();
                if (pathResults is LinkData<UserResult> originalLinkData)
                {
                    var idsToLookup = originalLinkData.Paths.Aggregate(new List<string>(), (set, path) =>
                        {
                            foreach (var id in path.Where(link => link.Value.ScreenName == null).Select(link => link.Value.ID))
                                set.Add(id);
                            return set;
                        })
                        .Distinct();
                    if (idsToLookup.Count() > 0)
                    {
                        int maxIDsToLookup = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min() * MaxSingleQueryUserLookupCount;
                        TwitterCache.UpdateUsers(Configuration, (await LookupIDs(maxIDsToLookup, idsToLookup.ToList())).AsEnumerable());
                        var updatedResults = await findPath() as LinkData<UserResult>;
                        updatedResults.Metadata.Time = updatedResults.Metadata.Time + originalLinkData.Metadata.Time;
                        updatedResults.Metadata.Calls = originalLinkData.Metadata.Calls;
                        return Ok(updatedResults);
                    }
                }
                return Ok(pathResults);
            }
            else
                return await FindLink(user1obj.ID, user2obj.ID, numberOfDegrees, maxAPICalls, maxNodeConnections, "User",
                async query =>
                Ok(((await GetUserConnectionIDs(query) as OkObjectResult)?.Value as IEnumerable<string>)),
                TwitterAPIEndpoint.FollowersIDs);
        }

        private async Task<IActionResult> FindLink<T>(T start, T end, int maxNumberOfDegrees, int maxAPICalls, int maxConnectionsPerNode, string label, Func<T, Task<IActionResult>> connectionLookupFunc, TwitterAPIEndpoint rateLimitEndpoint)
            where T : class
        {
            if (maxAPICalls < 1)
                return BadRequest("Rate limit exceeded.");
            if (start.Equals(end))
                return BadRequest("Start and end must differ.");

            try
            {
                DateTime startTime = DateTime.Now;

                if (TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, label) is List<List<ConnectionInfo<T>.Node>> cachedUserPaths && cachedUserPaths.Count > 0)
                    return await FormatCachedLinkData(maxConnectionsPerNode, connectionLookupFunc, startTime, cachedUserPaths);

                IDictionary<Status, IEnumerable<T>> tweetLinksFound = new Dictionary<Status, IEnumerable<T>>();
                var remainingFromStart = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(start, 0) };
                var remainingFromEnd = new List<ConnectionInfo<T>.Node>() { new ConnectionInfo<T>.Node(end, 0) };
                ISet<T> queriedNodes = new HashSet<T>();
                ISet<T> seenValues = new HashSet<T>() { start, end };
                ISet<T> seenValuesAtStart = new HashSet<T>() { start };
                ISet<T> seenValuesAtEnd = new HashSet<T>() { end };
                IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections = new Dictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>>(new ConnectionInfo<T>.Node.EqualityComparer())
                {
                    { remainingFromStart.First(), new ConnectionInfo<T>() },
                    { remainingFromEnd.First(), new ConnectionInfo<T>() }
                };

                int callsMade = 0;
                bool foundLink = false;
                bool currentSearchIsFromStart = true;
                while (ContinueSearch(maxAPICalls, callsMade, remainingFromStart, remainingFromEnd, foundLink))
                {
                    var remaining = currentSearchIsFromStart ? remainingFromStart : remainingFromEnd;
                    ISet<T> seenValuesInCurrentDirection = currentSearchIsFromStart ? seenValuesAtStart : seenValuesAtEnd;
                    ISet<T> goalValues = currentSearchIsFromStart ? seenValuesAtEnd : seenValuesAtStart;
                    if (remaining.Count > 0)
                    {
                        ConnectionInfo<T>.Node nodeToQuery = remaining.First();
                        remaining.Remove(nodeToQuery);
                        int previousLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, rateLimitEndpoint, userManager, User).Limit;
                        if ((await connectionLookupFunc(nodeToQuery.Value) as OkObjectResult)?.Value is object lookupResult)
                        {
                            int newLimit = RateLimitController.GetCurrentUserInfo(rateLimitDb, rateLimitEndpoint, userManager, User).Limit;
                            if (newLimit < previousLimit)
                                ++callsMade;
                            (IEnumerable<T> lookupValues, var resultsContainLink) = HandleSearchResults(lookupResult, tweetLinksFound);
                            lookupValues = lookupValues.Where(result => !result.Equals(nodeToQuery.Value)).Distinct();

                            queriedNodes.Add(nodeToQuery.Value);
                            int nextDistance = nodeToQuery.Distance + 1;

                            foreach (var value in lookupValues)
                            {
                                if (connections[nodeToQuery].Connections.Count < maxConnectionsPerNode)
                                    connections[nodeToQuery].Connections.Add(new ConnectionInfo<T>.Node(value, nextDistance), 1);

                                if (!seenValues.Contains(value))
                                {
                                    var node = new ConnectionInfo<T>.Node(value, nextDistance);
                                    if (nextDistance < maxNumberOfDegrees - 1)
                                        remaining.Add(node);
                                    connections[node] = new ConnectionInfo<T>();
                                    seenValues.Add(value);
                                    seenValuesInCurrentDirection.Add(value);
                                }
                            }
                            remaining.Sort((lhs, rhs) => lhs.Heuristic(nextDistance).CompareTo(rhs.Heuristic(nextDistance)));

                            foundLink = resultsContainLink(lookupValues, goalValues);
                        }
                    }
                    currentSearchIsFromStart = !currentSearchIsFromStart;
                }

                List<List<ConnectionInfo<T>.Node>> paths = (!foundLink) ? null : TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, label);

                var expandedStart = seenValuesAtStart.Aggregate(new HashSet<T>(), AggregateSetConnections(connections));
                var expandedEnd = seenValuesAtEnd.Aggregate(new HashSet<T>(), AggregateSetConnections(connections));
                var formattedConnections = connections
                            .Where(entry => entry.Value.Connections.Count > 0)
                            .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value));
                return Ok(new LinkData<T>()
                {
                    Connections = (start is UserResult)
                    ? formattedConnections.ToDictionary(entry => (entry.Key as UserResult).ScreenName, entry => entry.Value)
                    : formattedConnections as Dictionary<string, IEnumerable<T>>,
                    Paths = paths?.Select(path => path.ToDictionary(node => node.Distance, node => node.Value)) ?? Enumerable.Empty<Dictionary<int, T>>(),
                    Links = foundLink ? ExtractLinksInPaths(tweetLinksFound, paths).Distinct().Select(status => status.URL) : Enumerable.Empty<string>(),
                    Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, Calls = callsMade }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<IActionResult> FormatCachedLinkData<T>(int maxConnectionsPerNode, Func<T, Task<IActionResult>> connectionLookupFunc, DateTime startTime, List<List<ConnectionInfo<T>.Node>> cachedPaths) where T : class
        {
            var cachedConnections = new Dictionary<string, IEnumerable<T>>();
            var cachedLinks = new Dictionary<Status, IEnumerable<T>>();
            foreach (var path in cachedPaths.Distinct())
            {
                for (int i = 0; i < path.Count; ++i)
                {
                    var node = path[i];
                    string key = (node.Value is UserResult user) ? user.ScreenName : node.Value as string;
                    var lookupResults = (await connectionLookupFunc(node.Value) as OkObjectResult)?.Value;
                    if (lookupResults != null)
                    {
                        cachedConnections[key] = ExtractConnections<T>(maxConnectionsPerNode, lookupResults);

                        if (node.Value is string)
                        {
                            foreach (var entry in ((IDictionary<Status, IEnumerable<T>>)lookupResults).Where(entry => !cachedLinks.ContainsKey(entry.Key)))
                                cachedLinks.Add(entry.Key, entry.Value);
                        }
                    }
                }
            }

            return Ok(new LinkData<T>()
            {
                Connections = cachedConnections,
                Paths = cachedPaths?.Select(path => path.ToDictionary(node => node.Distance, node => node.Value)) ?? Enumerable.Empty<Dictionary<int, T>>(),
                Links = (cachedLinks.Count > 0) ? ExtractLinksInPaths(cachedLinks, cachedPaths).Select(status => status.URL).Distinct() : Enumerable.Empty<string>(),
                Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, Calls = 0 }
            });
        }

        private static IEnumerable<T> ExtractConnections<T>(int maxConnectionsPerNode, object lookup) where T : class
        {
            return (lookup is IEnumerable<T> connections)
                ? connections.Take(maxConnectionsPerNode)
                : (lookup as IDictionary<Status, IEnumerable<T>>)?
                .Aggregate(new HashSet<T>(), AppendValuesInStatus)
                .AsEnumerable();
        }

        private static HashSet<T> AppendValuesInStatus<T>(HashSet<T> set, KeyValuePair<Status, IEnumerable<T>> statusConnections) where T : class
        {
            return set.Union(statusConnections.Value).ToHashSet();
        }

        private static List<Status> ExtractLinksInPaths<T>(IDictionary<Status, IEnumerable<T>> tweetLinks, List<List<ConnectionInfo<T>.Node>> paths) where T : class
        {
            var results = new List<Status>();
            foreach (var path in paths)
            {
                if (tweetLinks == null || path == null || tweetLinks.Count == 0)
                    continue;
                for (int i = 0; i < path.Count() - 1; ++i)
                    results.AddRange(tweetLinks
                        .Where(entry => entry.Value.Contains(path[i].Value) && entry.Value.Contains(path[i + 1].Value))
                        .Select(link => link.Key));
            }
            return results;
        }

        private static bool ContinueSearch<T>(int maxAPICalls, int callsMade, ICollection<ConnectionInfo<T>.Node> startList, ICollection<ConnectionInfo<T>.Node> endList, bool foundLink) where T : class
        {
            return !foundLink && (callsMade == 0 || callsMade < maxAPICalls) && (startList.Count > 0 || endList.Count > 0);
        }

        private static Func<HashSet<T>, T, HashSet<T>> AggregateSetConnections<T>(IDictionary<ConnectionInfo<T>.Node, ConnectionInfo<T>> connections) where T : class
        {
            return (set, next) =>
            {
                if (!set.Contains(next))
                    set.Add(next);
                set.UnionWith(connections.FirstOrDefault(conn => conn.Key.Value.Equals(next)).Value?.Connections.Select(c => c.Key.Value) ?? Enumerable.Empty<T>());
                return set;
            };
        }

        /// <summary>
        /// Extracts the actual values in the results and provides a function to check whether a link was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="links"></param>
        /// <returns></returns>
        private (IEnumerable<T>, Func<IEnumerable<T>, IEnumerable<T>, bool>) HandleSearchResults<T>(object results, IDictionary<Status, IEnumerable<T>> links)
            where T : class
        {
            if (results is IEnumerable<T> lookup)
                return (lookup, (values, goals) => values.Intersect(goals).Count() > 0);
            else if (results is IDictionary<Status, IEnumerable<T>> newLinks)
            {
                foreach (var entry in newLinks.Where(entry => !links.ContainsKey(entry.Key)))
                    links.Add(entry.Key, entry.Value);
                var hashtags = newLinks.Aggregate(new List<T>(), (collection, entry) => { collection.AddRange(entry.Value); return collection; });
                return (hashtags, (values, goals) => values.Intersect(goals).Count() > 0);
            }
            return (Enumerable.Empty<T>(), (values, goals) => false);
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
        public async Task<IActionResult> GetUser(string screen_name)
        {
            if (screen_name == null)
                return BadRequest("Invalid parameters.");
            try
            {
                // Look for a cached user, but ensure they have been looked up (and don't just have a cached ID)
                if (TwitterCache.LookupUserByName(Configuration, screen_name) is UserResult cached && cached.ScreenName != null)
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

        private static UserResult ToUserResult(UserSearchResults results)
        {
            if (results == null)
                return null;
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
        /// <param name="limit">The maximum number of users to return.
        /// The maximum number of friends/followers to lookup in one query is 1500 (for each) due to rate limiting.</param>
        /// <returns></returns>
        [HttpGet("user/connections")]
        public async Task<IActionResult> GetUserConnections(string screen_name, int limit = MaxSingleQueryUserLookupCount)
        {
            if (screen_name == null || limit < 1)
                return BadRequest("Invalid parameters.");
            try
            {
                int maxLookupCount = RateLimitCache.Get.MinimumRateLimits(QueryType.UserConnectionsByScreenName, rateLimitDb, userManager, User).Values.Min();
                int lookupLimit = Math.Min(limit, maxLookupCount * MaxSingleQueryUserLookupCount);

                UserResult queried = (await GetUser(screen_name) as OkObjectResult)?.Value as UserResult;
                string userID = queried?.ID;
                if (userID == null)
                    return BadRequest("Invalid user screen name.");

                if (TwitterCache.UserConnectionsQueried(Configuration, queried))
                {
                    IEnumerable<UserResult> cachedResults = TwitterCache.FindUserConnections(Configuration, queried).Take(limit);
                    TwitterCache.UpdateUsers(Configuration, await LookupIDs(maxLookupCount * MaxSingleQueryUserLookupCount, cachedResults.Where(user => user.ScreenName == null).Select(user => user.ID).ToList()));
                    // Now that all cached users have been looked up, return the updated cached results.
                    return Ok(TwitterCache.FindUserConnections(Configuration, queried).Take(limit));
                }

                var remainingIDsToLookup = ((await GetUserConnectionIDs(userID) as OkObjectResult)?.Value as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList();
                ICollection<UserResult> results = await LookupIDs(lookupLimit, remainingIDsToLookup);
                TwitterCache.UpdateUserConnections(Configuration, queried, results);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<ICollection<UserResult>> LookupIDs(int limit, List<string> remainingIDsToLookup)
        {
            ICollection<UserResult> results = new List<UserResult>();
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
        /// <param name="user_id">The id of the Twitter user to search for.</param>
        /// <param name="limit">The maximum number of users to return (capped at 5000 followers and 5000 friends).</param>
        /// <returns></returns>
        [HttpGet("user/connectionids")]
        public async Task<IActionResult> GetUserConnectionIDs(string user_id)
        {
            if (user_id == null)
                return BadRequest("Invalid parameters.");
            try
            {
                if (TwitterCache.UserConnectionsQueried(Configuration, user_id))
                    return Ok(TwitterCache.FindUserConnectionIDs(Configuration, user_id));

                var followerResults = await GetResults<UserIdsResults>(
                    user_id,
                    AuthenticationType.User,
                    TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                    TwitterAPIEndpoint.FollowersIDs);
                ISet<long> uniqueIds = new HashSet<long>(followerResults?.Ids ?? Enumerable.Empty<long>());

                var friendResults = await GetResults<UserIdsResults>(
                    user_id,
                    AuthenticationType.User,
                    TwitterAPIUtils.FollowersFriendsIDsQueryByID,
                    TwitterAPIEndpoint.FriendsIDs);
                if (friendResults != null)
                    foreach (long id in friendResults.Ids)
                    {
                        if (!uniqueIds.Contains(id))
                            uniqueIds.Add(id);
                    }

                TwitterCache.UpdateUserConnections(Configuration, user_id, uniqueIds.Select(id => id.ToString()));
                return Ok(uniqueIds.Select(id => id.ToString()));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
