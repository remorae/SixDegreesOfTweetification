using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class TweetSearch
    {
        [JsonProperty("statuses")]
        public List<Status> Statuses { get; set; }

        [JsonProperty("search_metadata")]
        public SearchMetadata SearchMetadata { get; set; }
    }
}