using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class Description
    {
        [JsonProperty("urls")]
        public List<Url> Urls { get; set; }
    }
}