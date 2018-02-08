using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class VideoInfo
    {
        [JsonProperty("aspect_ratio")]
        public List<long> AspectRatio { get; set; }

        [JsonProperty("variants")]
        public List<Variant> Variants { get; set; }

        [JsonProperty("duration_millis")]
        public long? DurationMillis { get; set; }
    }
}