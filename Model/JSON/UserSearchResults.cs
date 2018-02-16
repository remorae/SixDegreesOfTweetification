namespace SixDegrees.Model.JSON
{
    using Newtonsoft.Json;

    public partial class UserSearchResults
    {
        public static UserSearchResults FromJson(string json) => JsonConvert.DeserializeObject<UserSearchResults>(json, SixDegrees.Model.JSON.Converter.Settings);
    }
}
