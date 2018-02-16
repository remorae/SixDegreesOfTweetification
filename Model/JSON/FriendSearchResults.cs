namespace SixDegrees.Model.JSON
{

    using Newtonsoft.Json;

    public partial class FriendSearchResults
    {
        public static FriendSearchResults FromJson(string json) => JsonConvert.DeserializeObject<FriendSearchResults>(json, SixDegrees.Model.JSON.Converter.Settings);
    }
}
