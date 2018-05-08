using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Data;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    /// <summary>
    /// Finds paths between two users by ID.
    /// </summary>
    class UserIDLinkFinder : DegreeLinkFinder<string>
    {
        public UserIDLinkFinder(IConfiguration configuration, SearchController controller,
            RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
            : base(configuration, controller, rateLimitDb, userManager, maxConnectionsPerNode)
        {
        }

        protected override string Label => "User";

        protected override TwitterAPIEndpoint RateLimitEndpoint => TwitterAPIEndpoint.FollowersIDs;

        protected override async Task<IActionResult> FindConnections(string query, bool allowAPICalls) =>
            Ok(((await Controller.GetUserConnectionIDs(query, allowAPICalls) as OkObjectResult)?.Value as IEnumerable<string>));

        protected override void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<string>> cachedConnections,
            string key, Connection<TPath>.Node node)
        {
            if (!cachedConnections[key].Contains((node.Value as TwitterUser).ID))
                cachedConnections[key].Add((node.Value as TwitterUser).ID);
        }

        protected override async Task<IActionResult> HandleCachedLinkData(
            List<(List<Connection<string>.Node> Path, List<Status> Links)> cachedPaths)
        {
            // We still want populated user objects for the path itself.
            var replacedPaths = new List<(List<Connection<TwitterUser>.Node>, List<Status>)>();
            foreach (var (Path, Links) in cachedPaths)
            {
                var users = await Controller.LookupIDs(SearchController.MaxSingleQueryUserLookupCount, Path.Select(node => node.Value as string).ToList());
                replacedPaths.Add((users.Select((user, index) => new Connection<TwitterUser>.Node(user, index)).ToList(), Links));
            }
            return await FormatCachedLinkData((TwitterUser user, bool allowAPICalls) => FindConnections(user.ID as string, allowAPICalls), replacedPaths);
        }
    }
}
