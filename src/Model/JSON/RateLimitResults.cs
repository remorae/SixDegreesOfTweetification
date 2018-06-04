using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class RateLimitResults
    {
        public static RateLimitResults FromJson(string json) => JsonConvert.DeserializeObject<RateLimitResults>(json, Converter.Settings);
    }
}
