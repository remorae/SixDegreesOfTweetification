namespace SixDegrees.Model.JSON
{
    using Newtonsoft.Json;

    public partial class TweetSearchResults
    {
        public static TweetSearchResults FromJson(string json) => JsonConvert.DeserializeObject<TweetSearchResults>(json, SixDegrees.Model.JSON.Converter.Settings);
    }
}
