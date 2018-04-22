using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Data;
using SixDegrees.Model;

namespace SixDegrees.Controllers
{
    class UserLinkFinder : DegreeLinkFinder<UserResult>
    {
        public UserLinkFinder(IConfiguration configuration, SearchController controller,
            RateLimitDbContext rateLimitDb, UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
            : base(configuration, controller, rateLimitDb, userManager, maxConnectionsPerNode)
        {
        }

        private protected override string Label => "User";

        private protected override TwitterAPIEndpoint RateLimitEndpoint => TwitterAPIEndpoint.FollowersIDs;

        private protected override void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<UserResult>> cachedConnections, string key, ConnectionInfo<TPath>.Node node)
        {
            if (!cachedConnections[key].Contains(node.Value as UserResult))
                cachedConnections[key].Add(node.Value as UserResult);
        }

        private protected override async Task<IActionResult> FindConnections(UserResult query, bool allowAPICalls) =>
                Ok(((await Controller.GetUserConnections(query.ScreenName, allowAPICalls: allowAPICalls) as OkObjectResult)?.Value as IEnumerable<UserResult>));
    }
}
