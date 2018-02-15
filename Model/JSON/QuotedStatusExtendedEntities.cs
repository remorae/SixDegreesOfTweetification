using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class QuotedStatusExtendedEntities
    {
        [JsonProperty("media")]
        public List<FluffyMedia> Media { get; set; }
    }
}