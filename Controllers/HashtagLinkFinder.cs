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
    class HashtagLinkFinder : DegreeLinkFinder<string>
    {
        public HashtagLinkFinder(IConfiguration configuration, SearchController controller, RateLimitDbContext rateLimitDb,
            UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
            : base(configuration, controller, rateLimitDb, userManager, maxConnectionsPerNode)
        {
        }

        private protected override string Label => "Hashtag";

        private protected override TwitterAPIEndpoint RateLimitEndpoint => TwitterAPIEndpoint.SearchTweets;

        private protected override ICollection<string> ExtractConnections(object lookup) =>
            ((IDictionary<Status, IEnumerable<string>>)lookup).Aggregate(new HashSet<string>(), SearchController.AppendValuesInStatus);

        private protected override IEnumerable<string> ExtractValuesFromSearchResults(object results, IDictionary<Status, ICollection<string>> links)
        {
            var newLinks = (IDictionary<Status, ICollection<string>>)results;
            foreach (var entry in newLinks.Where(entry => !links.ContainsKey(entry.Key)))
                links.Add(entry.Key, entry.Value);
            return newLinks.Aggregate(new List<string>(), (collection, entry) => { collection.AddRange(entry.Value); return collection; });
        }

        private protected override async Task<IActionResult> FindConnections(string query, bool allowAPICalls) =>
            Ok(await Controller.GetUniqueHashtags(query, maximumConnectionsPerNode, allowAPICalls));
    }
}
