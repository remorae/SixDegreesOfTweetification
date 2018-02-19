namespace SixDegrees.Model.JSON
{
    using Newtonsoft.Json;

    public partial class UserSearchResults : IQueryResults
    {
        public static UserSearchResults FromJson(string json) => JsonConvert.DeserializeObject<UserSearchResults>(json, Converter.Settings);
    }
}
