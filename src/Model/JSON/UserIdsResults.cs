namespace SixDegrees.Model.JSON
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public partial class UserIdsResults : IQueryResults
    {
        public static List<UserIdsResults> FromJson(string json) => JsonConvert.DeserializeObject<List<UserIdsResults>>(json, Converter.Settings);
    }
}
