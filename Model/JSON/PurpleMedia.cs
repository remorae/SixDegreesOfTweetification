using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class PurpleMedia
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("indices")]
        public List<long> Indices { get; set; }

        [JsonProperty("media_url")]
        public string MediaUrl { get; set; }

        [JsonProperty("media_url_https")]
        public string MediaUrlHttps { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }

        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("type")]
        public string PurpleType { get; set; }

        [JsonProperty("sizes")]
        public Sizes Sizes { get; set; }

        [JsonProperty("source_status_id")]
        public long? SourceStatusId { get; set; }

        [JsonProperty("source_status_id_str")]
        public string SourceStatusIdStr { get; set; }

        [JsonProperty("source_user_id")]
        public long? SourceUserId { get; set; }

        [JsonProperty("source_user_id_str")]
        public string SourceUserIdStr { get; set; }
    }
}