using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class Coordinates
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coordinates")]
        public List<double> Value { get; set; }
    }
}