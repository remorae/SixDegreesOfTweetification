namespace SixDegrees.Model.JSON
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public static class Serialize
    {
        public static string ToJson(this UserIdsResults self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this List<UserSearchResults> self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this TweetSearchResults self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this UserSearchResults self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this RateLimitResults self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }
}
