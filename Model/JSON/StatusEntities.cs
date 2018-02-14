using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class StatusEntities
    {
        [JsonProperty("hashtags")]
        public List<Hashtag> Hashtags { get; set; }

        [JsonProperty("symbols")]
        public List<object> Symbols { get; set; }

        [JsonProperty("user_mentions")]
        public List<UserMention> UserMentions { get; set; }

        [JsonProperty("urls")]
        public List<Url> Urls { get; set; }

        [JsonProperty("media")]
        public List<PurpleMedia> Media { get; set; }
    }
}