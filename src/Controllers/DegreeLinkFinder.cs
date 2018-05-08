using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Data;
using SixDegrees.Model;
using SixDegrees.Model.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SixDegrees.Controllers
{
    /// <summary>
    /// Handles computation of paths between Twitter information.
    /// </summary>
    /// <typeparam name="TConnection">The type of objects that make up each path.</typeparam>
    abstract class DegreeLinkFinder<TConnection>
        where TConnection : class
    {
        protected const int MaxNodesPerDegreeSearch = 1000;

        protected readonly int maximumConnectionsPerNode;
        private DateTime startTime = DateTime.Now;
        private int callsMade = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="controller"></param>
        /// <param name="rateLimitDb"></param>
        /// <param name="userManager"></param>
        /// <param name="maxConnectionsPerNode">The cap on the number of connected entities to any given entity. Lower to reduce lag during visualization.</param>
        protected DegreeLinkFinder(IConfiguration configuration, SearchController controller,
            RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
        {
            Configuration = configuration;
            Controller = controller;
            RateLimitDb = rateLimitDb;
            UserManager = userManager;
            maximumConnectionsPerNode = maxConnectionsPerNode;
        }

        // Info taken from the constructing controller in order to make API calls and interact with the database.
        protected IConfiguration Configuration { get; }
        protected SearchController Controller { get; }
        protected RateLimitDbContext RateLimitDb { get; }
        protected UserManager<ApplicationUser> UserManager { get; }
        protected ClaimsPrincipal User => Controller.User;

        protected IActionResult BadRequest(object error) => Controller.BadRequest(error);
        protected IActionResult Ok(object value) => Controller.Ok(value);

        /// <summary>
        /// The corresponding Neo4j node label for any cached path data.
        /// </summary>
        protected abstract string Label { get; }
        /// <summary>
        /// Which Twitter API endpoint is being used to find live connection data.
        /// </summary>
        protected abstract TwitterAPIEndpoint RateLimitEndpoint { get; }

        /// <summary>
        /// Finds cconnection information for the given entity and caches it if necessary.
        /// </summary>
        /// <param name="query">The entity to gather connections for.</param>
        /// <param name="allowAPICalls">Whether live information can be gathered.</param>
        /// <returns></returns>
        protected abstract Task<IActionResult> FindConnections(TConnection query, bool allowAPICalls);

        /// <summary>
        /// Hook for derived classes to perform any special logic with cached path results before formatting it.
        /// </summary>
        /// <param name="cachedPaths">The obtained paths.</param>
        /// <returns></returns>
        protected virtual async Task<IActionResult> HandleCachedLinkData(List<(List<Connection<TConnection>.Node> Path, List<Status> Links)> cachedPaths) =>
            await FormatCachedLinkData((query, allowAPICalls) => FindConnections(query as TConnection, allowAPICalls), cachedPaths);

        /// <summary>
        /// Parses connection information from a cache lookup.
        /// </summary>
        /// <param name="lookup">The cached data.</param>
        /// <returns></returns>
        protected virtual ICollection<TConnection> ExtractCachedConnections(object lookup) =>
            ((IEnumerable<TConnection>)lookup).Take(maximumConnectionsPerNode).ToList();

        /// <summary>
        /// Parses connection information from a search (either from the Twitter API or from the cache).
        /// </summary>
        /// <param name="results">The results of the search.</param>
        /// <param name="links">A collection to store any Tweets used to connect objects in the path.</param>
        /// <returns></returns>
        protected virtual IEnumerable<TConnection> ExtractValuesFromSearchResults(object results,
            IDictionary<Status, ICollection<TConnection>> links) => (IEnumerable<TConnection>)results;

        protected async Task<IActionResult> FormatCachedLinkData<TPath>(Func<TPath, bool, Task<IActionResult>> findConnections,
            List<(List<Connection<TPath>.Node> Path, List<Status> Links)> cachedPaths)
            where TPath : class
        {
            return Ok(new LinkData<TPath, TConnection>()
            {
                Connections = await GetCachedConnections(cachedPaths, findConnections),
                Paths = cachedPaths?.Select(tuple => new LinkPath<TPath>()
                {
                    Path = tuple.Path.ToDictionary(node => node.Distance, node => node.Value),
                    Links = tuple.Links.Select(link => link.URL)
                }) ?? Enumerable.Empty<LinkPath<TPath>>(),
                Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, Calls = callsMade }
            });
        }

        protected abstract void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<TConnection>> cachedConnections,
            string key, Connection<TPath>.Node node)
            where TPath : class;

        /// <summary>
        /// Finds a new set of paths between the given entities.
        /// </summary>
        /// <remarks>
        /// An instance of this class can be reused. Each call to Execute will clear all previous computations.
        /// </remarks>
        /// <param name="start">The starting entity to search for.</param>
        /// <param name="end">The ending entity to search for.</param>
        /// <param name="maxNumberOfDegrees">The cap on the path length between the starting and ending nodes.</param>
        /// <param name="maxAPICalls">The maximum number of Twitter API calls to make during the search before giving up.</param>
        /// <returns>The results of the search.</returns>
        public async Task<IActionResult> Execute(TConnection start, TConnection end, int maxNumberOfDegrees, int maxAPICalls)
        {
            callsMade = 0;
            startTime = DateTime.Now;

            if (maxAPICalls < 1)
                return BadRequest("Rate limit exceeded.");
            if (start.Equals(end))
                return BadRequest("Start and end must differ.");

            try
            {
                // If a path exists in the cache, return it.
                if (TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, Label)
                    is List<(List<Connection<TConnection>.Node>, List<Status>)> cachedUserPaths
                    && cachedUserPaths.Count > 0)
                {
                    return await HandleCachedLinkData(cachedUserPaths);
                }

                #region Variable Initialization
                IDictionary<Status, ICollection<TConnection>> tweetLinksFound = new Dictionary<Status, ICollection<TConnection>>();
                var remainingNodesToQueryFromStart = new List<Connection<TConnection>.Node>() { new Connection<TConnection>.Node(start, 0) };
                var remainingNodesToQueryFromEnd = new List<Connection<TConnection>.Node>() { new Connection<TConnection>.Node(end, 0) };
                ISet<TConnection> queriedNodes = new HashSet<TConnection>();
                ISet<TConnection> seenValues = new HashSet<TConnection>() { start, end };
                IDictionary<Connection<TConnection>.Node, Connection<TConnection>> connections = new Dictionary<Connection<TConnection>.Node, Connection<TConnection>>(new Connection<TConnection>.Node.EqualityComparer())
                {
                    { remainingNodesToQueryFromStart.First(), new Connection<TConnection>() },
                    { remainingNodesToQueryFromEnd.First(), new Connection<TConnection>() }
                };

                bool foundLink = false;
                bool currentSearchIsFromStart = true;
                #endregion
                
                // Only continue the search if a path has not been found, the cap on API calls has not been hit, and both "ends" of the search have remaining possibilities.
                while (ContinueSearch(maxAPICalls, remainingNodesToQueryFromStart, remainingNodesToQueryFromEnd, foundLink))
                {
                    // Get the next node to search with
                    var remaining = currentSearchIsFromStart ? remainingNodesToQueryFromStart : remainingNodesToQueryFromEnd;
                    Connection<TConnection>.Node nodeToQuery = remaining.First();
                    remaining.Remove(nodeToQuery);
                    
                    int previousLimit = RateLimitController.GetCurrentUserInfo(RateLimitDb, RateLimitEndpoint, UserManager, User).Limit;
                    if ((await FindConnections(nodeToQuery.Value, true) as OkObjectResult)?.Value is object lookupResult)
                    {
                        // If the rate limits have changed, we made an API call. Otherwise, the connections were cached.
                        int newLimit = RateLimitController.GetCurrentUserInfo(RateLimitDb, RateLimitEndpoint, UserManager, User).Limit;
                        if (newLimit < previousLimit)
                            ++callsMade;

                        // Get the distinct connection information we need
                        IEnumerable<TConnection> lookupValues = ExtractValuesFromSearchResults(lookupResult, tweetLinksFound);
                        lookupValues = lookupValues.Where(result => !result.Equals(nodeToQuery.Value)).Distinct();

                        // Mark the current node we're searching with as queried to prevent repeats
                        queriedNodes.Add(nodeToQuery.Value);
                        int nextDistance = nodeToQuery.Distance + 1;

                        foreach (var value in lookupValues)
                        {
                            // Store all the connections in the results as 1 away from the current node
                            AddConnections(connections, nodeToQuery, nextDistance, value);
                            // Add any brand-new results to the remaining search possibilities
                            if (!seenValues.Contains(value))
                            {
                                HandleNewNode(maxNumberOfDegrees, connections, remaining, nextDistance, value);
                                seenValues.Add(value);
                            }
                        }

                        // Did we find a path?
                        foundLink = TwitterCache.PathExists(Configuration, start, end, maxNumberOfDegrees, Label);
                        if (!foundLink)
                        {
                            // Determine which node should be searched with next
                            remaining.Sort((lhs, rhs) => lhs.Heuristic(nextDistance).CompareTo(rhs.Heuristic(nextDistance)));
                        }
                    }
                    // Alternate searching from the start and the end
                    currentSearchIsFromStart = !currentSearchIsFromStart;
                }

                if (foundLink) // The path is guaranteed to be in the cache, so just use it for simplicity's sake.
                    return await HandleCachedLinkData(TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, Label));

                // We ran out of possibilities or API calls, so tidy up what we've gathered so far.
                return FormatObtainedConnections(start, connections);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool ContinueSearch(int maxAPICalls, ICollection<Connection<TConnection>.Node> startList,
            ICollection<Connection<TConnection>.Node> endList, bool foundLink) =>
            !foundLink && (callsMade == 0 || callsMade < maxAPICalls) && startList.Count > 0 && endList.Count > 0;

        private IActionResult FormatObtainedConnections(TConnection start, IDictionary<Connection<TConnection>.Node, Connection<TConnection>> connections)
        {
            var formattedConnections = connections
                .Where(entry => entry.Value.Connections.Count > 0)
                .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value));
            return Ok(new LinkData<TConnection, TConnection>()
            {
                Connections = ((start is TwitterUser)
                ? formattedConnections.ToDictionary(entry => (entry.Key as TwitterUser).ScreenName, entry => entry.Value)
                : formattedConnections as Dictionary<string, IEnumerable<TConnection>>)
                .ToDictionary(entry => entry.Key, entry => entry.Value.ToList() as ICollection<TConnection>),
                Paths = Enumerable.Empty<LinkPath<TConnection>>(),
                Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, Calls = callsMade }
            });
        }
        
        private static void HandleNewNode(int maxNumberOfDegrees, IDictionary<Connection<TConnection>.Node,
            Connection<TConnection>> connections, List<Connection<TConnection>.Node> remaining, int nextDistance, TConnection value)
        {
            var node = new Connection<TConnection>.Node(value, nextDistance);
            if (nextDistance < maxNumberOfDegrees - 1)
                remaining.Add(node);
            connections[node] = new Connection<TConnection>();
        }

        private void AddConnections(IDictionary<Connection<TConnection>.Node, Connection<TConnection>> connections,
            Connection<TConnection>.Node nodeToQuery, int nextDistance, TConnection value)
        {
            // Don't track new connections if we've hit the cap for that specific node
            if (connections[nodeToQuery].Connections.Count < maximumConnectionsPerNode)
                connections[nodeToQuery].Connections.Add(new Connection<TConnection>.Node(value, nextDistance), 1);
        }

        private async Task<Dictionary<string, ICollection<TConnection>>> GetCachedConnections<TPath>(
            List<(List<Connection<TPath>.Node> Path, List<Status> Links)> cachedPaths, Func<TPath, bool, Task<IActionResult>> findConnections)
            where TPath : class
        {
            var cachedConnections = new Dictionary<string, ICollection<TConnection>>();
            int count = 0;
            foreach (var (Path, Links) in cachedPaths)
            {
                for (int i = 0; i < Path.Count; ++i)
                {
                    var node = Path[i];
                    string key = GetAppropriateKey(node);
                    if (cachedConnections.ContainsKey(key))
                        continue;
                    var lookupResults = (await findConnections(node.Value, true) as OkObjectResult)?.Value;
                    if (lookupResults != null)
                    {
                        cachedConnections[key] = ExtractCachedConnections(lookupResults);
                        if (i < Path.Count - 1)
                            EnsureLinksToNext(cachedConnections, key, Path[i + 1]);
                        count += cachedConnections[key].Count;
                    }
                }
                if (count >= MaxNodesPerDegreeSearch)
                    break;
            }

            return cachedConnections;
        }

        private static string GetAppropriateKey<TPath>(Connection<TPath>.Node node)
            where TPath : class
        {
            if (typeof(TPath) == typeof(string))
                return GetAppropriateKey(node as Connection<string>.Node);
            else if (typeof(TPath) == typeof(TwitterUser))
                return GetAppropriateKey(node as Connection<TwitterUser>.Node);
            throw new ArgumentException("Unknown key for given node.");
        }

        private static string GetAppropriateKey(Connection<TwitterUser>.Node node) =>
            (typeof(TConnection) == typeof(TwitterUser))
            ? node.Value.ScreenName ?? node.Value.ID
            : node.Value.ID;

        private static string GetAppropriateKey(Connection<string>.Node node) => node.Value;
    }
}
