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
    /// Finds paths between two hashtags.
    /// </summary>
    class HashtagLinkFinder : DegreeLinkFinder<string>
    {
        public HashtagLinkFinder(IConfiguration configuration, SearchController controller, RateLimitDbContext rateLimitDb,
            UserManager<ApplicationUser> userManager, int maxConnectionsPerNode)
            : base(configuration, controller, rateLimitDb, userManager, maxConnectionsPerNode)
        {
        }

        protected override string Label => "Hashtag";

        protected override TwitterAPIEndpoint RateLimitEndpoint => TwitterAPIEndpoint.SearchTweets;
        
        protected override void EnsureLinksToNext<TPath>(Dictionary<string, ICollection<string>> cachedConnections,
            string key, Connection<TPath>.Node node)
        {
            if (!cachedConnections.ContainsKey(key))
                cachedConnections[key] = new HashSet<string>();
            if (!cachedConnections[key].Contains(node.Value as string))
                cachedConnections[key].Add(node.Value as string);
        }
        
        protected override ICollection<string> ExtractCachedConnections(object lookup) =>
            ((IDictionary<Status, IEnumerable<string>>)lookup).Aggregate(new HashSet<string>(), SearchController.AppendValuesInStatus);
        
        protected override IEnumerable<string> ExtractValuesFromSearchResults(object results, IDictionary<Status, ICollection<string>> links)
        {
            var newLinks = (IDictionary<Status, IEnumerable<string>>)results;
            foreach (var entry in newLinks.Where(entry => !links.ContainsKey(entry.Key)))
                links.Add(entry.Key, entry.Value.ToList());
            return newLinks.Aggregate(new List<string>(), (collection, entry) => { collection.AddRange(entry.Value); return collection; });
        }
        
        protected override async Task<IActionResult> FindConnections(string query, bool allowAPICalls) =>
            Ok(await Controller.GetUniqueHashtags(query, maximumConnectionsPerNode, allowAPICalls));
    }
}
