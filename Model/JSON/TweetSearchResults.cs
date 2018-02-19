namespace SixDegrees.Model.JSON
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TweetSearchResults : IQueryResults
    {
        public string MinStatusID => Statuses.Min(status => status.IdStr);
        public static TweetSearchResults FromJson(string json) => JsonConvert.DeserializeObject<TweetSearchResults>(json, Converter.Settings);
    }
}
