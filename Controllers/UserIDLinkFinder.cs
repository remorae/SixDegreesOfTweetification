using System;
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
            string key, ConnectionInfo<TPath>.Node node)
        {
            if (!cachedConnections[key].Contains((node.Value as UserResult).ID))
                cachedConnections[key].Add((node.Value as UserResult).ID);
        }

        protected override async Task<IActionResult> HandleCachedLinkData(
            List<(List<ConnectionInfo<string>.Node> Path, List<Status> Links)> cachedPaths)
        {
            var replacedPaths = new List<(List<ConnectionInfo<UserResult>.Node>, List<Status>)>();
            foreach (var (Path, Links) in cachedPaths)
            {
                var users = await Controller.LookupIDs(SearchController.MaxSingleQueryUserLookupCount, Path.Select(node => node.Value as string).ToList());
                replacedPaths.Add((users.Select((user, index) => new ConnectionInfo<UserResult>.Node(user, index)).ToList(), Links));
            }
            return await FormatCachedLinkData((UserResult user, bool allowAPICalls) => FindConnections(user.ID as string, allowAPICalls), replacedPaths);
        }
    }
}
