using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class Status
    {
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("full_text")]
        public string FullText { get; set; }

        [JsonProperty("truncated")]
        public bool Truncated { get; set; }

        [JsonProperty("display_text_range")]
        public List<long> DisplayTextRange { get; set; }

        [JsonProperty("entities")]
        public StatusEntities Entities { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("in_reply_to_status_id")]
        public long? InReplyToStatusId { get; set; }

        [JsonProperty("in_reply_to_status_id_str")]
        public string InReplyToStatusIdStr { get; set; }

        [JsonProperty("in_reply_to_user_id")]
        public long? InReplyToUserId { get; set; }

        [JsonProperty("in_reply_to_user_id_str")]
        public string InReplyToUserIdStr { get; set; }

        [JsonProperty("in_reply_to_screen_name")]
        public string InReplyToScreenName { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("geo")]
        public object Geo { get; set; }

        [JsonProperty("coordinates")]
        public object Coordinates { get; set; }

        [JsonProperty("place")]
        public Place Place { get; set; }

        [JsonProperty("contributors")]
        public object Contributors { get; set; }

        [JsonProperty("is_quote_status")]
        public bool IsQuoteStatus { get; set; }

        [JsonProperty("retweet_count")]
        public long RetweetCount { get; set; }

        [JsonProperty("favorite_count")]
        public long FavoriteCount { get; set; }

        [JsonProperty("favorited")]
        public bool Favorited { get; set; }

        [JsonProperty("retweeted")]
        public bool Retweeted { get; set; }

        [JsonProperty("possibly_sensitive")]
        public bool? PossiblySensitive { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("retweeted_status")]
        public RetweetedStatus RetweetedStatus { get; set; }

        [JsonProperty("extended_entities")]
        public StatusExtendedEntities ExtendedEntities { get; set; }

        [JsonProperty("quoted_status_id")]
        public long? QuotedStatusId { get; set; }

        [JsonProperty("quoted_status_id_str")]
        public string QuotedStatusIdStr { get; set; }

        [JsonProperty("quoted_status")]
        public QuotedStatus QuotedStatus { get; set; }
    }
}