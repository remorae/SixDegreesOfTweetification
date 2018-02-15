using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class SearchMetadata
    {
        [JsonProperty("completed_in")]
        public double CompletedIn { get; set; }

        [JsonProperty("max_id")]
        public long MaxId { get; set; }

        [JsonProperty("max_id_str")]
        public string MaxIdStr { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("refresh_url")]
        public string RefreshUrl { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("since_id")]
        public long SinceId { get; set; }

        [JsonProperty("since_id_str")]
        public string SinceIdStr { get; set; }
    }
}