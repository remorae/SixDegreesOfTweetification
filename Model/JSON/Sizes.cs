using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class Sizes
    {
        [JsonProperty("small")]
        public Large Small { get; set; }

        [JsonProperty("thumb")]
        public Large Thumb { get; set; }

        [JsonProperty("large")]
        public Large Large { get; set; }

        [JsonProperty("medium")]
        public Large Medium { get; set; }
    }
}