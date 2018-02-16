namespace SixDegrees.Model.JSON
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public static class Serialize
    {
        public static string ToJson(this FriendSearchResults self) => JsonConvert.SerializeObject(self, SixDegrees.Model.JSON.Converter.Settings);
        public static string ToJson(this List<UserSearchResults> self) => JsonConvert.SerializeObject(self, SixDegrees.Model.JSON.Converter.Settings);
        public static string ToJson(this TweetSearchResults self) => JsonConvert.SerializeObject(self, SixDegrees.Model.JSON.Converter.Settings);
        public static string ToJson(this UserSearchResults self) => JsonConvert.SerializeObject(self, SixDegrees.Model.JSON.Converter.Settings);
    }
}
