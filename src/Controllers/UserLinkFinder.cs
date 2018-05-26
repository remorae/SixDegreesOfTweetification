using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Data;
using SixDegrees.Model;

namespace SixDegrees.Controllers
{
    /// <summary>
    /// Finds paths between two users.
    /// </summary>
    class UserLinkFinder : DegreeLinkFinder<TwitterUser>
    {
        public UserLinkFinder(IConfiguration configuration, SearchController controller,
            RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
            : base(configuration, controller, rateLimitDb, userManager, maxConnectionsPerNode)
        {
        }

        protected override string Label => "User";

        protected override TwitterAPIEndpoint RateLimitEndpoint => TwitterAPIEndpoint.FollowersIDs;

        protected override void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<TwitterUser>> cachedConnections, string key, Connection<TPath>.Node node)
        {
            if (!cachedConnections.ContainsKey(key))
                cachedConnections[key] = new HashSet<TwitterUser>();
            if (!cachedConnections[key].Contains(node.Value as TwitterUser))
                cachedConnections[key].Add(node.Value as TwitterUser);
        }

        protected override async Task<IActionResult> FindConnections(TwitterUser query, bool allowAPICalls) =>
                Ok(((await Controller.GetUserConnections(query.ScreenName, allowAPICalls: allowAPICalls) as OkObjectResult)?.Value as IEnumerable<TwitterUser>));
    }
}
