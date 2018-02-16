namespace SixDegrees.Model.JSON
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class UserLookupResults
    {
        public static List<UserSearchResults> FromJson(string json) => JsonConvert.DeserializeObject<List<UserSearchResults>>(json, SixDegrees.Model.JSON.Converter.Settings);
    }
}
