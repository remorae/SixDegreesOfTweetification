namespace SixDegrees.Model.JSON
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class RateLimitResults
    {
        [JsonProperty("rate_limit_context")]
        public RateLimitContext RateLimitContext { get; set; }

        [JsonProperty("resources")]
        public Resources Resources { get; set; }
    }

    public partial class RateLimitContext
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public partial class Resources
    {
        [JsonProperty("users")]
        public Dictionary<string, HelpConfiguration> Users { get; set; }

        [JsonProperty("statuses")]
        public Dictionary<string, HelpConfiguration> Statuses { get; set; }

        [JsonProperty("help")]
        public Help Help { get; set; }

        [JsonProperty("search")]
        public Search Search { get; set; }
    }

    public partial class Help
    {
        [JsonProperty("/help/privacy")]
        public HelpConfiguration HelpPrivacy { get; set; }

        [JsonProperty("/help/tos")]
        public HelpConfiguration HelpTos { get; set; }

        [JsonProperty("/help/configuration")]
        public HelpConfiguration HelpConfiguration { get; set; }

        [JsonProperty("/help/languages")]
        public HelpConfiguration HelpLanguages { get; set; }
    }

    public partial class HelpConfiguration
    {
        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("remaining")]
        public long Remaining { get; set; }

        [JsonProperty("reset")]
        public long Reset { get; set; }
    }

    public partial class Search
    {
        [JsonProperty("/search/tweets")]
        public HelpConfiguration SearchTweets { get; set; }
    }

    public partial class FriendSearchResults
    {
        [JsonProperty("previous_cursor")]
        public long PreviousCursor { get; set; }

        [JsonProperty("previous_cursor_str")]
        public string PreviousCursorStr { get; set; }

        [JsonProperty("next_cursor")]
        public long NextCursor { get; set; }

        [JsonProperty("users")]
        public List<UserSearchResults> Users { get; set; }

        [JsonProperty("next_cursor_str")]
        public string NextCursorStr { get; set; }
    }

    public partial class Status
    {
        [JsonProperty("coordinates")]
        public Coordinates Coordinates { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("favorited")]
        public bool Favorited { get; set; }

        [JsonProperty("truncated")]
        public bool Truncated { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("in_reply_to_user_id_str")]
        public string InReplyToUserIdStr { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("contributors")]
        public List<long> Contributors { get; set; }

        [JsonProperty("retweet_count")]
        public long RetweetCount { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("in_reply_to_status_id_str")]
        public string InReplyToStatusIdStr { get; set; }

        [JsonProperty("geo")]
        public object Geo { get; set; }

        [JsonProperty("retweeted")]
        public bool Retweeted { get; set; }

        [JsonProperty("in_reply_to_user_id")]
        public long? InReplyToUserId { get; set; }

        [JsonProperty("place")]
        public Place Place { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("in_reply_to_screen_name")]
        public string InReplyToScreenName { get; set; }

        [JsonProperty("in_reply_to_status_id")]
        public long? InReplyToStatusId { get; set; }

        [JsonProperty("retweeted_status")]
        public Status RetweetedStatus { get; set; }

        [JsonProperty("possibly_sensitive")]
        public bool? PossiblySensitive { get; set; }

        [JsonProperty("entities")]
        public StatusEntities Entities { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("user")]
        public UserSearchResults User { get; set; }

        [JsonProperty("is_quote_status")]
        public bool? IsQuoteStatus { get; set; }

        [JsonProperty("favorite_count")]
        public long? FavoriteCount { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }
    }

    public partial class Coordinates
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coordinates")]
        public List<double> Value { get; set; }
    }

    public partial class BoundingBox
    {
        [JsonProperty("type")]
        public string PurpleType { get; set; }

        [JsonProperty("coordinates")]
        public List<List<List<double>>> Coordinates { get; set; }
    }

    public partial class Place
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("place_type")]
        public string PlaceType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("contained_within")]
        public List<object> ContainedWithin { get; set; }

        [JsonProperty("bounding_box")]
        public BoundingBox BoundingBox { get; set; }

        [JsonProperty("attributes")]
        public object Attributes { get; set; }
    }

    public partial class UserSearchResults
    {
        [JsonProperty("profile_sidebar_fill_color")]
        public string ProfileSidebarFillColor { get; set; }

        [JsonProperty("profile_sidebar_border_color")]
        public string ProfileSidebarBorderColor { get; set; }

        [JsonProperty("profile_background_tile")]
        public bool ProfileBackgroundTile { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profile_image_url")]
        public string ProfileImageUrl { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("follow_request_sent")]
        public bool? FollowRequestSent { get; set; }

        [JsonProperty("profile_link_color")]
        public string ProfileLinkColor { get; set; }

        [JsonProperty("is_translator")]
        public bool IsTranslator { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("default_profile")]
        public bool DefaultProfile { get; set; }

        [JsonProperty("contributors_enabled")]
        public bool ContributorsEnabled { get; set; }

        [JsonProperty("favourites_count")]
        public long FavouritesCount { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("profile_banner_url")]
        public string ProfileBannerUrl { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("utc_offset")]
        public long? UtcOffset { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("profile_use_background_image")]
        public bool ProfileUseBackgroundImage { get; set; }

        [JsonProperty("listed_count")]
        public long ListedCount { get; set; }

        [JsonProperty("profile_text_color")]
        public string ProfileTextColor { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("followers_count")]
        public long FollowersCount { get; set; }

        [JsonProperty("protected")]
        public bool Protected { get; set; }

        [JsonProperty("notifications")]
        public bool? Notifications { get; set; }

        [JsonProperty("profile_background_image_url_https")]
        public string ProfileBackgroundImageUrlHttps { get; set; }

        [JsonProperty("profile_background_color")]
        public string ProfileBackgroundColor { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("geo_enabled")]
        public bool GeoEnabled { get; set; }

        [JsonProperty("time_zone")]
        public string TimeZone { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("default_profile_image")]
        public bool DefaultProfileImage { get; set; }

        [JsonProperty("profile_background_image_url")]
        public string ProfileBackgroundImageUrl { get; set; }

        [JsonProperty("statuses_count")]
        public long StatusesCount { get; set; }

        [JsonProperty("friends_count")]
        public long FriendsCount { get; set; }

        [JsonProperty("following")]
        public bool? Following { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("show_all_inline_media")]
        public bool? ShowAllInlineMedia { get; set; }

        [JsonProperty("entities")]
        public UserEntities Entities { get; set; }

        [JsonProperty("profile_location")]
        public object ProfileLocation { get; set; }

        [JsonProperty("is_translation_enabled")]
        public bool? IsTranslationEnabled { get; set; }

        [JsonProperty("has_extended_profile")]
        public bool? HasExtendedProfile { get; set; }

        [JsonProperty("translator_type")]
        public string TranslatorType { get; set; }
    }

    public partial class StatusEntities
    {
        [JsonProperty("urls")]
        public List<object> Urls { get; set; }

        [JsonProperty("hashtags")]
        public List<Hashtag> Hashtags { get; set; }

        [JsonProperty("user_mentions")]
        public List<UserMention> UserMentions { get; set; }

        [JsonProperty("symbols")]
        public List<object> Symbols { get; set; }
    }

    public partial class Hashtag
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("indices")]
        public List<long> Indices { get; set; }
    }

    public partial class UserMention
    {
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("indices")]
        public List<long> Indices { get; set; }
    }

    public partial class Metadata
    {
        [JsonProperty("iso_language_code")]
        public string IsoLanguageCode { get; set; }

        [JsonProperty("result_type")]
        public string ResultType { get; set; }
    }

    public partial class UserEntities
    {
        [JsonProperty("url")]
        public Description Url { get; set; }

        [JsonProperty("description")]
        public Description Description { get; set; }
    }

    public partial class Description
    {
        [JsonProperty("urls")]
        public List<Url> Urls { get; set; }
    }

    public partial class Url
    {
        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("url")]
        public string UrlUrl { get; set; }

        [JsonProperty("indices")]
        public List<long> Indices { get; set; }

        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }
    }

    public partial class TweetSearchResults
    {
        [JsonProperty("statuses")]
        public List<Status> Statuses { get; set; }

        [JsonProperty("search_metadata")]
        public SearchMetadata SearchMetadata { get; set; }
    }

    public partial class SearchMetadata
    {
        [JsonProperty("max_id")]
        public long MaxId { get; set; }

        [JsonProperty("since_id")]
        public long SinceId { get; set; }

        [JsonProperty("refresh_url")]
        public string RefreshUrl { get; set; }

        [JsonProperty("next_results")]
        public string NextResults { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("completed_in")]
        public double CompletedIn { get; set; }

        [JsonProperty("since_id_str")]
        public string SinceIdStr { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("max_id_str")]
        public string MaxIdStr { get; set; }
    }
}
