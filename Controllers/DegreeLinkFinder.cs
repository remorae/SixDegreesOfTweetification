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
    abstract class DegreeLinkFinder<TConnection>
        where TConnection : class
    {
        protected const int MaxNodesPerDegreeSearch = 1000;

        protected readonly int maximumConnectionsPerNode;
        private DateTime startTime = DateTime.Now;
        private TimeSpan timeSpentSearching;
        private int callsMade = 0;

        protected DegreeLinkFinder(IConfiguration configuration, SearchController controller,
            RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
        {
            Configuration = configuration;
            Controller = controller;
            RateLimitDb = rateLimitDb;
            UserManager = userManager;
            maximumConnectionsPerNode = maxConnectionsPerNode;
        }

        protected IConfiguration Configuration { get; }
        protected SearchController Controller { get; }
        protected RateLimitDbContext RateLimitDb { get; }
        protected UserManager<ApplicationUser> UserManager { get; }
        protected ClaimsPrincipal User => Controller.User;

        protected abstract string Label { get; }
        protected abstract TwitterAPIEndpoint RateLimitEndpoint { get; }

        protected IActionResult BadRequest(object error) => Controller.BadRequest(error);
        protected IActionResult Ok(object value) => Controller.Ok(value);

        protected abstract Task<IActionResult> FindConnections(TConnection query, bool allowAPICalls);

        protected virtual async Task<IActionResult> HandleCachedLinkData(List<(List<ConnectionInfo<TConnection>.Node> Path, List<Status> Links)> cachedPaths) =>
            await FormatCachedLinkData((query, allowAPICalls) => FindConnections(query as TConnection, allowAPICalls), cachedPaths);

        protected virtual ICollection<TConnection> ExtractConnections(object lookup) =>
            ((IEnumerable<TConnection>)lookup).Take(maximumConnectionsPerNode).ToList();

        protected virtual IEnumerable<TConnection> ExtractValuesFromSearchResults(object results,
            IDictionary<Status, ICollection<TConnection>> links) => (IEnumerable<TConnection>)results;

        public async Task<IActionResult> Execute(TConnection start, TConnection end, int maxNumberOfDegrees, int maxAPICalls)
        {
            callsMade = 0;
            startTime = DateTime.Now;
            timeSpentSearching = new TimeSpan();

            if (maxAPICalls < 1)
                return BadRequest("Rate limit exceeded.");
            if (start.Equals(end))
                return BadRequest("Start and end must differ.");

            try
            {
                if (TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, Label)
                    is List<(List<ConnectionInfo<TConnection>.Node>, List<Status>)> cachedUserPaths
                    && cachedUserPaths.Count > 0)
                {
                    SearchController.Log("Retrieving cached path...");
                    return await HandleCachedLinkData(cachedUserPaths);
                }
                SearchController.Log("New search begins...");

                IDictionary<Status, ICollection<TConnection>> tweetLinksFound = new Dictionary<Status, ICollection<TConnection>>();
                var remainingFromStart = new List<ConnectionInfo<TConnection>.Node>() { new ConnectionInfo<TConnection>.Node(start, 0) };
                var remainingFromEnd = new List<ConnectionInfo<TConnection>.Node>() { new ConnectionInfo<TConnection>.Node(end, 0) };
                ISet<TConnection> queriedNodes = new HashSet<TConnection>();
                ISet<TConnection> seenValues = new HashSet<TConnection>() { start, end };
                IDictionary<ConnectionInfo<TConnection>.Node, ConnectionInfo<TConnection>> connections = new Dictionary<ConnectionInfo<TConnection>.Node, ConnectionInfo<TConnection>>(new ConnectionInfo<TConnection>.Node.EqualityComparer())
                {
                    { remainingFromStart.First(), new ConnectionInfo<TConnection>() },
                    { remainingFromEnd.First(), new ConnectionInfo<TConnection>() }
                };
                
                bool foundLink = false;
                bool currentSearchIsFromStart = true;

                while (ContinueSearch(maxAPICalls, remainingFromStart, remainingFromEnd, foundLink))
                {
                    var remaining = currentSearchIsFromStart ? remainingFromStart : remainingFromEnd;
                    if (remaining.Count > 0)
                    {
                        ConnectionInfo<TConnection>.Node nodeToQuery = remaining.First();
                        remaining.Remove(nodeToQuery);

                        int previousLimit = RateLimitController.GetCurrentUserInfo(RateLimitDb, RateLimitEndpoint, UserManager, User).Limit;
                        if ((await FindConnections(nodeToQuery.Value, true) as OkObjectResult)?.Value is object lookupResult)
                        {
                            int newLimit = RateLimitController.GetCurrentUserInfo(RateLimitDb, RateLimitEndpoint, UserManager, User).Limit;
                            if (newLimit < previousLimit)
                                ++callsMade;

                            IEnumerable<TConnection> lookupValues = ExtractValuesFromSearchResults(lookupResult, tweetLinksFound);
                            lookupValues = lookupValues.Where(result => !result.Equals(nodeToQuery.Value)).Distinct();

                            queriedNodes.Add(nodeToQuery.Value);
                            int nextDistance = nodeToQuery.Distance + 1;

                            foreach (var value in lookupValues)
                            {
                                AddConnections(connections, nodeToQuery, nextDistance, value);
                                MarkNewNodes(maxNumberOfDegrees, seenValues, connections, remaining, nextDistance, value);
                            }
                            remaining.Sort((lhs, rhs) => lhs.Heuristic(nextDistance).CompareTo(rhs.Heuristic(nextDistance)));

                            foundLink = TwitterCache.PathExists(Configuration, start, end, maxNumberOfDegrees, Label);
                        }
                    }
                    currentSearchIsFromStart = !currentSearchIsFromStart;
                }

                if (foundLink)
                {
                    SearchController.Log("SUCCESS - Formatting results...");
                    timeSpentSearching = DateTime.Now - startTime;
                    return await HandleCachedLinkData(TwitterCache.ShortestPaths(Configuration, start, end, maxNumberOfDegrees, Label));
                }

                SearchController.Log("FAILURE");
                return FormatObtainedConnections(start, connections);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool ContinueSearch(int maxAPICalls, ICollection<ConnectionInfo<TConnection>.Node> startList,
            ICollection<ConnectionInfo<TConnection>.Node> endList, bool foundLink) =>
            !foundLink && (callsMade == 0 || callsMade < maxAPICalls) && startList.Count > 0 && endList.Count > 0;

        private IActionResult FormatObtainedConnections(TConnection start, IDictionary<ConnectionInfo<TConnection>.Node, ConnectionInfo<TConnection>> connections)
        {
            var formattedConnections = connections
                .Where(entry => entry.Value.Connections.Count > 0)
                .ToDictionary(entry => entry.Key.Value, entry => entry.Value.Connections.Select(node => node.Key.Value));
            return Ok(new LinkData<TConnection, TConnection>()
            {
                Connections = ((start is UserResult)
                ? formattedConnections.ToDictionary(entry => (entry.Key as UserResult).ScreenName, entry => entry.Value)
                : formattedConnections as Dictionary<string, IEnumerable<TConnection>>)
                .ToDictionary(entry => entry.Key, entry => entry.Value.ToList() as ICollection<TConnection>),
                Paths = Enumerable.Empty<LinkPath<TConnection>>(),
                Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, Calls = callsMade }
            });
        }

        private static void MarkNewNodes(int maxNumberOfDegrees, ISet<TConnection> seenValues, IDictionary<ConnectionInfo<TConnection>.Node,
            ConnectionInfo<TConnection>> connections, List<ConnectionInfo<TConnection>.Node> remaining, int nextDistance, TConnection value)
        {
            if (!seenValues.Contains(value))
            {
                var node = new ConnectionInfo<TConnection>.Node(value, nextDistance);
                if (nextDistance < maxNumberOfDegrees - 1)
                    remaining.Add(node);
                connections[node] = new ConnectionInfo<TConnection>();
                seenValues.Add(value);
            }
        }

        private void AddConnections(IDictionary<ConnectionInfo<TConnection>.Node, ConnectionInfo<TConnection>> connections,
            ConnectionInfo<TConnection>.Node nodeToQuery, int nextDistance, TConnection value)
        {
            if (connections[nodeToQuery].Connections.Count < maximumConnectionsPerNode)
                connections[nodeToQuery].Connections.Add(new ConnectionInfo<TConnection>.Node(value, nextDistance), 1);
        }

        protected async Task<IActionResult> FormatCachedLinkData<TPath>(Func<TPath, bool, Task<IActionResult>> findConnections,
            List<(List<ConnectionInfo<TPath>.Node> Path, List<Status> Links)> cachedPaths)
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
                Metadata = new LinkMetaData() { Time = DateTime.Now - startTime, SearchTime = timeSpentSearching, Calls = callsMade }
            });
        }

        private async Task<Dictionary<string, ICollection<TConnection>>> GetCachedConnections<TPath>(
            List<(List<ConnectionInfo<TPath>.Node> Path, List<Status> Links)> cachedPaths, Func<TPath, bool, Task<IActionResult>> findConnections)
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
                        cachedConnections[key] = ExtractConnections(lookupResults);
                        if (i < Path.Count - 1)
                            EnsureLinksToNext(cachedConnections, key, Path[i + 1]);
                        count += cachedConnections[key].Count;
                    }
                }
                if (count >= MaxNodesPerDegreeSearch)
                    break;
            }

            SearchController.Log($"All connections retrieved after {DateTime.Now - startTime} since start.");
            return cachedConnections;
        }

        protected abstract void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<TConnection>> cachedConnections,
            string key, ConnectionInfo<TPath>.Node node)
            where TPath : class;

        private static string GetAppropriateKey<TPath>(ConnectionInfo<TPath>.Node node)
            where TPath : class
        {
            if (typeof(TPath) == typeof(string))
                return GetAppropriateKey(node as ConnectionInfo<string>.Node);
            else if (typeof(TPath) == typeof(UserResult))
                return GetAppropriateKey(node as ConnectionInfo<UserResult>.Node);
            throw new ArgumentException("Unknown key for given node.");
        }

        private static string GetAppropriateKey(ConnectionInfo<UserResult>.Node node) =>
            (typeof(TConnection) == typeof(UserResult))
            ? node.Value.ScreenName ?? node.Value.ID
            : node.Value.ID;

        private static string GetAppropriateKey(ConnectionInfo<string>.Node node) => node.Value;
    }
}
