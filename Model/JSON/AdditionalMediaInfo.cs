using Newtonsoft.Json;

namespace SixDegrees.Model.JSON
{
    public partial class AdditionalMediaInfo
    {
        [JsonProperty("monetizable")]
        public bool Monetizable { get; set; }
    }
}