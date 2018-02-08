using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class Metadata
    {
        [JsonProperty("iso_language_code")]
        public string IsoLanguageCode { get; set; }

        [JsonProperty("result_type")]
        public string ResultType { get; set; }
    }
}