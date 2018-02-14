using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class Variant
    {
        [JsonProperty("bitrate")]
        public long? Bitrate { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}