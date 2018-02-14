using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class UserEntities
    {
        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("url")]
        public Description Url { get; set; }
    }
}