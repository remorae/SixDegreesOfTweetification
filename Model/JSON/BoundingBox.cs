using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class BoundingBox
    {
        [JsonProperty("type")]
        public string PurpleType { get; set; }

        [JsonProperty("coordinates")]
        public List<List<List<double>>> Coordinates { get; set; }
    }
}