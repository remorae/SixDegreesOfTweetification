using Newtonsoft.Json;
using System.Collections.Generic;

namespace SixDegrees.Model.JSON
{
    public partial class Place
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("place_type")]
        public string PlaceType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("contained_within")]
        public List<object> ContainedWithin { get; set; }

        [JsonProperty("bounding_box")]
        public BoundingBox BoundingBox { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }
    }
}