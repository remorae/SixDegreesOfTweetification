using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class StatusExtendedEntities
    {
        [JsonProperty("media")]
        public List<PurpleMedia> Media { get; set; }
    }
}