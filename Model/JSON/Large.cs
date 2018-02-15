using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class Large
    {
        [JsonProperty("w")]
        public long W { get; set; }

        [JsonProperty("h")]
        public long H { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }
}